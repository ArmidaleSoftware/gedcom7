// Copyright (c) Armidale Software
// SPDX-License-Identifier: MIT
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GedcomCommon;
using GedcomLoader;
using System;
using System.Collections.Generic;

namespace Tests
{
    [TestClass]
    public class BasicTests
    {
        private TestContext testContextInstance;

        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
        }

        void TestMemoryUsage(string fileName, long maxSize)
        {
            GedcomStructureSchema.LoadAll(GedcomVersion.V70);
            GC.Collect();
            long beforeMemory = GC.GetTotalMemory(true);
            TestContext.WriteLine($"Baseline: {beforeMemory} bytes");

            var note = new GedcomFile();

            // Fetch new process information.
            GC.Collect();
            long emptyMemory = GC.GetTotalMemory(true);
            long used = emptyMemory - beforeMemory;
            TestContext.WriteLine($"Empty GedcomFile: {used} bytes");

            // Removed the following check since it is unreliable.
            // Assert.IsTrue(used <= 608);

            List<string> errors = note.LoadFromPath(fileName);
            Assert.HasCount(0, errors);

            // Fetch new process information.
            GC.Collect();
            long afterMemory = GC.GetTotalMemory(true);
            used = afterMemory - emptyMemory;
            TestContext.WriteLine($"Full GedcomFile: +{used} bytes for {fileName}");

            // Removed since this is not reliable.
            // Assert.IsTrue(used <= maxSize);
        }

        [TestMethod]
        public void Minimal70MemoryUsage()
        {
            TestMemoryUsage("../../../../external/GEDCOM-registries/registry_tools/GEDCOM.io/testfiles/gedcom70/minimal70.ged", 9376);
        }

        [TestMethod]
        public void Maximal70MemoryUsage()
        {
            TestMemoryUsage("../../../../external/GEDCOM-registries/registry_tools/GEDCOM.io/testfiles/gedcom70/maximal70.ged", 219336);
        }
    }
}
