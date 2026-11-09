// Copyright (c) Armidale Software
// SPDX-License-Identifier: MIT
using Gedcom7;
using GedcomCommon;
using GedcomLoader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Tests
{
    [TestClass]
    public class SchemaTests70 : SchemaTestsUtilities
    {
        public const string VersionString = "7.0";
        private const string TEST_FILES_BASE_70_PATH = "../../../../external/GEDCOM-registries/registry_tools/GEDCOM.io/testfiles/gedcom70";
        private const string TEST_FILES_REMOTE_70_PATH = "https://gedcom.io/testfiles/gedcom70/";
        private const string TEST_FILES_BASE_70_PATH2 = "../../../../external/test-files/7";

        [TestMethod]
        public void LoadStructureSchema()
        {
            GedcomStructureSchema.LoadAll(GedcomVersion.V70);
            var schema = GedcomStructureSchema.GetSchema(GedcomVersion.V70, null, GedcomStructureSchema.RecordSuperstructureUri, "HEAD", false);
            Assert.AreEqual("https://gedcom.io/terms/v7/HEAD", schema?.Uri);
            schema = GedcomStructureSchema.GetSchema(GedcomVersion.V70, null, "https://gedcom.io/terms/v7/DATA-EVEN", "DATE", false);
            Assert.AreEqual("https://gedcom.io/terms/v7/DATA-EVEN-DATE", schema?.Uri);
            schema = GedcomStructureSchema.GetSchema(GedcomVersion.V70, null, "https://gedcom.io/terms/v7/HEAD", "DATE", false);
            Assert.AreEqual("https://gedcom.io/terms/v7/HEAD-DATE", schema?.Uri);
        }

        public static void ValidateGedzipFile(string path, string expected_result = null)
        {
            var fileFactory = new GedcomFileFactory();
            var file = new GedzipFile();
            List<string> errors = file.LoadFromPath(path, fileFactory);
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
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH, "escapes.ged"));
        }

        [TestMethod]
        public void ValidateFileExtensionRecord()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH, "extension-record.ged"));
        }

        [TestMethod]
        public void ValidateFileLongUrl()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH, "long-url.ged"));
        }

        [TestMethod]
        public void ValidateFileMaximal70Lds()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH, "maximal70-lds.ged"));
        }

        [TestMethod]
        public void ValidateFileMaximal70Memories1()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH, "maximal70-memories1.ged"));
        }

        [TestMethod]
        public void ValidateFileMaximal70Memories2()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH, "maximal70-memories2.ged"));
        }

        [TestMethod]
        public void ValidateFileMaximal70Tree1()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH, "maximal70-tree1.ged"));
        }

        [TestMethod]
        public void ValidateFileMaximal70Tree2()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH, "maximal70-tree2.ged"));
        }

        [TestMethod]
        public void ValidateFileMaximal70()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH, "maximal70.ged"));
        }

        [TestMethod]
        public void ValidateRemoteFileMaximal70()
        {
            ValidateRemoteGedcomFile(TEST_FILES_REMOTE_70_PATH + "maximal70.ged");
        }

        [TestMethod]
        public void ValidateFileMaximal70Zip()
        {
            ValidateGedzipFile(Path.Combine(TEST_FILES_BASE_70_PATH, "maximal70.gdz"));
        }

        [TestMethod]
        public void ValidateFileMinimal70()
        {
            ValidateGedcomFile("minimal70.txt", "minimal70.txt must have a .ged extension");

            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH, "minimal70.ged"));
        }

        [TestMethod]
        public void ValidateFileMinimal70Zip()
        {
            ValidateGedzipFile("minimal70.zip", "minimal70.zip must have a .gdz extension");
            ValidateGedzipFile(Path.Combine(TEST_FILES_BASE_70_PATH, "minimal70.gdz"));
        }

        [TestMethod]
        public void ValidateFileRemarriage1()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH, "remarriage1.ged"));
        }

        [TestMethod]
        public void ValidateFileRemarriage2()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH, "remarriage2.ged"));
        }

        [TestMethod]
        public void ValidateFileSameSexMarriage()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH, "same-sex-marriage.ged"));
        }

        [TestMethod]
        public void ValidateFileVoidptr()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH, "voidptr.ged"));
        }

        [TestMethod]
        public void ValidateHeaderAndTrailer()
        {
            ValidateHeaderAndTrailer(GedcomVersion.V70);
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
 2 VERS 7.0
0 TRLR
", "Line 3: Line must start with an integer");
            ValidateGedcomText(@"0 HEAD
 1 GEDC
 2 VERS 7.0
0 TRLR
", "Line 2: Line must start with an integer\nLine 3: Line must start with an integer");

            ValidateSpacing(VersionString);
        }

        [TestMethod]
        public void ValidateXref()
        {
            ValidateXref(GedcomVersion.V70);

            // Test characters within an xref, which is
            // @<alphanum><pointer_string>@
            // GEDCOM 5.5.1:
            // where pointer_string has (alnum|space|#)
            // and GEDCOM 7.0
            // where pointer_string has (upper|digit|_)

            // GEDCOM 7.0 disallows @VOID@ as an actual xref id.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @VOID@ INDI
0 TRLR
", "Line 4: Xref must not be @VOID@");

            // Hash is ok in GEDCOM 5.5.1 (except at the start) but not 7.0.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @I#1@ INDI
0 TRLR
", "Line 4: Invalid character '#' in Xref \"@I#1@\"");

            // Underscore is ok in GEDCOM 7.0 but not 5.5.1.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @I_1@ INDI
0 TRLR
");

            // Lower-case letters are ok in GEDCOM 5.5.1 but not 7.0.
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
2 VERS 7.0
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
2 VERS 7.0
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
2 VERS 7.0
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

        /// <summary>
        /// Validate Name payload type.
        /// </summary>
        [TestMethod]
        public void ValidateNamePayloadType()
        {
            ValidateNamePayloadType(GedcomVersion.V70);
        }

        /// <summary>
        /// Validate exact date payload type.
        /// </summary>
        [TestMethod]
        public void ValidateExactDatePayloadType()
        {
            ValidateExactDatePayloadType(GedcomVersion.V70);
        }

        /// <summary>
        /// Validate date period payload type.
        /// </summary>
        [TestMethod]
        public void ValidateDatePeriodPayloadType()
        {
            SchemaTestsCommon.ValidateCommonDatePeriodPayloadType(GedcomVersion.V70);

            // Try some valid date period values.
            ValidateValidDatePeriodPayload(GedcomVersion.V70, "TO GREGORIAN 20 BCE");
            ValidateValidDatePeriodPayload(GedcomVersion.V70, "FROM HEBREW 1 TSH 1");
            ValidateValidDatePeriodPayload(GedcomVersion.V70, "FROM GREGORIAN 20 BCE TO GREGORIAN 12 BCE");

            // Try some invalid date period values.
            ValidateInvalidDatePeriodPayload(GedcomVersion.V70, "FROM HEBREW 1 TSH 1 BCE");
        }

        /// <summary>
        /// Validate date value payload type.
        /// </summary>
        [TestMethod]
        public void ValidateV70DateValuePayloadType()
        {
            SchemaTestsCommon.ValidateCommonDateValuePayloadType(GedcomVersion.V70);

            // Try some valid dates.
            ValidateValidDateValuePayload(GedcomVersion.V70, "GREGORIAN 20 BCE");
            ValidateValidDateValuePayload(GedcomVersion.V70, "HEBREW 1 TSH 1");

            // Try some valid date periods.
            ValidateValidDateValuePayload(GedcomVersion.V70, "TO GREGORIAN 20 BCE");
            ValidateValidDateValuePayload(GedcomVersion.V70, "FROM HEBREW 1 TSH 1");
            ValidateValidDateValuePayload(GedcomVersion.V70, "FROM GREGORIAN 20 BCE TO GREGORIAN 12 BCE");

            // Try some valid date ranges.
            ValidateValidDateValuePayload(GedcomVersion.V70, "BEF GREGORIAN 20 BCE");
            ValidateValidDateValuePayload(GedcomVersion.V70, "AFT HEBREW 1 TSH 1");
            ValidateValidDateValuePayload(GedcomVersion.V70, "BET GREGORIAN 20 BCE AND GREGORIAN 12 BCE");

            // Try some valid approximate dates.
            ValidateValidDateValuePayload(GedcomVersion.V70, "EST GREGORIAN 20 BCE");

            // Try some invalid date values.
            ValidateInvalidDateValuePayload(GedcomVersion.V70, "FROM HEBREW 1 TSH 1 BCE");
            ValidateInvalidDateValuePayload(GedcomVersion.V70, "AFT HEBREW 1 TSH 1 BCE");
        }

        /// <summary>
        /// Validate Time payload type.
        /// </summary>
        [TestMethod]
        public void ValidateTimePayloadType()
        {
            ValidateTimePayloadType(GedcomVersion.V70);
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
            ValidateValidAgePayload("> 79y");
            ValidateValidAgePayload("< 79y 1m 1w 1d");

            // Try some invalid age values.
            ValidateInvalidAgePayload(" ");
            ValidateInvalidAgePayload("invalid");
            ValidateInvalidAgePayload("d");
            ValidateInvalidAgePayload("79");
            ValidateInvalidAgePayload("1d 1m");
            ValidateInvalidAgePayload("<>1y");
            ValidateInvalidAgePayload(">79y");
            ValidateInvalidAgePayload("<79y 1m 1w 1d");
        }

        private void ValidateInvalidLanguagePayload(string value)
        {
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
1 LANG " + value + @"
0 TRLR
", "Line 4: \"" + value + "\" is not a valid language tag");
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
2 VERS 7.0
0 @O1@ OBJE
1 FILE foo
2 FORM
0 TRLR
", "Line 6: \"\" is not a valid media type");

            // Validate FORM payload.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
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
2 VERS 7.0
0 @N1@ SNOTE Test
1 MIME text/unknown
0 TRLR
");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 7.0
0 @N1@ SNOTE Test
1 MIME image/unknown
0 TRLR
", "Line 5: MIME payload must be a text type");
        }

        /// <summary>
        /// Validate payload as a pointer to recordType.
        /// </summary>
        [TestMethod]
        public void ValidateXrefPayloadType()
        {
            ValidateXrefPayloadType(VersionString);
        }

        // Test files from the test-files repository.

        [TestMethod]
        public void ValidateTestFileAtSign()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH2, "atsign.ged"));
        }

        [TestMethod]
        public void ValidateTestFileChar()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH2, "char_ascii_1.ged"));
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH2, "char_ascii_2.ged"));

            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH2, "char_utf16be-1.ged"));
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH2, "char_utf16be-2.ged"));

            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH2, "char_utf16le-1.ged"));
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH2, "char_utf16le-2.ged"));

            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH2, "char_utf8-1.ged"));
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH2, "char_utf8-2.ged"));
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH2, "char_utf8-3.ged"));
        }

        [TestMethod]
        public void ValidateTestFileDateAll()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH2, "date-all.ged"));
        }

        [TestMethod]
        [Ignore("Known issue: date-dual-valid.ged validation not implemented yet")]
        public void ValidateTestFileDateDualValid()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH2, "date-dual-valid.ged"));
        }

        [TestMethod]
        public void ValidateTestFileEnumExt()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH2, "enum-ext.ged"));
        }

        [TestMethod]
        public void ValidateTestFileFilename()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH2, "filename-1.ged"));
        }

        [TestMethod]
        public void ValidateTestFileLangAll()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH2, "lang-all.ged"));
        }

        [TestMethod]
        public void ValidateTestFileNotes()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH2, "notes-1.ged"));
        }

        [TestMethod]
        public void ValidateTestFileObje()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH2, "obje-1.ged"));
        }

        [TestMethod]
        public void ValidateTestFileObsolete()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH2, "obsolete-1.ged"));
        }

        [TestMethod]
        public void ValidateTestFilePedi()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH2, "pedi-1.ged"));
        }

        [TestMethod]
        public void ValidateTestFileRela()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH2, "rela_1.ged"));
        }

        [TestMethod]
        public void ValidateTestFileSour()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH2, "sour-1.ged"));
        }

        [TestMethod]
        public void ValidateTestFileTiny()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH2, "tiny-1.ged"));
        }

        [TestMethod]
        public void ValidateTestFileXrefCase()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_70_PATH2, "xref-case.ged"));
        }
    }
}
