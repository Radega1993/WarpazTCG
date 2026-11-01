using System;
using System.Diagnostics;
using Warpaz.Application;
using Warpaz.Content;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    /// <summary>
    /// Deterministic soak: Play / Reorganize / Attack / Intercept / EndTurn via LegalActions.
    /// </summary>
    public sealed class Sprint2StressRunner
    {
        private readonly Sprint2StressConfig _config;

        public Sprint2StressRunner(Sprint2StressConfig config = null)
        {
            _config = config ?? new Sprint2StressConfig();
        }

        public Sprint2StressReport Run()
        {
            var report = new Sprint2StressReport();
            var sw = Stopwatch.StartNew();

            for (var i = 0; i < _config.MatchCount; i++)
            {
                var seed = _config.BaseSeed + (ulong)i;
                report.MatchesStarted++;

                string failure;
                try
                {
                    failure = RunOneMatch(seed, report);
                }
                catch (InvariantViolationException ex)
                {
                    report.InvariantBreaks++;
                    failure = "InvariantViolationException: " + ex.Message;
                }
                catch (Exception ex)
                {
                    report.Stalls++;
                    failure = "Unhandled: " + ex.GetType().Name + ": " + ex.Message;
                }

                if (failure != null)
                {
                    if (!report.FirstFailedSeed.HasValue)
                    {
                        report.FirstFailedSeed = seed;
                        report.FirstFailureReason = failure;
                    }

                    if (_config.AbortOnFirstFailure)
                    {
                        break;
                    }

                    continue;
                }

                report.MatchesCompleted++;
            }

            sw.Stop();
            report.ElapsedMilliseconds = sw.ElapsedMilliseconds;
            return report;
        }

        public string RunOneMatch(ulong seed, Sprint2StressReport report = null)
        {
            report = report ?? new Sprint2StressReport();
            var catalog = CombatCatalog.Create();
            var session = new GameSession(catalog);
            var setup = new MatchSetup(
                seed,
                RuleConfig.AlphaDefaults(),
                catalog.ContentVersion,
                new PlayerSetup(PlayerId.Player0, MinimalCatalog.HeroA, MinimalCatalog.ClassA, CombatCatalog.Deck40()),
                new PlayerSetup(PlayerId.Player1, MinimalCatalog.HeroB, MinimalCatalog.ClassB, CombatCatalog.Deck40()));

            var start = session.Start(setup);
            if (!start.Accepted)
            {
                report.UnexpectedRejections++;
                return "Start rejected: " + start.RejectionReason;
            }

            report.CommandsAccepted++;
            var decision = (seed & 1UL) == 0UL ? MulliganDecision.KeepHand : MulliganDecision.ReplaceWholeHand;
            if (!session.ConfirmMulligan(PlayerId.Player0, decision).Accepted ||
                !session.ConfirmMulligan(PlayerId.Player1, decision).Accepted)
            {
                report.UnexpectedRejections++;
                return "Mulligan rejected seed " + seed;
            }

            report.CommandsAccepted += 2;
            var commands = 3;
            var turns = 0;

            while (session.State.Phase != MatchPhase.GameOver && turns < _config.MaxTurns)
            {
                if (commands >= _config.MaxCommandsPerMatch)
                {
                    report.Stalls++;
                    return "Seed " + seed + ": MaxCommandsPerMatch";
                }

                if (session.State.Phase == MatchPhase.PendingChoice)
                {
                    var interceptFail = SubmitNextIntercept(session, seed, report, ref commands);
                    if (interceptFail != null)
                    {
                        return interceptFail;
                    }

                    continue;
                }

                if (session.State.Phase != MatchPhase.Main)
                {
                    report.Stalls++;
                    return "Seed " + seed + ": unexpected phase " + session.State.Phase;
                }

                var acted = TrySubmitPreferred(session, seed + (ulong)commands, report, ref commands);
                if (acted != null)
                {
                    return acted;
                }

                turns++;
            }

            report.AddFinalHash(GameStateHasher.Hash(session.State));
            return null;
        }

        private static string TrySubmitPreferred(
            GameSession session,
            ulong pickSeed,
            Sprint2StressReport report,
            ref int commands)
        {
            var legal = LegalActionQuery.For(session);
            if (legal.Count == 0)
            {
                report.Stalls++;
                return "no legal actions";
            }

            LegalAction chosen = null;
            var preference = (int)(pickSeed % 4UL);
            if (preference == 0)
            {
                chosen = FirstOfType(legal, CommandType.Play);
            }
            else if (preference == 1)
            {
                chosen = FirstOfType(legal, CommandType.Attack);
            }
            else if (preference == 2)
            {
                chosen = FirstOfType(legal, CommandType.Reorganize);
            }

            if (chosen == null)
            {
                chosen = FirstOfType(legal, CommandType.EndTurn) ?? legal[0];
            }

            var result = SubmitAction(session, chosen);
            if (!result.Accepted)
            {
                report.UnexpectedRejections++;
                return "rejection: " + result.RejectionReason;
            }

            commands++;
            report.CommandsAccepted++;
            if (chosen.Type == CommandType.Play)
            {
                report.PlaysAccepted++;
            }
            else if (chosen.Type == CommandType.Attack)
            {
                report.AttacksAccepted++;
            }
            else if (chosen.Type == CommandType.Reorganize)
            {
                report.ReorgsAccepted++;
            }

            return null;
        }

        private static string SubmitNextIntercept(
            GameSession session,
            ulong seed,
            Sprint2StressReport report,
            ref int commands)
        {
            var legal = LegalActionQuery.For(session);
            LegalAction chosen;
            if ((seed & 2UL) == 0UL)
            {
                chosen = FirstOfType(legal, CommandType.PassPriority) ?? FirstOfType(legal, CommandType.ChooseTargets);
            }
            else
            {
                chosen = FirstOfType(legal, CommandType.ChooseTargets) ?? FirstOfType(legal, CommandType.PassPriority);
            }

            if (chosen == null)
            {
                report.Stalls++;
                return "PendingChoice with no intercept actions";
            }

            var result = SubmitAction(session, chosen);
            if (!result.Accepted)
            {
                report.UnexpectedRejections++;
                return "intercept rejection: " + result.RejectionReason;
            }

            commands++;
            report.CommandsAccepted++;
            report.InterceptsResolved++;
            return null;
        }

        private static CommandResult SubmitAction(GameSession session, LegalAction action)
        {
            var actor = action.ActorId;
            switch (action.Type)
            {
                case CommandType.Play:
                    if (!action.SecondaryInstanceId.IsNone)
                    {
                        return session.PlayReplace(actor, action.PrimaryInstanceId, action.SecondaryInstanceId);
                    }

                    return session.Play(
                        actor, action.PrimaryInstanceId, action.TargetRow, action.TargetSlotIndex);
                case CommandType.Reorganize:
                    if (action.HasReorganizeMode && action.ReorganizeMode == ReorganizeMode.SwapRows)
                    {
                        return session.ReorganizeSwap(actor, action.PrimaryInstanceId, action.SecondaryInstanceId);
                    }

                    return session.ReorganizeMove(
                        actor, action.PrimaryInstanceId, action.TargetRow, action.TargetSlotIndex);
                case CommandType.Attack:
                    if (action.TargetsHero)
                    {
                        return session.AttackHero(actor, action.PrimaryInstanceId);
                    }

                    return session.Attack(actor, action.PrimaryInstanceId, action.SecondaryInstanceId);
                case CommandType.ChooseTargets:
                    return session.ChooseInterceptor(actor, action.PrimaryInstanceId);
                case CommandType.PassPriority:
                    return session.DeclineInterception(actor);
                case CommandType.EndTurn:
                    return session.EndTurn(actor);
                default:
                    return CommandResult.Reject(
                        session.State,
                        CommandRejectionCode.CommandNotSupported,
                        "unsupported legal action " + action.Type);
            }
        }

        private static LegalAction FirstOfType(System.Collections.Generic.IReadOnlyList<LegalAction> legal, CommandType type)
        {
            for (var i = 0; i < legal.Count; i++)
            {
                if (legal[i].Type == type)
                {
                    return legal[i];
                }
            }

            return null;
        }
    }
}
