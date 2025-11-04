// Copyright (c) Armidale Software
// SPDX-License-Identifier: MIT
using GedcomLoader;
using Gedcom7;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Tests
{
    [TestClass]
    public class CompatibilityTests
    {
        static string BaselinePath = "../../../../external/GEDCOM-registries/registry_tools/GEDCOM.io/testfiles/gedcom70/";

        private void TestCompatibility(string filename, int tree1Percentage, int tree2Percentage,
            int memories1Percentage, int memories2Percentage, int ldsPercentage)
        {
            var fileFactory = new GedcomFileFactory();
            var file = new GedcomFile();
            List<string> errors = file.LoadFromPath(BaselinePath + filename + ".ged");
            Assert.IsEmpty(errors);
            var report = new GedcomCompatibilityReport(file, fileFactory);

            Assert.AreEqual(tree1Percentage, report.Tree1CompatibilityPercentage);
            Assert.AreEqual(tree2Percentage, report.Tree2CompatibilityPercentage);
            Assert.AreEqual(memories1Percentage, report.Memories1CompatibilityPercentage);
            Assert.AreEqual(memories2Percentage, report.Memories2CompatibilityPercentage);
            Assert.AreEqual(ldsPercentage, report.LdsCompatibilityPercentage);
        }

        [TestMethod]
        public void MinimalCompatibility()
        {
            TestCompatibility("minimal70", 7, 2, 6, 5, 4);
        }

        [TestMethod]
        public void Tree1Compatibility()
        {
            TestCompatibility("maximal70-tree1", 100, 34, 84, 75, 65);
        }

        [TestMethod]
        public void Tree2Compatibility()
        {
            TestCompatibility("maximal70-tree2", 100, 100, 84, 75, 65);
        }

        [TestMethod]
        public void Memories1Compatibility()
        {
            TestCompatibility("maximal70-memories1", 100, 34, 100, 89, 65);
        }

        [TestMethod]
        public void Memories2Compatibility()
        {
            TestCompatibility("maximal70-memories2", 100, 34, 100, 100, 65);
        }

        [TestMethod]
        public void LdsCompatibility()
        {
            TestCompatibility("maximal70-lds", 100, 34, 84, 75, 100);
        }

        [TestMethod]
        public void MaximalCompatibility()
        {
            TestCompatibility("maximal70", 100, 100, 100, 100, 100);
        }
    }
}
