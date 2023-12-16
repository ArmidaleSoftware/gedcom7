// Copyright (c) Armidale Software
// SPDX-License-Identifier: MIT
using Gedcom7;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Tests
{
    [TestClass]
    public class SchemaTests
    {
        [TestMethod]
        public void LoadStructureSchema()
        {
            GedcomStructureSchema.LoadAll();
            var schema = GedcomStructureSchema.GetSchema(null, GedcomStructureSchema.RecordSuperstructureUri, "HEAD");
            Assert.AreEqual(schema?.Uri, "https://gedcom.io/terms/v7/HEAD");
            schema = GedcomStructureSchema.GetSchema(null, "https://gedcom.io/terms/v7/DATA-EVEN", "DATE");
            Assert.AreEqual(schema?.Uri, "https://gedcom.io/terms/v7/DATA-EVEN-DATE");
            schema = GedcomStructureSchema.GetSchema(null, "https://gedcom.io/terms/v7/HEAD", "DATE");
            Assert.AreEqual(schema?.Uri, "https://gedcom.io/terms/v7/HEAD-DATE");
        }

        public static void ValidateGedcomFile(string path, string expected_result = null)
        {
            var file = new GedcomFile();
            List<string> errors = file.LoadFromPath(path);
            string error = null;
            if (errors.Count == 0)
            {
                errors.AddRange(file.Validate());
            }
            if (errors.Count > 0)
            {
                error = string.Join("\n", errors);
            }
            Assert.AreEqual(expected_result, error);
        }

        public static void ValidateGedzipFile(string path, string expected_result = null)
        {
            var file = new GedzipFile();
            List<string> errors = file.LoadFromPath(path);
            string error = null;
            if (errors.Count == 0)
            {
                errors.AddRange(file.Validate());
            }
            if (errors.Count > 0)
            {
                error = string.Join("\n", errors);
            }
            Assert.AreEqual(expected_result, error);
        }

        public static void ValidateGedcomText(string text, string expected_result = null)
        {
            var file = new GedcomFile();
            List<string> errors = file.LoadFromString(text);
            string error = null;
            if (errors.Count == 0)
            {
                errors.AddRange(file.Validate());
            }
            if (errors.Count > 0)
            {
                error = string.Join("\n", errors);
            }
            Assert.AreEqual(expected_result, error);
        }

        [TestMethod]
        public void ValidateFileEscapes()
        {
            ValidateGedcomFile("../../../../external/GEDCOM.io/testfiles/gedcom70/escapes.ged");
        }

        [TestMethod]
        public void ValidateFileExtensionRecord()
        {
            ValidateGedcomFile("../../../../external/GEDCOM.io/testfiles/gedcom70/extension-record.ged");
        }

        [TestMethod]
        public void ValidateFileLongUrl()
        {
            ValidateGedcomFile("../../../../external/GEDCOM.io/testfiles/gedcom70/long-url.ged");
        }

        [TestMethod]
        public void ValidateFileMaximal70Lds()
        {
            ValidateGedcomFile("../../../../external/GEDCOM.io/testfiles/gedcom70/maximal70-lds.ged");
        }

        [TestMethod]
        public void ValidateFileMaximal70Memories1()
        {
            ValidateGedcomFile("../../../../external/GEDCOM.io/testfiles/gedcom70/maximal70-memories1.ged");
        }

        [TestMethod]
        public void ValidateFileMaximal70Memories2()
        {
            ValidateGedcomFile("../../../../external/GEDCOM.io/testfiles/gedcom70/maximal70-memories2.ged");
        }

        [TestMethod]
        public void ValidateFileMaximal70Tree1()
        {
            ValidateGedcomFile("../../../../external/GEDCOM.io/testfiles/gedcom70/maximal70-tree1.ged");
        }

        [TestMethod]
        public void ValidateFileMaximal70Tree2()
        {
            ValidateGedcomFile("../../../../external/GEDCOM.io/testfiles/gedcom70/maximal70-tree2.ged");
        }

        [TestMethod]
        public void ValidateFileMaximal70()
        {
            ValidateGedcomFile("../../../../external/GEDCOM.io/testfiles/gedcom70/maximal70.ged");
        }

        [TestMethod]
        public void ValidateFileMaximal70Zip()
        {
            // https://github.com/FamilySearch/GEDCOM.io/issues/130
            ValidateGedzipFile("../../../../external/GEDCOM.io/testfiles/gedcom70/maximal70.gdz",
                "../../../../external/GEDCOM.io/testfiles/gedcom70/maximal70.gdz is missing file:///path/to/file1");
        }

        [TestMethod]
        public void ValidateFileMinimal70()
        {
            ValidateGedcomFile("minimal70.txt", "minimal70.txt must have a .ged extension");

            ValidateGedcomFile("../../../../external/GEDCOM.io/testfiles/gedcom70/minimal70.ged");
        }

        [TestMethod]
        public void ValidateFileMinimal70Zip()
        {
            ValidateGedzipFile("minimal70.zip", "minimal70.zip must have a .gdz extension");
            ValidateGedzipFile("../../../../external/GEDCOM.io/testfiles/gedcom70/minimal70.gdz");
        }

        [TestMethod]
        public void ValidateFileRemarriage1()
        {
            ValidateGedcomFile("../../../../external/GEDCOM.io/testfiles/gedcom70/remarriage1.ged");
        }

        [TestMethod]
        public void ValidateFileRemarriage2()
        {
            ValidateGedcomFile("../../../../external/GEDCOM.io/testfiles/gedcom70/remarriage2.ged");
        }

        [TestMethod]
        public void ValidateFileSameSexMarriage()
        {
            ValidateGedcomFile("../../../../external/GEDCOM.io/testfiles/gedcom70/same-sex-marriage.ged");
        }

        [TestMethod]
        public void ValidateFileVoidptr()
        {
            ValidateGedcomFile("../../../../external/GEDCOM.io/testfiles/gedcom70/voidptr.ged");
        }

        [TestMethod]
        public void ValidateHeaderAndTrailer()
        {
            // Missing TRLR.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
", "Missing TRLR record");

            // Missing HEAD.
            ValidateGedcomText("0 TRLR\n", "Line 1: HEAD must be the first record");

            // Minimal70.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 TRLR
");

            // Backwards order.
            ValidateGedcomText(@"0 TRLR
0 HEAD
1 GEDC
2 VERS 7.0
", "Line 1: HEAD must be the first record");

            // The trailer cannot contain substructures.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 TRLR
1 _EXT bad
", "Line 4: TRLR must not contain substructures");

            // Two HEADs.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 HEAD
1 GEDC
2 VERS 7.0
0 TRLR
", "Line 4: HEAD must be the first record");

            // Two TRLRs.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 TRLR
0 TRLR
", "Line 5: Duplicate TRLR record");

            // No records.
            ValidateGedcomText("", "Missing TRLR record");
        }

        [TestMethod]
        public void ValidateStructureCardinality()
        {
            // Try zero GEDC which should be {1:1}.
            ValidateGedcomText("0 HEAD\n0 TRLR\n", "Line 1: HEAD is missing a substructure of type https://gedcom.io/terms/v7/GEDC");

            // Try two VERS which should be {1:1}.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
2 VERS 7.0
0 TRLR
", "Line 2: GEDC does not permit multiple substructures of type https://gedcom.io/terms/v7/GEDC-VERS");

            // Try two SCHMA which should be {0:1}.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
1 SCHMA
1 SCHMA
0 TRLR
", "Line 1: HEAD does not permit multiple substructures of type https://gedcom.io/terms/v7/SCHMA");

            // Try zero FILE which should be {1:M}.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @O1@ OBJE
0 TRLR
", "Line 4: OBJE is missing a substructure of type https://gedcom.io/terms/v7/FILE");

            // Try a COPR at level 0.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @C0@ COPR
0 TRLR
", "Line 4: Undocumented standard record");

            // Try HEAD.PHON.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
1 PHON
0 TRLR
", "Line 4: PHON is not a valid substructure of HEAD");

            // Try a CONT in the wrong place.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
1 CONT bad
0 TRLR
", "Line 4: CONT is not a valid substructure of HEAD");
        }

        [TestMethod]
        public void ValidateSpacing()
        {
            // Leading whitespace is valid prior to 7.0 but not in 7.0.
            ValidateGedcomText(@"0 HEAD
1 GEDC
 2 VERS 5.5.1
0 TRLR
");
            ValidateGedcomText(@"0 HEAD
1 GEDC
 2 VERS 7.0
0 TRLR
", "Line 3: Line must start with an integer");
            ValidateGedcomText(@"0 HEAD
 1 GEDC
 2 VERS 7.0
0 TRLR
", "Line 2: Line must start with an integer\nLine 3: Line must start with an integer");

            // Extra space before the tag is not valid.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2  VERS 5.5.1
0 TRLR
", "Line 3: Tag must not be empty");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2  VERS 7.0
0 TRLR
", "Line 3: Tag must not be empty");

            // Trailing whitespace is not valid.
            ValidateGedcomText(@"0 HEAD
1 GEDC 
2 VERS 5.5.1
0 TRLR
", "Line 2: An empty payload is not valid after a space");
            ValidateGedcomText(@"0 HEAD
1 GEDC 
2 VERS 7.0
0 TRLR
", "Line 2: An empty payload is not valid after a space");
        }

        [TestMethod]
        public void ValidateXref()
        {
            // HEAD record does not allow an xref.
            ValidateGedcomText(@"0 @H1@ HEAD
1 GEDC
2 VERS 5.5.1
0 TRLR
", "Line 1: Xref is not valid for this record");
            ValidateGedcomText(@"0 @H1@ HEAD
1 GEDC
2 VERS 7.0
0 TRLR
", "Line 1: Xref is not valid for this record");

            // INDI record requires an xref.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
0 INDI
0 TRLR
", "Line 4: Missing Xref for this record");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 INDI
0 TRLR
", "Line 4: Missing Xref for this record");

            // TRLR record does not allow an xref.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
0 @T1@ TRLR
", "Line 4: Xref is not valid for this record");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @T1@ TRLR
", "Line 4: Xref is not valid for this record");

            // Xref must start with @.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
0 I1@ INDI
0 TRLR
", "Line 4: Undocumented standard record");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 I1@ INDI
0 TRLR
", "Line 4: Undocumented standard record");

            // Xref must end with @.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
0 @I1 INDI
0 TRLR
", "Line 4: Xref must start and end with @");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @I1 INDI
0 TRLR
", "Line 4: Xref must start and end with @");

            // Xref must contain something.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
0 @ INDI
0 TRLR
", "Line 4: Xref must start and end with @");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @ INDI
0 TRLR
", "Line 4: Xref must start and end with @");

            // Xref must start with @.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
0 I1@ INDI
0 TRLR
", "Line 4: Undocumented standard record");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 I1@ INDI
0 TRLR
", "Line 4: Undocumented standard record");

            // Xref must end with @.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
0 @I1 INDI
0 TRLR
", "Line 4: Xref must start and end with @");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @I1 INDI
0 TRLR
", "Line 4: Xref must start and end with @");

            // Xref must contain something.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
0 @ INDI
0 TRLR
", "Line 4: Xref must start and end with @");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @ INDI
0 TRLR
", "Line 4: Xref must start and end with @");

            // Test characters within an xref, which is
            // @<alphanum><pointer_string>@
            // GEDCOM 5.5.1:
            // where pointer_string has (alnum|space|#)
            // and GEDCOM 7.0
            // where pointer_string has (upper|digit|_)

            // Upper case letters and numbers are fine.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
0 @I1@ INDI
0 TRLR
");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @I1@ INDI
0 TRLR
");

            // GEDCOM 7.0 disallows @VOID@ as an actual xref id.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
0 @VOID@ INDI
0 TRLR
");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @VOID@ INDI
0 TRLR
", "Line 4: Xref must not be @VOID@");

            // Hash is ok in GEDCOM 5.5.1 (except at the start) but not 7.0.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
0 @I#1@ INDI
0 TRLR
");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
0 @#I1@ INDI
0 TRLR
", "Line 4: Xref \"@#I1@\" does not start with a letter or digit");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @I#1@ INDI
0 TRLR
", "Line 4: Invalid character '#' in Xref \"@I#1@\"");

            // Underscore is ok in GEDCOM 7.0 but not 5.5.1.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
0 @I_1@ INDI
0 TRLR
", "Line 4: Invalid character '_' in Xref \"@I_1@\"");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @I_1@ INDI
0 TRLR
");

            // Lower-case letters are ok in GEDCOM 5.5.1 but not 7.0.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
0 @i1@ INDI
0 TRLR
");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @i1@ INDI
0 TRLR
", "Line 4: Invalid character 'i' in Xref \"@i1@\"");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @I1@ INDI
0 @I1@ INDI
0 TRLR
", "Line 5: Duplicate Xref @I1@");
        }

        [TestMethod]
        public void ValidatePayloadType()
        {
            // Validate null.
            ValidateGedcomText(@"0 HEAD
1 GEDC 1
2 VERS 5.5.1
0 TRLR
", "Line 2: GEDC payload must be null");

            // Validate an integer.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
0 @I1@ INDI
1 NCHI 0
0 TRLR
");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
0 @I1@ INDI
1 NCHI -1
0 TRLR
", "Line 5: \"-1\" is not a non-negative integer");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
0 @I1@ INDI
1 NCHI
0 TRLR
", "Line 5: \"\" is not a non-negative integer");

            // Test Y|<NULL>.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
0 @I1@ INDI
1 BIRT N
0 TRLR
", "Line 5: BIRT payload must be 'Y' or empty");

            // We can't validate "standard" structures
            // under an extension, since they may be
            // ambiguous, such as "NAME" or "HUSB".
            // TODO: We could perhaps try ALL possibilities.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
1 _UNKNOWN
2 UNKNOWN
0 TRLR
");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @U1@ _UNKNOWN
1 SOUR @S1@
0 @S1@ SOUR
1 TITL Title
0 TRLR
");
        }

        private void ValidateValidFilePayload(string value)
        {
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
0 @O1@ OBJE
1 FILE " + value + @"
2 FORM application/x-other
0 TRLR
");
        }

        private void ValidateInvalidFilePayload(string value)
        {
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
0 @O1@ OBJE
1 FILE " + value + @"
2 FORM application/x-other
0 TRLR
", "Line 5: \"" + value + "\" is not a valid URI reference");
        }

        private void ValidateInvalidFormPayload(string value)
        {
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
0 @O1@ OBJE
1 FILE foo
2 FORM " + value + @"
0 TRLR
", "Line 6: \"" + value + "\" is not a valid media type");
        }

        /// <summary>
        /// Validate enum payload type.
        /// </summary>
        [TestMethod]
        public void ValidateEnumPayloadType()
        {
            // Try a valid enum value.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @I1@ INDI
1 SEX U
0 TRLR
");

            // Try an invalid enum value.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @I1@ INDI
1 SEX UNKNOWN
0 TRLR
", "Line 5: \"UNKNOWN\" is not a valid value for SEX");

            // Try a valid structure name as an enum value.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @I1@ INDI
1 NO CENS
0 TRLR
");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @I1@ INDI
1 NO ADOP
0 TRLR
");

            // Try an incorrect structure name as an enum value.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @I1@ INDI
1 NO FAM
0 TRLR
", "Line 5: \"FAM\" is not a valid value for NO");

            // Validate List of Enum.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @I1@ INDI
1 RESN CONFIDENTIAL
0 TRLR
");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @I1@ INDI
1 RESN CONFIDENTIAL, LOCKED
0 TRLR
");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @I1@ INDI
1 RESN UNKNOWN
0 TRLR
", "Line 5: \"UNKNOWN\" is not a valid value for RESN");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @I1@ INDI
1 RESN CONFIDENTIAL,
0 TRLR
", "Line 5: \"\" is not a valid value for RESN");
        }

        private void ValidateInvalidNamePayload(string value)
        {
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @I1@ INDI
1 NAME " + value + @"
", "Line 5: \"" + value + "\" is not a valid name");
        }

        private void ValidateValidNamePayload(string value)
        {
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @I1@ INDI
1 NAME " + value + @"
0 TRLR
");
        }

        /// <summary>
        /// Validate Name payload type.
        /// </summary>
        [TestMethod]
        public void ValidateNamePayloadType()
        {
            // Try some valid name values.
            ValidateValidNamePayload("John Smith");
            ValidateValidNamePayload("John /Smith/");
            ValidateValidNamePayload("John /Smith/ Jr.");

            // Try some invalid name values.
            ValidateInvalidNamePayload("/");
            ValidateInvalidNamePayload("a/b/c/d");
            ValidateInvalidNamePayload("a\tb");
        }

        private void ValidateInvalidExactDatePayload(string value)
        {
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
1 DATE " + value + @"
", "Line 4: \"" + value + "\" is not a valid exact date");
        }

        private void ValidateValidExactDatePayload(string value)
        {
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
1 DATE " + value + @"
0 TRLR
");
        }

        /// <summary>
        /// Validate exact date payload type.
        /// </summary>
        [TestMethod]
        public void ValidateExactDatePayloadType()
        {
            // Try some valid name values.
            ValidateValidExactDatePayload("3 DEC 2023");
            ValidateValidExactDatePayload("03 DEC 2023");

            // Try some invalid name values.
            ValidateInvalidExactDatePayload("invalid");
            ValidateInvalidExactDatePayload("3 dec 2023");
            ValidateInvalidExactDatePayload("3 JUNE 2023");
            ValidateInvalidExactDatePayload("DEC 2023");
            ValidateInvalidExactDatePayload("2023");
        }

        private void ValidateInvalidDatePeriodPayload(string value)
        {
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @I1@ INDI
1 NO MARR
2 DATE " + value + @"
0 TRLR
", "Line 6: \"" + value + "\" is not a valid date period");
        }

        private void ValidateValidDatePeriodPayload(string value)
        {
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @I1@ INDI
1 NO MARR
2 DATE " + value + @"
0 TRLR
");
        }

        /// <summary>
        /// Validate date period payload type.
        /// </summary>
        [TestMethod]
        public void ValidateDatePeriodPayloadType()
        {
            // Try some valid date period values.
            ValidateValidDatePeriodPayload("TO 3 DEC 2023");
            ValidateValidDatePeriodPayload("TO DEC 2023");
            ValidateValidDatePeriodPayload("TO 2023");
            ValidateValidDatePeriodPayload("TO GREGORIAN 20 BCE");
            ValidateValidDatePeriodPayload("FROM 03 DEC 2023");
            ValidateValidDatePeriodPayload("FROM 2000 TO 2020");
            ValidateValidDatePeriodPayload("FROM MAR 2000 TO JUN 2000");
            ValidateValidDatePeriodPayload("FROM 30 NOV 2000 TO 1 DEC 2000");
            ValidateValidDatePeriodPayload("FROM HEBREW 1 TSH 1");
            ValidateValidDatePeriodPayload("FROM GREGORIAN 20 BCE TO GREGORIAN 12 BCE");

            // Try some invalid date period values.
            ValidateInvalidDatePeriodPayload("2023");
            ValidateInvalidDatePeriodPayload("TO 40 DEC 2023");
            ValidateInvalidDatePeriodPayload("TO 3 dec 2023");
            ValidateInvalidDatePeriodPayload("TO 3 JUNE 2023");
            ValidateInvalidDatePeriodPayload("TO ABC 2023");
            ValidateInvalidDatePeriodPayload("FROM HEBREW 1 TSH 1 BCE");
        }

        private void ValidateInvalidDateValuePayload(string value)
        {
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @I1@ INDI
1 DEAT
2 DATE " + value + @"
0 TRLR
", "Line 6: \"" + value + "\" is not a valid date value");
        }

        private void ValidateValidDateValuePayload(string value)
        {
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @I1@ INDI
1 DEAT
2 DATE " + value + @"
0 TRLR
");
        }

        /// <summary>
        /// Validate date value payload type.
        /// </summary>
        [TestMethod]
        public void ValidateDateValuePayloadType()
        {
            // Try some valid dates.
            ValidateValidDateValuePayload("3 DEC 2023");
            ValidateValidDateValuePayload("DEC 2023");
            ValidateValidDateValuePayload("2023");
            ValidateValidDateValuePayload("GREGORIAN 20 BCE");
            ValidateValidDateValuePayload("HEBREW 1 TSH 1");

            // Try some valid date periods.
            ValidateValidDateValuePayload("TO 3 DEC 2023");
            ValidateValidDateValuePayload("TO DEC 2023");
            ValidateValidDateValuePayload("TO 2023");
            ValidateValidDateValuePayload("TO GREGORIAN 20 BCE");
            ValidateValidDateValuePayload("FROM 03 DEC 2023");
            ValidateValidDateValuePayload("FROM 2000 TO 2020");
            ValidateValidDateValuePayload("FROM MAR 2000 TO JUN 2000");
            ValidateValidDateValuePayload("FROM 30 NOV 2000 TO 1 DEC 2000");
            ValidateValidDateValuePayload("FROM HEBREW 1 TSH 1");
            ValidateValidDateValuePayload("FROM GREGORIAN 20 BCE TO GREGORIAN 12 BCE");

            // Try some valid date ranges.
            ValidateValidDateValuePayload("BEF 3 DEC 2023");
            ValidateValidDateValuePayload("BEF DEC 2023");
            ValidateValidDateValuePayload("BEF 2023");
            ValidateValidDateValuePayload("BEF GREGORIAN 20 BCE");
            ValidateValidDateValuePayload("AFT 03 DEC 2023");
            ValidateValidDateValuePayload("AFT HEBREW 1 TSH 1");
            ValidateValidDateValuePayload("BET 2000 AND 2020");
            ValidateValidDateValuePayload("BET MAR 2000 AND JUN 2000");
            ValidateValidDateValuePayload("BET 30 NOV 2000 AND 1 DEC 2000");
            ValidateValidDateValuePayload("BET GREGORIAN 20 BCE AND GREGORIAN 12 BCE");

            // Try some valid approximate dates.
            ValidateValidDateValuePayload("ABT 3 DEC 2023");
            ValidateValidDateValuePayload("CAL DEC 2023");
            ValidateValidDateValuePayload("EST GREGORIAN 20 BCE");

            // Try some invalid date values.
            ValidateInvalidDateValuePayload("TO 40 DEC 2023");
            ValidateInvalidDateValuePayload("TO 3 dec 2023");
            ValidateInvalidDateValuePayload("TO 3 JUNE 2023");
            ValidateInvalidDateValuePayload("TO ABC 2023");
            ValidateInvalidDateValuePayload("BEF 40 DEC 2023");
            ValidateInvalidDateValuePayload("BEF 3 dec 2023");
            ValidateInvalidDateValuePayload("BEF 3 JUNE 2023");
            ValidateInvalidDateValuePayload("BEF ABC 2023");
            ValidateInvalidDateValuePayload("BET 2000");
            ValidateInvalidDateValuePayload("FROM HEBREW 1 TSH 1 BCE");
            ValidateInvalidDateValuePayload("AFT HEBREW 1 TSH 1 BCE");
        }

        private void ValidateInvalidTimePayload(string value)
        {
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
1 DATE 1 DEC 2023
2 TIME " + value + @"
0 TRLR
", "Line 5: \"" + value + "\" is not a valid time");
        }

        private void ValidateValidTimePayload(string value)
        {
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
1 DATE 1 DEC 2023
2 TIME " + value + @"
0 TRLR
");
        }

        /// <summary>
        /// Validate Time payload type.
        /// </summary>
        [TestMethod]
        public void ValidateTimePayloadType()
        {
            // Try some valid time values.
            ValidateValidTimePayload("02:50");
            ValidateValidTimePayload("2:50");
            ValidateValidTimePayload("2:50:00.00Z");

            // Try some invalid time values.
            ValidateInvalidTimePayload(" ");
            ValidateInvalidTimePayload("invalid");
            ValidateInvalidTimePayload("000:00");
            ValidateInvalidTimePayload("24:00:00");
            ValidateInvalidTimePayload("2:5");
            ValidateInvalidTimePayload("2:60");
            ValidateInvalidTimePayload("2:00:60");
        }

        private void ValidateInvalidAgePayload(string value)
        {
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @I1@ INDI
1 DEAT
2 AGE " + value + @"
0 TRLR
", "Line 6: \"" + value + "\" is not a valid age");
        }

        private void ValidateValidAgePayload(string value)
        {
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @I1@ INDI
1 DEAT
2 AGE " + value + @"
0 TRLR
");
        }

        /// <summary>
        /// Validate Age payload type.
        /// </summary>
        [TestMethod]
        public void ValidateAgePayloadType()
        {
            // Try some valid age values.
            ValidateValidAgePayload("79y");
            ValidateValidAgePayload("79y 1d");
            ValidateValidAgePayload("79y 1w");
            ValidateValidAgePayload("79y 1w 1d");
            ValidateValidAgePayload("79y 1m");
            ValidateValidAgePayload("79y 1m 1d");
            ValidateValidAgePayload("79y 1m 1w");
            ValidateValidAgePayload("79y 1m 1w 1d");
            ValidateValidAgePayload("79m");
            ValidateValidAgePayload("1m 1d");
            ValidateValidAgePayload("1m 1w");
            ValidateValidAgePayload("1m 1w 1d");
            ValidateValidAgePayload("79w");
            ValidateValidAgePayload("79w 1d");
            ValidateValidAgePayload("79d");
            ValidateValidAgePayload(">79y");
            ValidateValidAgePayload("<79y 1m 1w 1d");

            // Try some invalid age values.
            ValidateInvalidAgePayload(" ");
            ValidateInvalidAgePayload("invalid");
            ValidateInvalidAgePayload("d");
            ValidateInvalidAgePayload("79");
            ValidateInvalidAgePayload("1d 1m");
            ValidateInvalidAgePayload("<>1y");
        }

        private void ValidateInvalidLanguagePayload(string value)
        {
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
1 LANG " + value + @"
0 TRLR
", "Line 4: \"" + value + "\" is not a valid language");
        }

        private void ValidateValidLanguagePayload(string value)
        {
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
1 LANG " + value + @"
0 TRLR
");
        }

        /// <summary>
        /// Validate Language payload type.
        /// </summary>
        [TestMethod]
        public void ValidateLanguagePayloadType()
        {
            // Try some valid language values.
            ValidateValidLanguagePayload("und");
            ValidateValidLanguagePayload("mul");
            ValidateValidLanguagePayload("en");
            ValidateValidLanguagePayload("en-US");
            ValidateValidLanguagePayload("und-Latn-pinyin");

            // Try some invalid language values.
            ValidateInvalidLanguagePayload(" ");
            ValidateInvalidLanguagePayload("-");
            ValidateInvalidLanguagePayload("und-");
            ValidateInvalidLanguagePayload("-und");
            ValidateInvalidLanguagePayload("en US");
        }

        /// <summary>
        /// Validate file path payload type.
        /// </summary>
        [TestMethod]
        public void ValidateFilePathPayloadType()
        {
            // Test some valid values, including some file: URIs
            // from RFC 8089.
            ValidateValidFilePayload("media/filename");
            ValidateValidFilePayload("http://www.contoso.com/path/filename");
            ValidateValidFilePayload("file://host.example.com/path/to/file");
            ValidateValidFilePayload("file:///path/to/file");

            // Test invalid values.  These test strings are taken from
            // https://learn.microsoft.com/en-us/dotnet/api/system.uri.iswellformeduristring?view=net-8.0
            ValidateInvalidFilePayload("http://www.contoso.com/path???/file name");
            ValidateInvalidFilePayload("c:\\\\directory\\filename");
            ValidateInvalidFilePayload("file://c:/directory/filename");
            ValidateInvalidFilePayload("http:\\\\\\host/path/file");
            ValidateInvalidFilePayload("2013.05.29_14:33:41");
        }

        /// <summary>
        /// Validate media type payload type.
        /// </summary>
        [TestMethod]
        public void ValidateMediaTypePayloadType()
        {
            // Validate a media type.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
0 @O1@ OBJE
1 FILE foo
2 FORM
0 TRLR
", "Line 6: \"\" is not a valid media type");

            // Validate FORM payload.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
0 @O1@ OBJE
1 FILE foo
2 FORM application/x-other
0 TRLR
");
            ValidateInvalidFormPayload("invalid media type");
            ValidateInvalidFormPayload("text/");
            ValidateInvalidFormPayload("/text");
            ValidateInvalidFormPayload("text/a/b");
            ValidateInvalidFormPayload("text");

            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
0 @N1@ SNOTE Test
1 MIME text/unknown
0 TRLR
", "Line 5: MIME payload must be text/plain or text/html");
        }

        /// <summary>
        /// Validate payload as a pointer to recordType.
        /// </summary>
        [TestMethod]
        public void ValidateXrefPayloadType()
        {
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
1 SUBM @S1
0 TRLR
", "Line 4: Payload must be a pointer");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
1 SUBM
0 TRLR
", "Line 4: Payload must be a pointer");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
1 SUBM S1@
0 TRLR
", "Line 4: Payload must be a pointer");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
1 SUBM @S1@
0 TRLR
", "Line 4: @S1@ has no associated record");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
1 SUBM @I1@
0 @I1@ INDI
0 TRLR
", "Line 4: SUBM points to a INDI record");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
1 SUBM @I1@
0 @I1@ _SUBM
0 TRLR
", "Line 4: SUBM points to a _SUBM record");

            // We can't validate the record type for an
            // undocumented extension.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
1 _SUBM @I1@
0 @I1@ INDI
0 TRLR
");
        }

        // Test files from the test-files repository.

        [TestMethod]
        public void ValidateTestFileAtSign()
        {
            ValidateGedcomFile("../../../../external/test-files/7/atsign.ged");
        }

        [TestMethod]
        public void ValidateTestFileChar()
        {
            ValidateGedcomFile("../../../../external/test-files/7/char_ascii_1.ged");
            ValidateGedcomFile("../../../../external/test-files/7/char_ascii_2.ged");

            ValidateGedcomFile("../../../../external/test-files/7/char_utf16be-1.ged");
            ValidateGedcomFile("../../../../external/test-files/7/char_utf16be-2.ged");

            ValidateGedcomFile("../../../../external/test-files/7/char_utf16le-1.ged");
            ValidateGedcomFile("../../../../external/test-files/7/char_utf16le-2.ged");

            ValidateGedcomFile("../../../../external/test-files/7/char_utf8-1.ged");
            ValidateGedcomFile("../../../../external/test-files/7/char_utf8-2.ged");
            ValidateGedcomFile("../../../../external/test-files/7/char_utf8-3.ged");
        }

        [TestMethod]
        public void ValidateTestFileDateAll()
        {
            ValidateGedcomFile("../../../../external/test-files/7/date-all.ged");
        }

        [TestMethod]
        public void ValidateTestFileDateDual()
        {
            ValidateGedcomFile("../../../../external/test-files/7/date-dual.ged");
        }

        [TestMethod]
        public void ValidateTestFileEnumExt()
        {
            ValidateGedcomFile("../../../../external/test-files/7/enum-ext.ged");
        }

        [TestMethod]
        public void ValidateTestFileFilename()
        {
            ValidateGedcomFile("../../../../external/test-files/7/filename-1.ged");
        }

        [TestMethod]
        public void ValidateTestFileLangAll()
        {
            ValidateGedcomFile("../../../../external/test-files/7/lang-all.ged");
        }

        [TestMethod]
        public void ValidateTestFileNotes()
        {
            ValidateGedcomFile("../../../../external/test-files/7/notes-1.ged");
        }

        [TestMethod]
        public void ValidateTestFileObje()
        {
            ValidateGedcomFile("../../../../external/test-files/7/obje-1.ged");
        }

        [TestMethod]
        public void ValidateTestFileObsolete()
        {
            ValidateGedcomFile("../../../../external/test-files/7/obsolete-1.ged");
        }

        [TestMethod]
        public void ValidateTestFilePedi()
        {
            ValidateGedcomFile("../../../../external/test-files/7/pedi-1.ged");
        }

        [TestMethod]
        public void ValidateTestFileRela()
        {
            ValidateGedcomFile("../../../../external/test-files/7/rela_1.ged");
        }

        [TestMethod]
        public void ValidateTestFileSour()
        {
            ValidateGedcomFile("../../../../external/test-files/7/sour-1.ged");
        }

        [TestMethod]
        public void ValidateTestFileTiny()
        {
            ValidateGedcomFile("../../../../external/test-files/7/tiny-1.ged");
        }

        [TestMethod]
        public void ValidateTestFileXrefCase()
        {
            ValidateGedcomFile("../../../../external/test-files/7/xref-case.ged");
        }
    }
}
