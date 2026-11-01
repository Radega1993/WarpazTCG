using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Warpaz.Tests.EditMode.UIValidation
{
    /// <summary>
    /// Smoke documentation tests for S2-UI-VALIDATION-01. Do not gate product defects.
    /// </summary>
    public sealed class UiValidationFolderSmokeTests
    {
        [Test]
        public void ArtifactsFolder_Exists_ForValidationRun()
        {
            var root = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "Artifacts/UIValidation/S2_UI_VALIDATION_v1"));
            Assert.IsTrue(Directory.Exists(root), "Expected artifacts root at " + root);
            Assert.IsTrue(Directory.Exists(Path.Combine(root, "screenshots", "1920x1080")));
            Assert.IsTrue(Directory.Exists(Path.Combine(root, "evidence")));
        }
    }
}
