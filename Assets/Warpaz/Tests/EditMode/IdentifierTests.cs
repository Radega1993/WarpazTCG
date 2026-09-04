using System;
using NUnit.Framework;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    public sealed class IdentifierTests
    {
        [Test]
        public void CardId_AcceptsWzPattern()
        {
            Assert.IsTrue(CardId.IsValid("WZ1-001"));
            Assert.IsTrue(CardId.IsValid("WZ1-120"));
            Assert.IsTrue(CardId.IsValid("WZ0-011"));
            Assert.IsTrue(CardId.IsValid("WZ10-003"));
            Assert.AreEqual("WZ1-001", new CardId("WZ1-001").Value);
        }

        [Test]
        public void CardId_RejectsInvalidValues()
        {
            Assert.IsFalse(CardId.IsValid(null));
            Assert.IsFalse(CardId.IsValid(string.Empty));
            Assert.IsFalse(CardId.IsValid("WZ1-12"));
            Assert.IsFalse(CardId.IsValid("WZ1-0001"));
            Assert.IsFalse(CardId.IsValid("WZ-001"));
            Assert.IsFalse(CardId.IsValid("ABC-001"));
            Assert.IsFalse(CardId.IsValid("TEST-HERO-A"));
            Assert.IsFalse(CardId.IsValid("Hero A"));
            Assert.Throws<ArgumentException>(() => new CardId("Hero A"));
            Assert.Throws<ArgumentException>(() => new CardId("WZ1-12"));
        }

        [Test]
        public void CardId_Equality_IsByValue()
        {
            var a = new CardId("WZ1-001");
            var b = new CardId("WZ1-001");
            var c = new CardId("WZ1-002");
            Assert.AreEqual(a, b);
            Assert.IsTrue(a == b);
            Assert.AreNotEqual(a, c);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void CardInstanceId_RejectsNonPositive_AndEqualsByValue()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CardInstanceId(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CardInstanceId(-3));
            var a = new CardInstanceId(4);
            var b = new CardInstanceId(4);
            Assert.IsTrue(a == b);
            Assert.AreNotEqual(a, new CardInstanceId(5));
            Assert.IsTrue(CardInstanceId.None.IsNone);
        }

        [Test]
        public void PlayerId_AcceptsSeatsAndHost_RejectsOthers()
        {
            Assert.AreEqual(0, PlayerId.Player0.Value);
            Assert.AreEqual(1, PlayerId.Player1.Value);
            Assert.IsTrue(PlayerId.Host.IsHost);
            Assert.AreEqual(PlayerId.Player1, PlayerId.Player0.Opponent);
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerId(2));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerId(-2));
            Assert.IsTrue(PlayerId.FromIndex(0) == PlayerId.Player0);
        }

        [Test]
        public void CommandId_RejectsBlank_AndEqualsByValue()
        {
            Assert.Throws<ArgumentException>(() => new CommandId(" "));
            Assert.Throws<ArgumentException>(() => new CommandId(null));
            var a = CommandId.Parse("cmd-1");
            var b = new CommandId("cmd-1");
            Assert.IsTrue(a == b);
            Assert.AreNotEqual(a, new CommandId("cmd-2"));
        }
    }
}
