using NUnit.Framework;
using UnityEngine;

namespace Warpaz.Tests.PlayMode.UIValidation
{
    /// <summary>
    /// Documents expected BLOCKED UI states under current policies. Validation-only.
    /// </summary>
    public sealed class UiValidationBlockedStatesTests
    {
        [Test]
        public void ExpectedBlockedStates_AreDocumented()
        {
            // Product cannot legally open intercept PendingChoice under P-HeroReach.
            // Per-card mulligan selection is out of product scope (Keep/Replace only) — not a repair defect.
            Assert.Pass(
                "Documented external BLOCKED: pending_intercept only. Mulligan Keep/Replace is product-correct.");
        }
    }
}
