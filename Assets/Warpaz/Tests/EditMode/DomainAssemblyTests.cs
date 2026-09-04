using NUnit.Framework;
using System.Reflection;
using Warpaz.Application;
using Warpaz.Content;
using Warpaz.Domain;
using Warpaz.Infrastructure;

namespace Warpaz.Tests.EditMode
{
    public sealed class DomainAssemblyTests
    {
        [Test]
        public void Domain_DoesNotReferenceApplication()
        {
            AssertNoAssembly(typeof(GameState).Assembly, "Warpaz.Application");
        }

        [Test]
        public void Application_DoesNotReferencePresentation()
        {
            AssertNoAssembly(typeof(GameSession).Assembly, "Warpaz.Presentation");
        }

        [Test]
        public void Domain_DoesNotReferenceUnityEngine()
        {
            AssertNoUnity(typeof(GameState).Assembly, "Warpaz.Domain");
        }

        [Test]
        public void Application_DoesNotReferenceUnityEngine()
        {
            AssertNoUnity(typeof(GameSession).Assembly, "Warpaz.Application");
        }

        [Test]
        public void Content_DoesNotReferenceUnityEngine()
        {
            AssertNoUnity(typeof(CardCatalogLoader).Assembly, "Warpaz.Content");
        }

        [Test]
        public void Infrastructure_DoesNotReferenceUnityEngine()
        {
            AssertNoUnity(typeof(Utf8Files).Assembly, "Warpaz.Infrastructure");
        }

        private static void AssertNoAssembly(Assembly assembly, string forbiddenName)
        {
            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                Assert.AreNotEqual(
                    forbiddenName,
                    reference.Name,
                    assembly.GetName().Name + " referenced " + forbiddenName);
            }
        }

        private static void AssertNoUnity(Assembly assembly, string expectedName)
        {
            Assert.AreEqual(expectedName, assembly.GetName().Name);

            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                Assert.IsFalse(
                    reference.Name.StartsWith("UnityEngine"),
                    expectedName + " referenced " + reference.Name);
                Assert.IsFalse(
                    reference.Name.StartsWith("UnityEditor"),
                    expectedName + " referenced " + reference.Name);
            }
        }
    }
}
