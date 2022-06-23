using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gedcom7;

namespace Tests
{
    [TestClass]
    public class ComparisonTests
    {
        void CompareFileWithSelf(string path)
        {
            var file = new GedcomFile();
            bool ok = file.Load(path);
            Assert.IsTrue(ok);

            GedcomComparisonReport report = file.Compare(file);
            Assert.AreEqual(report.StructuresAdded.Count, 0);
            Assert.AreEqual(report.StructuresRemoved.Count, 0);
            Assert.AreEqual(report.CompatibilityPercentage, 100);
        }

        [TestMethod]
        public void CompareMinimalWithSelf()
        {
            CompareFileWithSelf("../../../../external/GEDCOM.io/testfiles/gedcom70/minimal70.ged");
        }

        [TestMethod]
        public void CompareMaximalWithSelf()
        {
            CompareFileWithSelf("../../../../external/GEDCOM.io/testfiles/gedcom70/maximal70.ged");
        }

        private void CompareSubsetWithSuperset(string subset, string superset, int structuresAdded, int percentage)
        {
            var subsetFile = new GedcomFile();
            bool ok = subsetFile.Load("../../../../external/GEDCOM.io/testfiles/gedcom70/" + subset + ".ged");
            Assert.IsTrue(ok);

            var supersetFile = new GedcomFile();
            ok = supersetFile.Load("../../../../external/GEDCOM.io/testfiles/gedcom70/" + superset + ".ged");
            Assert.IsTrue(ok);

            // Adding information is ok.
            GedcomComparisonReport report = subsetFile.Compare(supersetFile);
            Assert.AreEqual(report.StructuresAdded.Count, structuresAdded);
            Assert.AreEqual(report.StructuresRemoved.Count, 0);
            Assert.AreEqual(report.CompatibilityPercentage, 100);

            // Losing information is not ok.
            report = supersetFile.Compare(subsetFile);
            Assert.AreEqual(report.StructuresAdded.Count, 0);
            Assert.AreEqual(report.StructuresRemoved.Count, structuresAdded);
            Assert.AreEqual(report.CompatibilityPercentage, percentage);
        }

        [TestMethod]
        public void CompareMinimalWithMaximal()
        {
            CompareSubsetWithSuperset("minimal70", "maximal70", 808, 3);
        }

        [TestMethod]
        public void CompareMinimalWithTree1()
        {
            CompareSubsetWithSuperset("minimal70", "maximal70-tree1", 51, 7);
        }

        [TestMethod]
        public void CompareTree1WithTree2()
        {
            CompareSubsetWithSuperset("maximal70-tree1", "maximal70-tree2", 108, 33);
        }

        [TestMethod]
        public void CompareTree2WithMaximal()
        {
            CompareSubsetWithSuperset("maximal70-tree1", "maximal70", 757, 9);
        }

        [TestMethod]
        public void CompareTree1WithLds()
        {
            CompareSubsetWithSuperset("maximal70-tree1", "maximal70-lds", 29, 65);
        }

        [TestMethod]
        public void CompareLdsWithMaximal()
        {
            CompareSubsetWithSuperset("maximal70-lds", "maximal70", 728, 13);
        }

        [TestMethod]
        public void CompareTree1WithMemories1()
        {
            CompareSubsetWithSuperset("maximal70-tree1", "maximal70-memories1", 10, 84);
        }

        [TestMethod]
        public void CompareMemories1WithMemories2()
        {
            CompareSubsetWithSuperset("maximal70-memories1", "maximal70-memories2", 8, 89);
        }

        [TestMethod]
        public void CompareMemories2WithMaximal()
        {
            CompareSubsetWithSuperset("maximal70-memories2", "maximal70", 739, 11);
        }

        [TestMethod]
        public void CompareNoteWithSharedNote()
        {
            var note = new GedcomFile();
            bool ok = note.Load("../../../samples/note.ged");
            Assert.IsTrue(ok);

            var snote = new GedcomFile();
            ok = snote.Load("../../../samples/snote.ged");
            Assert.IsTrue(ok);

            GedcomComparisonReport report = note.Compare(snote);
            Assert.AreEqual(report.StructuresAdded.Count, 0);
            Assert.AreEqual(report.StructuresRemoved.Count, 0);
            Assert.AreEqual(report.CompatibilityPercentage, 100);
        }

        [TestMethod]
        public void CompareSingleNamePiecesWithMultipleNamePieces()
        {
            var singles = new GedcomFile();
            bool ok = singles.Load("../../../samples/name-pieces-single.ged");
            Assert.IsTrue(ok);

            var multiples = new GedcomFile();
            ok = multiples.Load("../../../samples/name-pieces-multiple.ged");
            Assert.IsTrue(ok);

            // Verify that both compare equally.
            GedcomComparisonReport report = singles.Compare(multiples);
            Assert.AreEqual(report.StructuresAdded.Count, 0);
            Assert.AreEqual(report.StructuresRemoved.Count, 0);
            Assert.AreEqual(report.CompatibilityPercentage, 100);

            report = multiples.Compare(singles);
            Assert.AreEqual(report.StructuresAdded.Count, 0);
            Assert.AreEqual(report.StructuresRemoved.Count, 0);
            Assert.AreEqual(report.CompatibilityPercentage, 100);

            var mismatch = new GedcomFile();
            ok = mismatch.Load("../../../samples/name-pieces-multiple-mismatch.ged");
            Assert.IsTrue(ok);

            // Verify that mismatch does not compare equally.
            report = singles.Compare(mismatch);
            Assert.AreEqual(report.StructuresAdded.Count, 6);
            Assert.AreEqual(report.StructuresRemoved.Count, 6);
            Assert.AreEqual(report.CompatibilityPercentage, 50);

            report = mismatch.Compare(singles);
            Assert.AreEqual(report.StructuresAdded.Count, 6);
            Assert.AreEqual(report.StructuresRemoved.Count, 6);
            Assert.AreEqual(report.CompatibilityPercentage, 66);

            report = multiples.Compare(mismatch);
            Assert.AreEqual(report.StructuresAdded.Count, 6);
            Assert.AreEqual(report.StructuresRemoved.Count, 6);
            Assert.AreEqual(report.CompatibilityPercentage, 66);

            report = mismatch.Compare(multiples);
            Assert.AreEqual(report.StructuresAdded.Count, 6);
            Assert.AreEqual(report.StructuresRemoved.Count, 6);
            Assert.AreEqual(report.CompatibilityPercentage, 66);
        }
    }
}
