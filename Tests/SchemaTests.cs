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
            if (ok)
            {
                ok = file.Validate();
            }
            Assert.AreEqual(expected_result, ok);
        }

        void ValidateGedcomText(string text, bool expected_result)
        {
            var file = new GedcomFile();
            bool ok = file.LoadFromString(text);
            if (ok)
            {
                ok = file.Validate();
            }
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
            ValidateGedcomFile("../../../../external/GEDCOM.io/testfiles/gedcom70/spaces.ged", false);
        }

        [TestMethod]
        public void ValidateVoidptr()
        {
            ValidateGedcomFile("../../../../external/GEDCOM.io/testfiles/gedcom70/voidptr.ged", true);
        }

        [TestMethod]
        public void ValidateHeaderAndTrailer()
        {
            // Missing TRLR.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
", false);

            // Missing HEAD.
            ValidateGedcomText("0 TRLR\n", false);

            // Minimal70.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 TRLR
", true);

            // Backwards order.
            ValidateGedcomText(@"0 TRLR
0 HEAD
1 GEDC
2 VERS 7.0
", false);

            // The trailer cannot contain substructures.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 TRLR
1 _EXT bad
", false);

            // Two HEADs.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 HEAD
1 GEDC
2 VERS 7.0
0 TRLR
", false);

            // Two TRLRs.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 TRLR
0 TRLR
", false);
        }

        [TestMethod]
        public void ValidateStructureCardinality()
        {
            // Try zero GEDC which should be {1:1}.
            ValidateGedcomText("0 HEAD\n0 TRLR\n", false);

            // Try two VERS which should be {1:1}.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
2 VERS 7.0
0 TRLR
", false);

            // Try two SCHMA which should be {0:1}.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
1 SCHMA
1 SCHMA
0 TRLR
", false);

            // Try zero FILE which should be {1:M}.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @O1@ OBJE
0 TRLR
", false);

            // Try a COPR at level 0.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 COPR
0 TRLR
", false);

            // Try HEAD.PHON.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
1 PHON
0 TRLR
", false);

            // Try a CONT in the wrong place.
            ValidateGedcomText("0 HEAD\n1 CONT bad\n0 TRLR\n", false);
        }

        [TestMethod]
        public void ValidateSpacing()
        {
            // Leading whitespace is valid prior to 7.0 but not in 7.0.
            ValidateGedcomText(@"0 HEAD
1 GEDC
 2 VERS 5.5.1
0 TRLR
", true);
            ValidateGedcomText(@"0 HEAD
1 GEDC
 2 VERS 7.0
0 TRLR
", false);

            // Extra space before the tag is not valid.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2  VERS 5.5.1
0 TRLR
", false);
            ValidateGedcomText(@"0 HEAD
1 GEDC
2  VERS 7.0
0 TRLR
", false);

            // Trailing whitespace is not valid.
            ValidateGedcomText(@"0 HEAD
1 GEDC 
2 VERS 5.5.1
0 TRLR
", false);
            ValidateGedcomText(@"0 HEAD
1 GEDC 
2 VERS 7.0
0 TRLR
", false);
        }
    }
}
