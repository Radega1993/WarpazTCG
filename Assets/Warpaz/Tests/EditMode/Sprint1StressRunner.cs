using System;
using System.Diagnostics;
using Warpaz.Application;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    /// <summary>
    /// EditMode-only stress runner for Sprint 1. No bot, no card plays, no scenes.
    /// </summary>
    public sealed class Sprint1StressRunner
    {
        private readonly Sprint1StressConfig _config;

        public Sprint1StressRunner(Sprint1StressConfig config = null)
        {
            _config = config ?? new Sprint1StressConfig();
        }

        public Sprint1StressReport Run()
        {
            var report = new Sprint1StressReport();
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
                    failure = "Unhandled exception: " + ex.GetType().Name + ": " + ex.Message;
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

        /// <summary>
        /// Runs a single seed. Returns null on success, or a failure reason.
        /// </summary>
        public string RunOneMatch(ulong seed, Sprint1StressReport report = null)
        {
            report = report ?? new Sprint1StressReport();

            var session = new GameSession(Wz1MatchFixtures.Catalog);
            var start = session.Start(Wz1MatchFixtures.Standard(seed));
            if (!start.Accepted)
            {
                report.UnexpectedRejections++;
                return "Start rejected for seed " + seed + ": " + start.RejectionReason;
            }

            var commandsThisMatch = 1;
            report.CommandsAccepted++;
            var prevEventIndex = 0;
            var phaseTransitions = 0;
            var lastPhase = session.State.Phase;

            var postStart = AfterAccepted(session, report, ref prevEventIndex, ref phaseTransitions, ref lastPhase, seed);
            if (postStart != null)
            {
                return postStart;
            }

            var decision = Sprint1StressConfig.MulliganForSeed(seed);

            var m0 = SubmitLegal(session, report, ref commandsThisMatch,
                () => session.ConfirmMulligan(PlayerId.Player0, decision));
            if (m0 != null)
            {
                return "Seed " + seed + " mulligan P0: " + m0;
            }

            var afterM0 = AfterAccepted(session, report, ref prevEventIndex, ref phaseTransitions, ref lastPhase, seed);
            if (afterM0 != null)
            {
                return afterM0;
            }

            var m1 = SubmitLegal(session, report, ref commandsThisMatch,
                () => session.ConfirmMulligan(PlayerId.Player1, decision));
            if (m1 != null)
            {
                return "Seed " + seed + " mulligan P1: " + m1;
            }

            var afterM1 = AfterAccepted(session, report, ref prevEventIndex, ref phaseTransitions, ref lastPhase, seed);
            if (afterM1 != null)
            {
                return afterM1;
            }

            var endTurns = 0;
            while (session.State.Phase != MatchPhase.GameOver && endTurns < _config.MaxTurns)
            {
                if (commandsThisMatch >= _config.MaxCommandsPerMatch)
                {
                    report.Stalls++;
                    return "Seed " + seed + ": exceeded MaxCommandsPerMatch (" +
                           _config.MaxCommandsPerMatch + ").";
                }

                if (session.State.EventIndex > _config.MaxEventsPerMatch)
                {
                    report.Stalls++;
                    return "Seed " + seed + ": exceeded MaxEventsPerMatch (" +
                           _config.MaxEventsPerMatch + ").";
                }

                if (phaseTransitions > _config.MaxPhaseTransitionsPerMatch)
                {
                    report.Stalls++;
                    return "Seed " + seed + ": exceeded MaxPhaseTransitionsPerMatch (" +
                           _config.MaxPhaseTransitionsPerMatch + ").";
                }

                if (session.State.Phase != MatchPhase.Main)
                {
                    var stall = DetectStall(session.State);
                    if (stall != null)
                    {
                        report.Stalls++;
                        return "Seed " + seed + ": " + stall;
                    }

                    return "Seed " + seed + ": unexpected phase " + session.State.Phase +
                           " before EndTurn loop.";
                }

                var actor = session.State.ActivePlayerId;
                var et = SubmitLegal(session, report, ref commandsThisMatch, () => session.EndTurn(actor));
                if (et != null)
                {
                    return "Seed " + seed + " EndTurn: " + et;
                }

                endTurns++;
                var afterEt = AfterAccepted(session, report, ref prevEventIndex, ref phaseTransitions, ref lastPhase, seed);
                if (afterEt != null)
                {
                    return afterEt;
                }
            }

            report.AddFinalHash(GameStateHasher.Hash(session.State));
            return null;
        }

        private static string SubmitLegal(
            GameSession session,
            Sprint1StressReport report,
            ref int commandsThisMatch,
            Func<CommandResult> submit)
        {
            var legal = LegalActionQuery.For(session.State);
            if (legal.Count == 0)
            {
                report.Stalls++;
                return "no legal actions in phase " + session.State.Phase;
            }

            CommandResult result;
            try
            {
                result = submit();
            }
            catch (InvariantViolationException)
            {
                throw;
            }

            if (!result.Accepted)
            {
                report.UnexpectedRejections++;
                return "unexpected rejection (" + result.Code + "): " + result.RejectionReason;
            }

            commandsThisMatch++;
            report.CommandsAccepted++;
            return null;
        }

        private static string AfterAccepted(
            GameSession session,
            Sprint1StressReport report,
            ref int prevEventIndex,
            ref int phaseTransitions,
            ref MatchPhase lastPhase,
            ulong seed)
        {
            if (session.State.Phase != lastPhase)
            {
                phaseTransitions++;
                lastPhase = session.State.Phase;
            }

            var violation = Sprint1StateAssertions.FindViolation(session.State, prevEventIndex);
            if (violation != null)
            {
                report.InvariantBreaks++;
                return "Seed " + seed + " invariant: " + violation;
            }

            prevEventIndex = session.State.EventIndex;
            return null;
        }

        private static string DetectStall(GameState state)
        {
            if (state.Phase == MatchPhase.GameOver
                || state.Phase == MatchPhase.Main
                || state.Phase == MatchPhase.MulliganP0
                || state.Phase == MatchPhase.MulliganP1)
            {
                return null;
            }

            var legal = LegalActionQuery.For(state);
            if (legal.Count == 0)
            {
                return "stall in phase " + state.Phase + " with no legal actions";
            }

            return null;
        }
    }
}
