using NUnit.Framework;
using PerspectivePuzzle.Level1;

namespace PerspectivePuzzle.EditMode
{
    public class CabinetBlocksTests
    {
        [Test]
        public void CassetteIsReachableOnlyAtTheSolvedTurns()
        {
            Assert.IsFalse(CabinetBlocks.Solved(0, 0));
            Assert.IsFalse(CabinetBlocks.Solved(1, 0));
            Assert.IsFalse(CabinetBlocks.Solved(0, 3));
            Assert.IsTrue(CabinetBlocks.Solved(CabinetBlocks.SolutionA, CabinetBlocks.SolutionB));
            Assert.IsTrue(CabinetBlocks.Solved(5, -1));
            Assert.AreEqual(0, CabinetBlocks.Normalize(4));
            Assert.AreEqual(3, CabinetBlocks.Normalize(-1));
        }
    }
}
