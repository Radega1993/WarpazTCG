using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Warpaz.Content;
using Warpaz.Presentation;

namespace Warpaz.Tests.PlayMode
{
    public sealed class BootstrapFlowTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            var roots = Object.FindObjectsByType<AppCompositionRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < roots.Length; i++)
            {
                Object.Destroy(roots[i].gameObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Bootstrap_OpensMainMenu_With120Cards()
        {
            LogAssert.NoUnexpectedReceived();
            yield return SceneManager.LoadSceneAsync(SceneIds.Bootstrap);

            var timeout = Time.realtimeSinceStartup + 8f;
            while (SceneManager.GetActiveScene().name != SceneIds.MainMenu && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.AreEqual(SceneIds.MainMenu, SceneManager.GetActiveScene().name);
            var root = Object.FindFirstObjectByType<AppCompositionRoot>();
            Assert.IsNotNull(root, "Composition root was not created.");
            Assert.IsNotNull(root.Runtime);
            Assert.IsNotNull(root.Runtime.Catalog);
            Assert.AreEqual(ContentManifest.ExpectedCardCount, root.Runtime.Catalog.Count);
            Assert.AreEqual(ContentManifest.ExpectedContentVersion, root.Runtime.Manifest.ContentVersion);
            Assert.AreEqual("0.1.0", root.Runtime.Rules.RuleVersion);

            var menu = Object.FindFirstObjectByType<MainMenuController>();
            Assert.IsNotNull(menu);
            Assert.DoesNotThrow(() => menu.SimulateComingSoon());
            Assert.DoesNotThrow(() => menu.SimulatePlay());

            timeout = Time.realtimeSinceStartup + 8f;
            while (SceneManager.GetActiveScene().name != SceneIds.Battle && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.AreEqual(SceneIds.Battle, SceneManager.GetActiveScene().name);
        }
    }
}
