using System;
using System.Collections.Generic;

namespace Warpaz.Domain
{
    /// <summary>
    /// Choice the engine is waiting on. <see cref="None"/> means the match is not blocked on input.
    /// </summary>
    public sealed class PendingDecision
    {
        public static readonly PendingDecision None = new PendingDecision(
            PendingDecisionKind.None,
            PlayerId.Host,
            CardInstanceId.None,
            CardInstanceId.None,
            false,
            Array.Empty<CardInstanceId>());

        private PendingDecision(
            PendingDecisionKind kind,
            PlayerId actorId,
            CardInstanceId attackerId,
            CardInstanceId declaredTargetId,
            bool targetsHero,
            IReadOnlyList<CardInstanceId> legalInterceptorIds)
        {
            Kind = kind;
            ActorId = actorId;
            AttackerId = attackerId;
            DeclaredTargetId = declaredTargetId;
            TargetsHero = targetsHero;
            LegalInterceptorIds = legalInterceptorIds ?? Array.Empty<CardInstanceId>();
        }

        public PendingDecisionKind Kind { get; }

        public PlayerId ActorId { get; }

        public CardInstanceId AttackerId { get; }

        public CardInstanceId DeclaredTargetId { get; }

        public bool TargetsHero { get; }

        public IReadOnlyList<CardInstanceId> LegalInterceptorIds { get; }

        public bool IsPending => Kind != PendingDecisionKind.None;

        public static PendingDecision Intercept(
            PlayerId defenderId,
            CardInstanceId attackerId,
            CardInstanceId declaredTargetId,
            bool targetsHero,
            IReadOnlyList<CardInstanceId> legalInterceptorIds)
        {
            if (!defenderId.IsSeated)
            {
                throw new ArgumentException("Intercept decision requires a seated defender.", nameof(defenderId));
            }

            if (attackerId.IsNone)
            {
                throw new ArgumentException("Intercept decision requires an attacker.", nameof(attackerId));
            }

            if (legalInterceptorIds == null || legalInterceptorIds.Count == 0)
            {
                throw new ArgumentException(
                    "Intercept decision requires at least one legal interceptor.",
                    nameof(legalInterceptorIds));
            }

            return new PendingDecision(
                PendingDecisionKind.ChooseInterceptor,
                defenderId,
                attackerId,
                declaredTargetId,
                targetsHero,
                legalInterceptorIds);
        }
    }
}
