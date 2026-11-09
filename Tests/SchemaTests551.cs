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
    public class SchemaTests551 : SchemaTestsUtilities
    {
        public const string VersionString = "5.5.1";
        private const string TEST_FILES_BASE_551_PATH2 = "../../../../external/test-files/5";

        [TestMethod]
        public void ValidateHeaderAndTrailer()
        {
            ValidateHeaderAndTrailer(GedcomVersion.V551);
        }

        [TestMethod]
        public void ValidateXref()
        {
            ValidateXref(GedcomVersion.V551);

            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
2 FORM LINEAGE-LINKED
1 SOUR Test
1 CHAR ASCII
1 SUBM @S1@
0 @S1@ SUBM
1 NAME Test
0 @VOID@ INDI
0 TRLR
");

            // Hash is ok in GEDCOM 5.5.1 (except at the start) but not 7.0.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
2 FORM LINEAGE-LINKED
1 SOUR Test
1 CHAR ASCII
1 SUBM @S1@
0 @S1@ SUBM
1 NAME Test
0 @I#1@ INDI
0 TRLR
");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
2 FORM LINEAGE-LINKED
1 SOUR Test
1 CHAR ASCII
1 SUBM @S1@
0 @S1@ SUBM
1 NAME Test
0 @#I1@ INDI
0 TRLR
", "Line 10: Xref \"@#I1@\" does not start with a letter or digit");

            // Underscore is ok in GEDCOM 7.0 but not 5.5.1.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
2 FORM LINEAGE-LINKED
1 SOUR Test
1 CHAR ASCII
1 SUBM @S1@
0 @S1@ SUBM
1 NAME Test
0 @I_1@ INDI
0 TRLR
", "Line 10: Invalid character '_' in Xref \"@I_1@\"");

            // Lower-case letters are ok in GEDCOM 5.5.1 but not 7.0.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
2 FORM LINEAGE-LINKED
1 SOUR Test
1 CHAR ASCII
1 SUBM @S1@
0 @S1@ SUBM
1 NAME Test
0 @i1@ INDI
0 TRLR
");
        }

        /// <summary>
        /// Validate date value payload type.
        /// </summary>
        [TestMethod]
        public void ValidateV551DateValuePayloadType()
        {
            SchemaTestsCommon.ValidateCommonDateValuePayloadType(GedcomVersion.V551);

            // Try some valid dates.
            ValidateValidDateValuePayload(GedcomVersion.V551, "1740/41");
            ValidateValidDateValuePayload(GedcomVersion.V551, "@#DGREGORIAN@ 1740/41");
            ValidateValidDateValuePayload(GedcomVersion.V551, "@#DGREGORIAN@ 20 B.C.");
            ValidateValidDateValuePayload(GedcomVersion.V551, "@#DHEBREW@ 1 TSH 1");

            // Try some valid date periods.
            ValidateValidDateValuePayload(GedcomVersion.V551, "TO @#DGREGORIAN@ 20 B.C.");
            ValidateValidDateValuePayload(GedcomVersion.V551, "FROM @#DHEBREW@ 1 TSH 1");
            ValidateValidDateValuePayload(GedcomVersion.V551, "FROM @#DGREGORIAN@ 20 B.C. TO @#DGREGORIAN@ 12 B.C.");

            // Try some valid date ranges.
            ValidateValidDateValuePayload(GedcomVersion.V551, "BEF @#DGREGORIAN@ 20 B.C.");
            ValidateValidDateValuePayload(GedcomVersion.V551, "AFT @#DHEBREW@ 1 TSH 1");
            ValidateValidDateValuePayload(GedcomVersion.V551, "BET @#DGREGORIAN@ 20 B.C. AND @#DGREGORIAN@ 12 B.C.");

            // Try some valid approximate dates.
            ValidateValidDateValuePayload(GedcomVersion.V551, "EST @#DGREGORIAN@ 20 B.C.");

            // Try some invalid date values.
            ValidateInvalidDateValuePayload(GedcomVersion.V551, "FROM @#DHEBREW@ 1 TSH 1 B.C.");
            ValidateInvalidDateValuePayload(GedcomVersion.V551, "AFT @#DHEBREW@ 1 TSH 1 B.C.");
        }

        /// <summary>
        /// Validate date period payload type.
        /// </summary>
        [TestMethod]
        public void ValidateDatePeriodPayloadType()
        {
            SchemaTestsCommon.ValidateCommonDatePeriodPayloadType(GedcomVersion.V551);

            // Try some valid date period values.
            ValidateValidDatePeriodPayload(GedcomVersion.V551, "TO @#DGREGORIAN@ 20 B.C.");
            ValidateValidDatePeriodPayload(GedcomVersion.V551, "FROM @#DHEBREW@ 1 TSH 1");
            ValidateValidDatePeriodPayload(GedcomVersion.V551, "FROM @#DGREGORIAN@ 20 B.C. TO @#DGREGORIAN@ 12 B.C.");

            // Try some invalid date period values.
            ValidateInvalidDatePeriodPayload(GedcomVersion.V551, "FROM @#DHEBREW@ 1 TSH 1 B.C.");
        }

        [TestMethod]
        public void ValidatePayloadType()
        {
            string additions = GetGedcomVersionHeaderAdditions(GedcomVersion.V551);

            // Validate null.
            ValidateGedcomText(@"0 HEAD
1 GEDC 1
2 VERS 5.5.1" + additions + @"
0 TRLR
", "Line 2: GEDC payload must be null");

            // Validate an integer.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1" + additions + @"
0 @I1@ INDI
1 NCHI 0
0 TRLR
");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1" + additions + @"
0 @I1@ INDI
1 NCHI -1
0 TRLR
", "Line 11: \"-1\" is not a non-negative integer");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1" + additions + @"
0 @I1@ INDI
1 NCHI
0 TRLR
", "Line 11: \"\" is not a non-negative integer");

            // Test Y|<NULL>.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1" + additions + @"
0 @I1@ INDI
1 BIRT N
0 TRLR
", "Line 11: BIRT payload must be 'Y' or empty");
        }

        [TestMethod]
        public void ValidateSpacing()
        {
            // Leading whitespace is valid prior to 7.0 but not in 7.0.
            ValidateGedcomText(@"0 HEAD
1 GEDC
 2 VERS 5.5.1
 2 FORM LINEAGE-LINKED
1 SOUR Test
1 CHAR ASCII
1 SUBM @S1@
0 @S1@ SUBM
1 NAME Test
0 TRLR
");

            // Extra space before the tag is not valid.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2  VERS 5.5.1
2 FORM LINEAGE-LINKED
1 SOUR Test
1 CHAR ASCII
1 SUBM @S1@
0 @S1@ SUBM
1 NAME Test
0 TRLR
", "Line 3: Tag must not be empty");

            ValidateSpacing(VersionString);
        }

        /// <summary>
        /// Validate Name payload type.
        /// </summary>
        [TestMethod]
        public void ValidateNamePayloadType()
        {
            ValidateNamePayloadType(GedcomVersion.V551);
        }

        /// <summary>
        /// Validate exact date payload type.
        /// </summary>
        [TestMethod]
        public void ValidateExactDatePayloadType()
        {
            ValidateExactDatePayloadType(GedcomVersion.V551);
        }

        /// <summary>
        /// Validate Time payload type.
        /// </summary>
        [TestMethod]
        public void ValidateTimePayloadType()
        {
            ValidateTimePayloadType(GedcomVersion.V551);
        }

        // Test files from the test-files repository.

        [TestMethod]
        public void ValidateTestFileAtSign()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_551_PATH2, "atsign.ged"));
        }

        [TestMethod]
        public void ValidateTestFileCharAscii()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_551_PATH2, "char_ascii_1.ged"));
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_551_PATH2, "char_ascii_2.ged"),
                "Line 7: \"LATIN1\" is not a valid value for CHAR");
        }

        [TestMethod]
        [Ignore("Known issue: UTF-16BE validation not implemented yet")]
        public void ValidateTestFileCharUtf16BE()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_551_PATH2, "char_utf16be-1.ged"));
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_551_PATH2, "char_utf16be-2.ged"));
        }

        [TestMethod]
        [Ignore("Known issue: UTF-16LE validation not implemented yet")]
        public void ValidateTestFileCharUtf16LE()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_551_PATH2, "char_utf16le-1.ged"));
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_551_PATH2, "char_utf16le-2.ged"));
        }

        [TestMethod]
        public void ValidateTestFileCharUtf8()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_551_PATH2, "char_utf8-1.ged"));
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_551_PATH2, "char_utf8-2.ged"));
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_551_PATH2, "char_utf8-3.ged"));
        }

        [TestMethod]
        [Ignore("Known issue: date-all.ged validation not implemented yet")]
        public void ValidateTestFileDateAll()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_551_PATH2, "date-all.ged"));
        }

        [TestMethod]
        public void ValidateTestFileDateDualValid()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_551_PATH2, "date-dual-valid.ged"));
        }

        [TestMethod]
        public void ValidateTestFileEnumExt()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_551_PATH2, "enum-ext.ged"));
        }

        [TestMethod]
        public void ValidateTestFileFilename()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_551_PATH2, "filename-1.ged"));
        }

        [TestMethod]
        public void ValidateTestFileLangAll()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_551_PATH2, "lang-all.ged"));
        }

        [TestMethod]
        public void ValidateTestFileNotes()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_551_PATH2, "notes-1.ged"));
        }

        [TestMethod]
        public void ValidateTestFileObje()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_551_PATH2, "obje-1.ged"),
                "Line 19: \"other\" is not a valid value for FORM\nLine 22: \"other\" is not a valid value for FORM");
        }

        [TestMethod]
        public void ValidateTestFileObsolete()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_551_PATH2, "obsolete-1.ged"));
        }

        [TestMethod]
        public void ValidateTestFilePedi()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_551_PATH2, "pedi-1.ged"));
        }

        [TestMethod]
        public void ValidateTestFileRela()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_551_PATH2, "rela_1.ged"));
        }

        [TestMethod]
        public void ValidateTestFileSour()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_551_PATH2, "sour-1.ged"));
        }

        [TestMethod]
        public void ValidateTestFileTiny()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_551_PATH2, "tiny-1.ged"),
                "Line 1: HEAD is missing a substructure of type https://gedcom.io/terms/v7/GEDC");
        }

        [TestMethod]
        public void ValidateTestFileXrefCase()
        {
            ValidateGedcomFile(Path.Combine(TEST_FILES_BASE_551_PATH2, "xref-case.ged"),
                "Line 3: @test@ has no associated record");
        }

        /// <summary>
        /// Validate CHILD_LINKAGE_STATUS payload type.
        /// </summary>
        [TestMethod]
        public void ValidateChildLinkageStatusPayloadType()
        {
            // Test valid values for STAT under FAMC (case insensitive).
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
2 FORM LINEAGE-LINKED
1 SOUR Test
1 CHAR ASCII
1 SUBM @S1@
0 @S1@ SUBM
1 NAME Test
0 @I1@ INDI
1 FAMC @F1@
2 STAT challenged
0 @F1@ FAM
0 TRLR
");

            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
2 FORM LINEAGE-LINKED
1 SOUR Test
1 CHAR ASCII
1 SUBM @S1@
0 @S1@ SUBM
1 NAME Test
0 @I1@ INDI
1 FAMC @F1@
2 STAT disproven
0 @F1@ FAM
0 TRLR
");

            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
2 FORM LINEAGE-LINKED
1 SOUR Test
1 CHAR ASCII
1 SUBM @S1@
0 @S1@ SUBM
1 NAME Test
0 @I1@ INDI
1 FAMC @F1@
2 STAT proven
0 @F1@ FAM
0 TRLR
");

            // Test case insensitivity.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
2 FORM LINEAGE-LINKED
1 SOUR Test
1 CHAR ASCII
1 SUBM @S1@
0 @S1@ SUBM
1 NAME Test
0 @I1@ INDI
1 FAMC @F1@
2 STAT CHALLENGED
0 @F1@ FAM
0 TRLR
");

            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
2 FORM LINEAGE-LINKED
1 SOUR Test
1 CHAR ASCII
1 SUBM @S1@
0 @S1@ SUBM
1 NAME Test
0 @I1@ INDI
1 FAMC @F1@
2 STAT Proven
0 @F1@ FAM
0 TRLR
");

            // Test invalid value.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
2 FORM LINEAGE-LINKED
1 SOUR Test
1 CHAR ASCII
1 SUBM @S1@
0 @S1@ SUBM
1 NAME Test
0 @I1@ INDI
1 FAMC @F1@
2 STAT invalid
0 @F1@ FAM
0 TRLR
", "Line 12: \"invalid\" is not a valid value for STAT");
        }

        /// <summary>
        /// Validate LDS_BAPTISM_DATE_STATUS and LDS_ENDOWMENT_DATE_STATUS payload types.
        /// </summary>
        [TestMethod]
        public void ValidateLdsBaptismEndowmentDateStatusPayloadType()
        {
            // Test valid values for BAPL STAT.
            string[] validValues = { "CHILD", "COMPLETED", "EXCLUDED", "PRE-1970", "STILLBORN", "SUBMITTED", "UNCLEARED" };
            foreach (string value in validValues)
            {
                ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
2 FORM LINEAGE-LINKED
1 SOUR Test
1 CHAR ASCII
1 SUBM @S1@
0 @S1@ SUBM
1 NAME Test
0 @I1@ INDI
1 BAPL
2 STAT " + value + @"
3 DATE 1 JAN 2000
0 TRLR
");
            }

            // Test valid values for ENDL STAT.
            string[] endlValidValues = { "CHILD", "COMPLETED", "EXCLUDED", "INFANT", "PRE-1970", "STILLBORN", "SUBMITTED", "UNCLEARED" };
            foreach (string value in endlValidValues)
            {
                ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
2 FORM LINEAGE-LINKED
1 SOUR Test
1 CHAR ASCII
1 SUBM @S1@
0 @S1@ SUBM
1 NAME Test
0 @I1@ INDI
1 ENDL
2 STAT " + value + @"
3 DATE 1 JAN 2000
0 TRLR
");
            }

            // Test case insensitivity for BAPL.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
2 FORM LINEAGE-LINKED
1 SOUR Test
1 CHAR ASCII
1 SUBM @S1@
0 @S1@ SUBM
1 NAME Test
0 @I1@ INDI
1 BAPL
2 STAT completed
3 DATE 1 JAN 2000
0 TRLR
");

            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
2 FORM LINEAGE-LINKED
1 SOUR Test
1 CHAR ASCII
1 SUBM @S1@
0 @S1@ SUBM
1 NAME Test
0 @I1@ INDI
1 BAPL
2 STAT Excluded
3 DATE 1 JAN 2000
0 TRLR
");

            // Test invalid value for BAPL.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
2 FORM LINEAGE-LINKED
1 SOUR Test
1 CHAR ASCII
1 SUBM @S1@
0 @S1@ SUBM
1 NAME Test
0 @I1@ INDI
1 BAPL
2 STAT INVALID
3 DATE 1 JAN 2000
0 TRLR
", "Line 12: \"INVALID\" is not a valid value for STAT");

            // Test that INFANT is invalid for BAPL.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
2 FORM LINEAGE-LINKED
1 SOUR Test
1 CHAR ASCII
1 SUBM @S1@
0 @S1@ SUBM
1 NAME Test
0 @I1@ INDI
1 BAPL
2 STAT INFANT
3 DATE 1 JAN 2000
0 TRLR
", "Line 12: \"INFANT\" is not a valid value for STAT");

            // Test invalid value for ENDL.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
2 FORM LINEAGE-LINKED
1 SOUR Test
1 CHAR ASCII
1 SUBM @S1@
0 @S1@ SUBM
1 NAME Test
0 @I1@ INDI
1 ENDL
2 STAT NOTVALID
3 DATE 1 JAN 2000
0 TRLR
", "Line 12: \"NOTVALID\" is not a valid value for STAT");
        }

        /// <summary>
        /// Validate LDS_CHILD_SEALING_DATE_STATUS payload type.
        /// </summary>
        [TestMethod]
        public void ValidateLdsChildSealingDateStatusPayloadType()
        {
            // Test valid values for SLGC STAT.
            string[] validValues = { "BIC", "COMPLETED", "EXCLUDED", "DNS", "PRE-1970", "STILLBORN", "SUBMITTED", "UNCLEARED" };
            foreach (string value in validValues)
            {
                ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
2 FORM LINEAGE-LINKED
1 SOUR Test
1 CHAR ASCII
1 SUBM @S1@
0 @S1@ SUBM
1 NAME Test
0 @I1@ INDI
1 SLGC
2 STAT " + value + @"
3 DATE 1 JAN 2000
0 TRLR
");
            }

            // Test case insensitivity.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
2 FORM LINEAGE-LINKED
1 SOUR Test
1 CHAR ASCII
1 SUBM @S1@
0 @S1@ SUBM
1 NAME Test
0 @I1@ INDI
1 SLGC
2 STAT bic
3 DATE 1 JAN 2000
0 TRLR
");

            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
2 FORM LINEAGE-LINKED
1 SOUR Test
1 CHAR ASCII
1 SUBM @S1@
0 @S1@ SUBM
1 NAME Test
0 @I1@ INDI
1 SLGC
2 STAT Completed
3 DATE 1 JAN 2000
0 TRLR
");

            // Test invalid value.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
2 FORM LINEAGE-LINKED
1 SOUR Test
1 CHAR ASCII
1 SUBM @S1@
0 @S1@ SUBM
1 NAME Test
0 @I1@ INDI
1 SLGC
2 STAT WRONG
3 DATE 1 JAN 2000
0 TRLR
", "Line 12: \"WRONG\" is not a valid value for STAT");
        }

        /// <summary>
        /// Validate LDS_SPOUSE_SEALING_DATE_STATUS payload type.
        /// </summary>
        [TestMethod]
        public void ValidateLdsSpouseSealingDateStatusPayloadType()
        {
            // Test valid values for SLGS STAT.
            string[] validValues = { "CANCELED", "COMPLETED", "DNS", "EXCLUDED", "DNS/CAN", "PRE-1970", "SUBMITTED", "UNCLEARED" };
            foreach (string value in validValues)
            {
                ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
2 FORM LINEAGE-LINKED
1 SOUR Test
1 CHAR ASCII
1 SUBM @S1@
0 @S1@ SUBM
1 NAME Test
0 @F1@ FAM
1 SLGS
2 STAT " + value + @"
3 DATE 1 JAN 2000
0 TRLR
");
            }

            // Test case insensitivity.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
2 FORM LINEAGE-LINKED
1 SOUR Test
1 CHAR ASCII
1 SUBM @S1@
0 @S1@ SUBM
1 NAME Test
0 @F1@ FAM
1 SLGS
2 STAT canceled
3 DATE 1 JAN 2000
0 TRLR
");

            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
2 FORM LINEAGE-LINKED
1 SOUR Test
1 CHAR ASCII
1 SUBM @S1@
0 @S1@ SUBM
1 NAME Test
0 @F1@ FAM
1 SLGS
2 STAT Dns/Can
3 DATE 1 JAN 2000
0 TRLR
");

            // Test invalid value.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS 5.5.1
2 FORM LINEAGE-LINKED
1 SOUR Test
1 CHAR ASCII
1 SUBM @S1@
0 @S1@ SUBM
1 NAME Test
0 @F1@ FAM
1 SLGS
2 STAT BADVALUE
3 DATE 1 JAN 2000
0 TRLR
", "Line 12: \"BADVALUE\" is not a valid value for STAT");
        }
    }
}
