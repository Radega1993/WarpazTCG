using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warpaz.Domain;

namespace Warpaz.Presentation
{
    /// <summary>Local two-player starter selection. Does not mutate decks or rules.</summary>
    public sealed class StarterDeckPicker : MonoBehaviour
    {
        public static void Show(Transform parent, IReadOnlyList<DeckList> decks, Action<int, int> start)
        {
            var old = parent.Find("StarterDeckPicker");
            if (old != null) Destroy(old.gameObject);
            var go = new GameObject("StarterDeckPicker", typeof(RectTransform), typeof(Image), typeof(StarterDeckPicker));
            go.transform.SetParent(parent, false);
            go.transform.SetAsLastSibling();
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0.06f, 0.07f, 0.09f, 0.99f);
            var selected = new[] { 0, 1 };
            Text(go.transform,"Starters WZ1 · Alpha",0.1f,0.84f,0.9f,0.95f,32);
            Text(go.transform,"40 personajes + Héroe + Clase por jugador.\nModo combate básico: NO se ejecutan habilidades impresas, palabras clave ni habilidades de identidad.",0.1f,0.68f,0.9f,0.83f,20);
            for (var seat = 0; seat < 2; seat++)
            {
                var player = seat;
                var x = seat == 0 ? 0.08f : 0.53f;
                Text(go.transform,"Jugador " + (seat + 1),x,0.58f,x+0.39f,0.66f,24);
                TMP_Text label = null;
                label = Button(go.transform,"",x,0.41f,x+0.39f,0.56f,()=> {
                    selected[player] = (selected[player] + 1) % decks.Count;
                    label.text = decks[selected[player]].DisplayName + "  ›";
                });
                label.text = decks[selected[player]].DisplayName + "  ›";
            }
            Text(go.transform,"Pulsa cada mazo para cambiarlo. Ambos jugadores pueden elegir el mismo.\nLas cartas conservan su texto de colección, todavía no funcional en esta alpha.",0.1f,0.26f,0.9f,0.38f,18);
            Button(go.transform,"Volver",0.12f,0.10f,0.40f,0.21f,()=>Destroy(go));
            Button(go.transform,"Empezar partida",0.60f,0.10f,0.88f,0.21f,()=>{start(selected[0],selected[1]); Destroy(go);});
        }

        private static TMP_Text Text(Transform parent,string value,float x0,float y0,float x1,float y1,int size)
        {
            var go = new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));
            go.transform.SetParent(parent,false);
            Place((RectTransform)go.transform,x0,y0,x1,y1);
            var text=go.GetComponent<TextMeshProUGUI>(); text.text=value; text.fontSize=size;
            text.enableAutoSizing=true; text.fontSizeMin=12; text.fontSizeMax=size;
            text.alignment=TextAlignmentOptions.Center; text.color=BattleUiColors.HudText; text.raycastTarget=false;
            return text;
        }
        private static TMP_Text Button(Transform parent,string label,float x0,float y0,float x1,float y1,Action click)
        {
            var go=new GameObject("Button",typeof(RectTransform),typeof(Image),typeof(Button));
            go.transform.SetParent(parent,false); Place((RectTransform)go.transform,x0,y0,x1,y1);
            var image=go.GetComponent<Image>(); image.color=BattleUiColors.ActionButton;
            var button=go.GetComponent<Button>(); button.targetGraphic=image; button.onClick.AddListener(()=>click());
            var text=Text(go.transform,label,0.04f,0.08f,0.96f,0.92f,23); text.color=BattleUiColors.ActionButtonText;
            return text;
        }
        private static void Place(RectTransform r,float x0,float y0,float x1,float y1)
        {r.anchorMin=new Vector2(x0,y0); r.anchorMax=new Vector2(x1,y1); r.offsetMin=r.offsetMax=Vector2.zero;}
    }
}
