using NUnit.Framework;
using UnityEngine;
using Warpaz.Presentation;

namespace Warpaz.Tests.EditMode
{
    public sealed class ModernCardLayoutTests
    {
        [TestCase(100, 300)]
        [TestCase(400, 160)]
        [TestCase(148, 208)]
        public void Fit_PreservesCardRatio_InsideAvailableSpace(float width, float height)
        {
            var fit = CardLayout.Fit(width, height);
            Assert.That(fit.x / fit.y, Is.EqualTo(CardLayout.Aspect).Within(0.0001f));
            Assert.LessOrEqual(fit.x, width + 0.001f);
            Assert.LessOrEqual(fit.y, height + 0.001f);
        }

        [Test]
        public void TextLength_DoesNotMoveArtworkWindow()
        {
            var go = new GameObject("Card", typeof(RectTransform));
            try
            {
                var card = go.AddComponent<CardPlaceholderView>();
                card.SetFaceUp("Short", "Personaje", "1", "P2", rules: "Breve.");
                var art = (RectTransform)go.transform.Find("EmptyContentArea");
                var min = art.anchorMin; var max = art.anchorMax;
                card.SetFaceUp("Long", "Personaje", "1", "P2", rules: new string('X', 250));
                Assert.AreEqual(min, art.anchorMin);
                Assert.AreEqual(max, art.anchorMax);
                var rules = (RectTransform)go.transform.Find("RulesPanel");
                Assert.Less(rules.anchorMax.y, art.anchorMin.y);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Rebinding_PreservesHighlights_AndFaceDownClearsThem()
        {
            var go = new GameObject("Card", typeof(RectTransform));
            try
            {
                var card = go.AddComponent<CardPlaceholderView>();
                card.SetFaceUp("Secret name", "Personaje", "3", "P5");
                card.SetSelected(true); card.SetPlayable(true); card.Bind(() => { }); card.SetInteractable(true);
                Assert.IsTrue(go.transform.Find("SelectedOverlay").gameObject.activeSelf);
                Assert.IsTrue(go.transform.Find("LegalOverlay").gameObject.activeSelf);
                card.SetFaceDown(); card.EnsureChrome();
                Assert.IsFalse(go.transform.Find("SelectedOverlay").gameObject.activeSelf);
                Assert.IsFalse(go.transform.Find("LegalOverlay").gameObject.activeSelf);
                StringAssert.DoesNotContain("Secret", card.GetVisibleTextSnapshot());
                Assert.AreEqual(Vector3.one, go.transform.localScale);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
