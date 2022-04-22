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
            Assert.AreEqual(report.CompatabilityPercentage, 100);
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

        [TestMethod]
        public void CompareMinimalWithMaximal()
        {
            var minimal70 = new GedcomFile();
            bool ok = minimal70.Load("../../../../external/GEDCOM.io/testfiles/gedcom70/minimal70.ged");
            Assert.IsTrue(ok);

            var maximal70 = new GedcomFile();
            ok = maximal70.Load("../../../../external/GEDCOM.io/testfiles/gedcom70/maximal70.ged");
            Assert.IsTrue(ok);

            // Adding information is ok.
            GedcomComparisonReport report = minimal70.Compare(maximal70);
            Assert.AreEqual(report.StructuresAdded.Count, 799);
            Assert.AreEqual(report.StructuresRemoved.Count, 0);
            Assert.AreEqual(report.CompatabilityPercentage, 100);

            // Losing information is not ok.
            report = maximal70.Compare(minimal70);
            Assert.AreEqual(report.StructuresAdded.Count, 0);
            Assert.AreEqual(report.StructuresRemoved.Count, 799);
            Assert.AreEqual(report.CompatabilityPercentage, 3);
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
            Assert.AreEqual(report.CompatabilityPercentage, 100);
        }
    }
}
