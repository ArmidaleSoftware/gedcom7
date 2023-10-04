using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gedcom7;
using System.Diagnostics;
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
            long beforeMemory = GC.GetTotalMemory(true);
            TestContext.WriteLine($"Baseline: {beforeMemory} bytes");

            var note = new GedcomFile();

            // Fetch new process information.
            long emptyMemory = GC.GetTotalMemory(true);
            long used = emptyMemory - beforeMemory;
            TestContext.WriteLine($"Empty GedcomFile: {used} bytes");
            Assert.IsTrue(used <= 560);

            bool ok = note.LoadFromPath(fileName);
            Assert.IsTrue(ok);

            // Fetch new process information.
            long afterMemory = GC.GetTotalMemory(true);
            used = afterMemory - emptyMemory;
            TestContext.WriteLine($"Full GedcomFile: +{used} bytes");
            Assert.IsTrue(used <= maxSize);
        }

        [TestMethod]
        public void Minimal70MemoryUsage()
        {
            TestMemoryUsage("../../../../external/GEDCOM.io/testfiles/gedcom70/minimal70.ged", 1752);
        }

        [TestMethod]
        public void Maximal70MemoryUsage()
        {
            TestMemoryUsage("../../../../external/GEDCOM.io/testfiles/gedcom70/maximal70.ged", 228824);
        }
    }
}
