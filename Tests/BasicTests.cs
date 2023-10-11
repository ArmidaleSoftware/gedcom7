// Copyright (c) Armidale Software
// SPDX-License-Identifier: MIT
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gedcom7;
using System;

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
            GedcomStructureSchema.LoadAll();
            long beforeMemory = GC.GetTotalMemory(true);
            TestContext.WriteLine($"{fileName}: {beforeMemory} bytes");
            Console.WriteLine($"{fileName}: {beforeMemory} bytes");

            var note = new GedcomFile();

            // Fetch new process information.
            long emptyMemory = GC.GetTotalMemory(true);
            long used = emptyMemory - beforeMemory;
            TestContext.WriteLine($"Empty GedcomFile: {used} bytes");
            Console.WriteLine($"Empty GedcomFile: {used} bytes");
            Assert.IsTrue(used <= 1664);

            bool ok = note.LoadFromPath(fileName);
            Assert.IsTrue(ok);

            // Fetch new process information.
            long afterMemory = GC.GetTotalMemory(true);
            used = afterMemory - emptyMemory;
            TestContext.WriteLine($"Full GedcomFile: +{used} bytes");
            Console.WriteLine($"Full GedcomFile: +{used} bytes");
            Assert.IsTrue(used <= maxSize);
        }

        [TestMethod]
        public void Minimal70MemoryUsage()
        {
            // Note: currently the number is high because the taginfo is lazily constructed
            // instead of constructed a priori which would remove it from the per-file size.
            TestMemoryUsage("../../../../external/GEDCOM.io/testfiles/gedcom70/minimal70.ged", 9824);

            // Now that the taginfo dictionary has been constructed,
            // do a real test.
            TestMemoryUsage("../../../../external/GEDCOM.io/testfiles/gedcom70/minimal70.ged", 1112);
        }

        [TestMethod]
        public void Maximal70MemoryUsage()
        {
            TestMemoryUsage("../../../../external/GEDCOM.io/testfiles/gedcom70/maximal70.ged", 235928);

            // Now that the taginfo dictionary has been constructed,
            // do a real test.
            TestMemoryUsage("../../../../external/GEDCOM.io/testfiles/gedcom70/maximal70.ged", 213616);
        }
    }
}
