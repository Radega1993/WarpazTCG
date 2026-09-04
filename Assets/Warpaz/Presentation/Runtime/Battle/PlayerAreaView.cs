using TMPro;
using UnityEngine;
using Warpaz.Application;
using Warpaz.Domain;

namespace Warpaz.Presentation
{
    public sealed class PlayerAreaView : MonoBehaviour
    {
        private TMP_Text _priorityName;
        private TMP_Text _priorityHero;
        private TMP_Text _priorityClass;
        private TMP_Text _priorityResolution;
        private TMP_Text _priorityPa;
        private TMP_Text _priorityHandCount;
        private TMP_Text _priorityDeckCount;
        private TMP_Text _priorityDiscardCount;

        private TMP_Text _rivalName;
        private TMP_Text _rivalHero;
        private TMP_Text _rivalClass;
        private TMP_Text _rivalResolution;
        private TMP_Text _rivalPa;
        private TMP_Text _rivalHandCount;
        private TMP_Text _rivalDeckCount;
        private TMP_Text _rivalDiscardCount;

        private void Awake()
        {
            var container = transform as RectTransform;
            if (container != null)
            {
                container.anchoredPosition = new Vector2(0, 260f);
            }

            _priorityName = Find("PriorityNameText");
            _priorityHero = Find("PriorityHeroText");
            _priorityClass = Find("PriorityClassText");
            _priorityResolution = Find("PriorityResolutionText");
            _priorityPa = Find("PriorityPaText");
            _priorityHandCount = Find("PriorityHandCountText");
            _priorityDeckCount = Find("PriorityDeckCountText");
            _priorityDiscardCount = Find("PriorityDiscardCountText");

            _rivalName = Find("RivalNameText");
            _rivalHero = Find("RivalHeroText");
            _rivalClass = Find("RivalClassText");
            _rivalResolution = Find("RivalResolutionText");
            _rivalPa = Find("RivalPaText");
            _rivalHandCount = Find("RivalHandCountText");
            _rivalDeckCount = Find("RivalDeckCountText");
            _rivalDiscardCount = Find("RivalDiscardCountText");

            var startY = 0f;
            var stepY = -24f;
            var xPriority = -240f;
            var xRival = 240f;

            // Columna prioridad.
            Set(_priorityName, xPriority, startY + stepY * 0);
            Set(_priorityHero, xPriority, startY + stepY * 1);
            Set(_priorityClass, xPriority, startY + stepY * 2);
            Set(_priorityResolution, xPriority, startY + stepY * 3);
            Set(_priorityPa, xPriority, startY + stepY * 4);
            Set(_priorityHandCount, xPriority, startY + stepY * 5);
            Set(_priorityDeckCount, xPriority, startY + stepY * 6);
            Set(_priorityDiscardCount, xPriority, startY + stepY * 7);

            // Columna rival.
            Set(_rivalName, xRival, startY + stepY * 0);
            Set(_rivalHero, xRival, startY + stepY * 1);
            Set(_rivalClass, xRival, startY + stepY * 2);
            Set(_rivalResolution, xRival, startY + stepY * 3);
            Set(_rivalPa, xRival, startY + stepY * 4);
            Set(_rivalHandCount, xRival, startY + stepY * 5);
            Set(_rivalDeckCount, xRival, startY + stepY * 6);
            Set(_rivalDiscardCount, xRival, startY + stepY * 7);
        }

        public void Render(
            MatchView view,
            PlayerId activePlayer,
            PlayerId prioritySeat,
            ICardCatalog catalog,
            string player0Name,
            string player1Name)
        {
            if (view == null)
            {
                return;
            }

            var priority = prioritySeat == PlayerId.Player0 ? view.Player0 : view.Player1;
            var rival = prioritySeat == PlayerId.Player0 ? view.Player1 : view.Player0;

            var priorityName = prioritySeat == PlayerId.Player0 ? player0Name : player1Name;
            var rivalName = prioritySeat == PlayerId.Player0 ? player1Name : player0Name;

            _priorityName.text = priorityName;
            _priorityResolution.text = "Res " + priority.Resolution;
            _priorityPa.text = "PA " + priority.PaCurrent + "/" + priority.PaMax;
            _priorityHandCount.text = "Mano " + priority.HandCount;
            _priorityDeckCount.text = "Mazo " + priority.DeckCount;
            _priorityDiscardCount.text = "Desc " + priority.Discard.Count;

            _rivalName.text = rivalName;
            _rivalResolution.text = "Res " + rival.Resolution;
            _rivalPa.text = "PA " + rival.PaCurrent + "/" + rival.PaMax;
            _rivalHandCount.text = "Mano " + rival.HandCount;
            _rivalDeckCount.text = "Mazo " + rival.DeckCount;
            _rivalDiscardCount.text = "Desc " + rival.Discard.Count;

            _priorityHero.text = ResolveName(catalog, priority.HeroId);
            _priorityClass.text = ResolveName(catalog, priority.ClassPathId);
            _rivalHero.text = ResolveName(catalog, rival.HeroId);
            _rivalClass.text = ResolveName(catalog, rival.ClassPathId);
        }

        private TMP_Text Find(string path)
        {
            var t = transform.Find(path);
            return t != null ? t.GetComponent<TMP_Text>() : null;
        }

        private static string ResolveName(ICardCatalog catalog, CardId id)
        {
            if (catalog != null && catalog.TryGet(id, out var def) && def != null)
            {
                return def.Name;
            }

            return id.Value;
        }

        private static void Set(TMP_Text text, float x, float y)
        {
            if (text == null)
            {
                return;
            }

            var rt = text.rectTransform;
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(x, y);
            }
        }
    }
}
