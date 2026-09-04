using System;
using NUnit.Framework;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    public sealed class BoardSlotTests
    {
        [Test]
        public void BoardSlot_RepresentsVanguardAndRearguard()
        {
            var frontLeft = BoardSlot.Vanguard(0);
            var frontRight = BoardSlot.Vanguard(1, new CardInstanceId(7));
            var backLeft = BoardSlot.Rearguard(0);
            var backRight = BoardSlot.Rearguard(1);

            Assert.AreEqual(BoardRow.Vanguard, frontLeft.Row);
            Assert.AreEqual(0, frontLeft.Index);
            Assert.IsFalse(frontLeft.IsOccupied);
            Assert.IsTrue(frontLeft.IsVanguard);

            Assert.AreEqual(BoardRow.Vanguard, frontRight.Row);
            Assert.AreEqual(1, frontRight.Index);
            Assert.IsTrue(frontRight.IsOccupied);
            Assert.AreEqual(new CardInstanceId(7), frontRight.Occupant);

            Assert.AreEqual(BoardRow.Rearguard, backLeft.Row);
            Assert.IsTrue(backLeft.IsRearguard);
            Assert.AreEqual(1, backRight.Index);
            Assert.AreNotEqual(frontLeft, backLeft);
        }

        [Test]
        public void BoardSlot_RejectsIndexOutsideZeroOrOne()
        {
            var error = Assert.Throws<ArgumentOutOfRangeException>(() => BoardSlot.Vanguard(2));
            StringAssert.Contains("0 or 1", error.Message);
            Assert.Throws<ArgumentOutOfRangeException>(() => BoardSlot.Rearguard(-1));
        }

        [Test]
        public void PlayerState_StartsWithFourEmptyCharacterSlots()
        {
            var player = new PlayerState(
                PlayerId.Player0,
                new CardId("WZ1-001"),
                new CardId("WZ1-002"),
                new CardInstanceId(1),
                new CardInstanceId(2),
                RuleConfig.AlphaDefaults());

            Assert.AreEqual(BoardSlot.TotalCharacterSlots, player.BoardSlots.Count);
            Assert.AreEqual(BoardRow.Vanguard, player.BoardSlots[0].Row);
            Assert.AreEqual(BoardRow.Vanguard, player.BoardSlots[1].Row);
            Assert.AreEqual(BoardRow.Rearguard, player.BoardSlots[2].Row);
            Assert.AreEqual(BoardRow.Rearguard, player.BoardSlots[3].Row);
        }
    }
}
