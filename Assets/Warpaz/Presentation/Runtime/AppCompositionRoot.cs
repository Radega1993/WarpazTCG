using UnityEngine;
using UnityEngine.SceneManagement;

namespace Warpaz.Presentation
{
    /// <summary>
    /// Composition root created by Bootstrap. Lives in DontDestroyOnLoad and
    /// injects <see cref="AppRuntime"/> into each loaded scene. Menus never look it up.
    /// </summary>
    public sealed class AppCompositionRoot : MonoBehaviour, ISceneFlow, IBattleStartProvider
    {
        public AppRuntime Runtime { get; private set; }

        private BattleStartPayload _pendingBattleStart;

        public void Initialize(AppRuntime runtime)
        {
            Runtime = runtime;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public void OpenMainMenu()
        {
            SceneManager.LoadScene(SceneIds.MainMenu);
        }

        public void OpenBattle()
        {
            SceneManager.LoadScene(SceneIds.Battle);
        }

        public void OpenLaboratory()
        {
            SceneManager.LoadScene(SceneIds.Laboratory);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public void SetPendingBattleStart(BattleStartPayload payload)
        {
            _pendingBattleStart = payload;
        }

        public BattleStartPayload ConsumePendingBattleStart()
        {
            var payload = _pendingBattleStart;
            _pendingBattleStart = null;
            return payload;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Inject(scene);
        }

        private void Inject(Scene scene)
        {
            if (Runtime == null || !scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var clients = roots[i].GetComponentsInChildren<MonoBehaviour>(true);
                for (var c = 0; c < clients.Length; c++)
                {
                    var client = clients[c] as IAppRuntimeClient;
                    if (client != null)
                    {
                        client.Bind(Runtime, this);
                    }
                }
            }
        }
    }
}
