using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Warpaz.Presentation
{
    public sealed class StubSceneController : MonoBehaviour, IAppRuntimeClient
    {
        [SerializeField] private UiTheme theme;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private string screenTitle;
        [SerializeField] private Button backButton;

        private ISceneFlow _flow;

        public void Bind(AppRuntime runtime, ISceneFlow flow)
        {
            _flow = flow;
            if (titleText != null)
            {
                titleText.text = string.IsNullOrEmpty(screenTitle) ? gameObject.scene.name : screenTitle;
            }

            if (theme != null)
            {
                theme.ApplyText(titleText);
                if (backButton != null)
                {
                    theme.ApplyButton(backButton, backButton.GetComponentInChildren<TMP_Text>(true));
                }
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(OnBack);
            }
        }

        private void OnBack()
        {
            if (_flow != null)
            {
                _flow.OpenMainMenu();
            }
        }
    }
}
