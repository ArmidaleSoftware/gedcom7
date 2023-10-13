// Copyright (c) Armidale Software
// SPDX-License-Identifier: MIT
using Gedcom7;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Tests
{
    [TestClass]
    public class SchemaTests
    {
        [TestMethod]
        public void LoadStructureSchema()
        {
            GedcomStructureSchema.LoadAll();
            var schema = GedcomStructureSchema.GetSchema(null, null, "HEAD");
            Assert.AreEqual(schema?.Uri, "https://gedcom.io/terms/v7/HEAD");
            schema = GedcomStructureSchema.GetSchema(null, "https://gedcom.io/terms/v7/DATA-EVEN", "DATE");
            Assert.AreEqual(schema?.Uri, "https://gedcom.io/terms/v7/DATA-EVEN-DATE");
            schema = GedcomStructureSchema.GetSchema(null, "https://gedcom.io/terms/v7/HEAD", "DATE");
            Assert.AreEqual(schema?.Uri, "https://gedcom.io/terms/v7/HEAD-DATE");
        }

        void ValidateGedcomFile(string path, bool expected_result)
        {
            var file = new GedcomFile();
            bool ok = file.LoadFromPath(path);
            Assert.IsTrue(ok);
            ok = file.Validate();
            Assert.AreEqual(expected_result, ok);
        }

        void ValidateGedcomText(string text, bool expected_result)
        {
            var file = new GedcomFile();
            bool ok = file.LoadFromString(text);
            Assert.IsTrue(ok);
            ok = file.Validate();
            Assert.AreEqual(expected_result, ok);
        }

        [TestMethod]
        public void ValidateEscapes()
        {
            ValidateGedcomFile("../../../../external/GEDCOM.io/testfiles/gedcom70/escapes.ged", true);
        }

        [TestMethod]
        public void ValidateExtensionRecord()
        {
            ValidateGedcomFile("../../../../external/GEDCOM.io/testfiles/gedcom70/extension-record.ged", true);
        }

        [TestMethod]
        public void ValidateLongUrl()
        {
            ValidateGedcomFile("../../../../external/GEDCOM.io/testfiles/gedcom70/long-url.ged", true);
        }

        [TestMethod]
        public void ValidateMaximal70Lds()
        {
            ValidateGedcomFile("../../../../external/GEDCOM.io/testfiles/gedcom70/maximal70-lds.ged", true);
        }

        [TestMethod]
        public void ValidateMaximal70Memories1()
        {
            ValidateGedcomFile("../../../../external/GEDCOM.io/testfiles/gedcom70/maximal70-memories1.ged", true);
        }

        [TestMethod]
        public void ValidateMaximal70Memories2()
        {
            ValidateGedcomFile("../../../../external/GEDCOM.io/testfiles/gedcom70/maximal70-memories2.ged", true);
        }

        [TestMethod]
        public void ValidateMaximal70Tree1()
        {
            ValidateGedcomFile("../../../../external/GEDCOM.io/testfiles/gedcom70/maximal70-tree1.ged", true);
        }

        [TestMethod]
        public void ValidateMaximal70Tree2()
        {
            ValidateGedcomFile("../../../../external/GEDCOM.io/testfiles/gedcom70/maximal70-tree2.ged", true);
        }

        [TestMethod]
        public void ValidateMaximal70()
        {
            ValidateGedcomFile("../../../../external/GEDCOM.io/testfiles/gedcom70/maximal70.ged", true);
        }

        [TestMethod]
        public void ValidateMinimal70()
        {
            ValidateGedcomFile("../../../../external/GEDCOM.io/testfiles/gedcom70/minimal70.ged", true);
        }

        [TestMethod]
        public void ValidateRemarriage1()
        {
            ValidateGedcomFile("../../../../external/GEDCOM.io/testfiles/gedcom70/remarriage1.ged", true);
        }

        [TestMethod]
        public void ValidateRemarriage2()
        {
            ValidateGedcomFile("../../../../external/GEDCOM.io/testfiles/gedcom70/remarriage2.ged", true);
        }

        [TestMethod]
        public void ValidateSameSexMarriage()
        {
            ValidateGedcomFile("../../../../external/GEDCOM.io/testfiles/gedcom70/same-sex-marriage.ged", true);
        }

        [TestMethod]
        public void ValidateSpaces()
        {
            ValidateGedcomFile("../../../../external/GEDCOM.io/testfiles/gedcom70/spaces.ged", true);
        }

        [TestMethod]
        public void ValidateVoidptr()
        {
            ValidateGedcomFile("../../../../external/GEDCOM.io/testfiles/gedcom70/voidptr.ged", true);
        }

        [TestMethod]
        public void ValidateTrailer()
        {
            ValidateGedcomText("0 HEAD\n", false);
            ValidateGedcomText("0 TRLR\n", false);
            ValidateGedcomText("0 HEAD\n0 TRLR\n", true);
            ValidateGedcomText("0 TRLR\n0 HEAD\n", false);

            // The trailer cannot contain substructures.
            ValidateGedcomText("0 HEAD\n0 TRLR\n1 _EXT bad", false);

            // Validate arity.
            ValidateGedcomText("0 HEAD\n0 HEAD\n0 TRLR\n", false);
            ValidateGedcomText("0 HEAD\n0 TRLR\n0 TRLR \n", false);
        }
    }
}
