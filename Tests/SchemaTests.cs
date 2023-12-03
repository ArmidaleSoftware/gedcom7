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

        void ValidateGedcomFile(string path, string expected_result = null)
        {
            var file = new GedcomFile();
            List<string> errors = file.LoadFromPath(path);
            string error = null;
            if (errors.Count == 0)
            {
                errors.AddRange(file.Validate());
            }
            else
            {
                error = string.Join("\n", errors);
            }
            Assert.AreEqual(expected_result, error);
        }

        void ValidateGedcomText(string text, string expected_result = null)
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
        public void ValidateFileMinimal70()
        {
            ValidateGedcomFile("../../../../external/GEDCOM.io/testfiles/gedcom70/minimal70.ged");
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

            // TODO: validate exact date payload
            // TODO: validate Date payload
            // TODO: validate date period payload
            // TODO: validate Name payload

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
    }
}
