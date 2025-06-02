// Copyright (c) Armidale Software
// SPDX-License-Identifier: MIT
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GedcomCommon;
using System.Collections.Generic;
using System.IO;

namespace Tests
{
    [TestClass]
    public class ComparisonTests
    {
        private const string TEST_FILES_BASE_PATH = "../../../../external/GEDCOM-registries/registry_tools/GEDCOM.io/testfiles/gedcom70";

        void CompareFileWithSelf(string path)
        {
            var file = new GedcomFile();
            List<string> errors = file.LoadFromPath(path);
            Assert.AreEqual(0, errors.Count);

            GedcomComparisonReport report = file.Compare(file);
            Assert.AreEqual(0, report.StructuresAdded.Count);
            Assert.AreEqual(0, report.StructuresRemoved.Count);
            Assert.AreEqual(100, report.CompatibilityPercentage);
        }

        [TestMethod]
        public void CompareMinimalWithSelf()
        {
            CompareFileWithSelf(Path.Combine(TEST_FILES_BASE_PATH, "minimal70.ged"));
        }

        [TestMethod]
        public void CompareMaximalWithSelf()
        {
            CompareFileWithSelf(Path.Combine(TEST_FILES_BASE_PATH, "maximal70.ged"));
        }

        private void CompareSubsetWithSuperset(string subset, string superset, int structuresAdded, int percentage)
        {
            var subsetFile = new GedcomFile();
            List<string> errors = subsetFile.LoadFromPath(Path.Combine(TEST_FILES_BASE_PATH, subset + ".ged"));
            Assert.AreEqual(0, errors.Count);

            var supersetFile = new GedcomFile();
            errors = supersetFile.LoadFromPath(Path.Combine(TEST_FILES_BASE_PATH, superset + ".ged"));
            Assert.AreEqual(0, errors.Count);

            // Adding information is ok.
            GedcomComparisonReport report = subsetFile.Compare(supersetFile);
            Assert.AreEqual(structuresAdded, report.StructuresAdded.Count);
            Assert.AreEqual(0, report.StructuresRemoved.Count);
            Assert.AreEqual(100, report.CompatibilityPercentage);

            // Losing information is not ok.
            report = supersetFile.Compare(subsetFile);
            Assert.AreEqual(0, report.StructuresAdded.Count);
            Assert.AreEqual(structuresAdded, report.StructuresRemoved.Count);
            Assert.AreEqual(percentage, report.CompatibilityPercentage);
        }

        [TestMethod]
        public void CompareMinimalWithMaximal()
        {
            CompareSubsetWithSuperset("minimal70", "maximal70", 837, 3);
        }

        [TestMethod]
        public void CompareMinimalWithTree1()
        {
            CompareSubsetWithSuperset("minimal70", "maximal70-tree1", 52, 7);
        }

        [TestMethod]
        public void CompareTree1WithTree2()
        {
            CompareSubsetWithSuperset("maximal70-tree1", "maximal70-tree2", 108, 34);
        }

        [TestMethod]
        public void CompareTree2WithMaximal()
        {
            CompareSubsetWithSuperset("maximal70-tree1", "maximal70", 785, 9);
        }

        [TestMethod]
        public void CompareTree1WithLds()
        {
            CompareSubsetWithSuperset("maximal70-tree1", "maximal70-lds", 29, 65);
        }

        [TestMethod]
        public void CompareLdsWithMaximal()
        {
            CompareSubsetWithSuperset("maximal70-lds", "maximal70", 756, 13);
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
            CompareSubsetWithSuperset("maximal70-memories2", "maximal70", 767, 11);
        }

        [TestMethod]
        public void CompareNoteWithSharedNote()
        {
            var note = new GedcomFile();
            List<string> errors = note.LoadFromPath("../../../samples/note.ged");
            Assert.AreEqual(0, errors.Count);

            var snote = new GedcomFile();
            errors = snote.LoadFromPath("../../../samples/snote.ged");
            Assert.AreEqual(0, errors.Count);

            GedcomComparisonReport report = note.Compare(snote);
            Assert.AreEqual(0, report.StructuresAdded.Count);
            Assert.AreEqual(0, report.StructuresRemoved.Count);
            Assert.AreEqual(100, report.CompatibilityPercentage);
        }

        [TestMethod]
        public void CompareSingleNamePiecesWithMultipleNamePieces()
        {
            var singles = new GedcomFile();
            List<string> errors = singles.LoadFromPath("../../../samples/name-pieces-single.ged");
            Assert.AreEqual(0, errors.Count);

            var multiples = new GedcomFile();
            errors = multiples.LoadFromPath("../../../samples/name-pieces-multiple.ged");
            Assert.AreEqual(0, errors.Count);

            // Verify that both compare equally.
            GedcomComparisonReport report = singles.Compare(multiples);
            Assert.AreEqual(0, report.StructuresAdded.Count);
            Assert.AreEqual(0, report.StructuresRemoved.Count);
            Assert.AreEqual(100, report.CompatibilityPercentage);

            report = multiples.Compare(singles);
            Assert.AreEqual(0, report.StructuresAdded.Count);
            Assert.AreEqual(0, report.StructuresRemoved.Count);
            Assert.AreEqual(100, report.CompatibilityPercentage);

            var mismatch = new GedcomFile();
            errors = mismatch.LoadFromPath("../../../samples/name-pieces-multiple-mismatch.ged");
            Assert.AreEqual(0, errors.Count);

            // Verify that mismatch does not compare equally.
            report = singles.Compare(mismatch);
            Assert.AreEqual(6, report.StructuresAdded.Count);
            Assert.AreEqual(6, report.StructuresRemoved.Count);
            Assert.AreEqual(50, report.CompatibilityPercentage);

            report = mismatch.Compare(singles);
            Assert.AreEqual(6, report.StructuresAdded.Count);
            Assert.AreEqual(6, report.StructuresRemoved.Count);
            Assert.AreEqual(66, report.CompatibilityPercentage);

            report = multiples.Compare(mismatch);
            Assert.AreEqual(6, report.StructuresAdded.Count);
            Assert.AreEqual(6, report.StructuresRemoved.Count);
            Assert.AreEqual(66, report.CompatibilityPercentage);

            report = mismatch.Compare(multiples);
            Assert.AreEqual(6, report.StructuresAdded.Count);
            Assert.AreEqual(6, report.StructuresRemoved.Count);
            Assert.AreEqual(66, report.CompatibilityPercentage);
        }
    }
}
