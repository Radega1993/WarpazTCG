using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Warpaz.Application;
using Warpaz.Domain;
using Warpaz.Presentation;

namespace Warpaz.Tests.EditMode
{
    public sealed class HandPaginationTests
    {
        [Test] public void ThirteenthCardCanBeClickedAndHiddenHandIsCleared()
        {
            var state = CombatTestHelpers.PlayToMain(); var player = state.GetPlayer(state.ActivePlayerId);
            while (player.Hand.Count < 13)
            {
                var id = player.Deck[0]; player.Deck.RemoveAt(0); player.Hand.Add(id); state.Cards[id].SetZone(CardZone.Hand);
            }
            var go = new GameObject("Hand", typeof(RectTransform));
            try
            {
                var hand = go.AddComponent<HandView>(); CardInstanceId clicked = default;
                hand.Bind(id => clicked = id);
                var view = GameStateViewProjector.Project(state, player.PlayerId);
                hand.Render(view, true, MatchFixtures.Catalog, true); hand.ChangePage(1);
                Assert.AreEqual(1, hand.CurrentPage);
                var card = go.transform.Find("HandCard4").GetComponent<CardPlaceholderView>();
                Assert.IsTrue(card.gameObject.activeSelf); card.GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual(player.Hand[12], clicked);
                hand.Render(view, false, MatchFixtures.Catalog, false); hand.ChangePage(1);
                Assert.AreEqual(0, hand.CurrentPage);
                Assert.IsFalse(go.transform.Find("HandPages").gameObject.activeSelf);
                Assert.IsTrue(go.GetComponentsInChildren<CardPlaceholderView>(true).All(c => !c.gameObject.activeSelf));
                Assert.IsTrue(go.GetComponentsInChildren<CardPlaceholderView>(true).All(c => !c.GetVisibleTextSnapshot().Contains("WZ")));
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
