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
    public class SchemaTestsUtilities
    {
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

        public static void ValidateRemoteGedcomFile(string url, string expected_result = null)
        {
            var file = new GedcomFile();
            List<string> errors = file.LoadFromUrl(url);
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

        protected static void ValidateXref(GedcomVersion version)
        {
            string versionString = GetGedcomVersionString(version);
            string additional = GetGedcomVersionHeaderAdditions(version);
            int additionalLines = additional.Split('\n').Count() - 1;

            // HEAD pseudo-structure does not allow an xref.
            ValidateGedcomText(@"0 @H1@ HEAD
1 GEDC
2 VERS " + versionString + additional + @"
0 TRLR
", "Line 1: Xref is not valid for HEAD");

            // Test an INDI record without an xref.  The spec says:
            // "Each record to which other structures point must have
            // a cross-reference identifier. A record to which no
            // structures point may have a cross-reference identifier,
            // but does not need to have one. A substructure or pseudo-
            // structure must not have a cross-reference identifier."
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS " + versionString + additional + @"
0 INDI
0 TRLR
");

            // TRLR pseudo-structure does not allow an xref.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS " + versionString + additional + @"
0 @T1@ TRLR
", "Line " + (4 + additionalLines) + ": Xref is not valid for TRLR");

            // Xref must start with @.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS " + versionString + additional + @"
0 I1@ INDI
0 TRLR
", "Line " + (4 + additionalLines) + ": Undocumented standard record");

            // Xref must end with @.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS " + versionString + additional + @"
0 @I1 INDI
0 TRLR
", "Line " + (4 + additionalLines) + ": Xref must start and end with @");

            // Xref must contain something.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS " + versionString + additional + @"
0 @ INDI
0 TRLR
", "Line " + (4 + additionalLines) + ": Xref must start and end with @");

            // Upper case letters and numbers are fine.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS " + versionString + additional + @"
0 @I1@ INDI
0 TRLR
");
        }

        public static string GetGedcomVersionString(GedcomVersion version)
        {
            switch (version)
            {
                case GedcomVersion.V551: return SchemaTests551.VersionString;
                case GedcomVersion.V70: return SchemaTests70.VersionString;
                default: throw new NotSupportedException();
            }
        }

        protected static void ValidateHeaderAndTrailer(GedcomVersion version)
        {
            string additions = GetGedcomVersionHeaderAdditions(version);
            int additionalLines = additions.Split('\n').Count() - 1;

            // Missing TRLR.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS " + GetGedcomVersionString(version) + additions + @"
", "Missing TRLR record");

            // Minimal valid.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS " + GetGedcomVersionString(version) + additions + @"
0 TRLR
");

            // Backwards order.
            ValidateGedcomText(@"0 TRLR
0 HEAD
1 GEDC
2 VERS " + GetGedcomVersionString(version) + additions + @"
", "Line 1: HEAD must be the first record");

            // The trailer cannot contain substructures.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS " + GetGedcomVersionString(version) + additions + @"
0 TRLR
1 _EXT bad
", "Line " + (4 + additionalLines) + ": TRLR must not contain substructures");

            // Two HEADs.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS " + GetGedcomVersionString(version) + additions + @"
0 HEAD
1 GEDC
2 VERS " + GetGedcomVersionString(version) + @"
0 TRLR
", "Line " + (4 + additionalLines) + ": HEAD must be the first record");

            // Two TRLRs.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS " + GetGedcomVersionString(version) + additions + @"
0 TRLR
0 TRLR
", "Line " + (5 + additionalLines) + ": Duplicate TRLR record");
        }

        protected static void ValidateSpacing(string versionString)
        {
            // Extra space before the tag is not valid.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2  VERS " + versionString + @"
0 TRLR
", "Line 3: Tag must not be empty");

            // Trailing whitespace is not valid.
            ValidateGedcomText(@"0 HEAD
1 GEDC " + @"
2 VERS " + versionString + @"
0 TRLR
", "Line 2: An empty payload is not valid after a space");
        }

        protected static void ValidateValidNamePayload(GedcomVersion version, string value)
        {
            string versionString = GetGedcomVersionString(version);
            string additions = GetGedcomVersionHeaderAdditions(version);

            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS " + versionString + additions + @"
0 @I1@ INDI
1 NAME " + value + @"
0 TRLR
");
        }

        protected static void ValidateInvalidNamePayload(GedcomVersion version, string value)
        {
            string versionString = GetGedcomVersionString(version);
            string additions = GetGedcomVersionHeaderAdditions(version);
            int additionalLines = additions.Split('\n').Count() - 1;

            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS " + versionString + additions + @"
0 @I1@ INDI
1 NAME " + value + @"
", "Line " + (5 + additionalLines) + ": \"" + value + "\" is not a valid name");
        }

        /// <summary>
        /// Validate Name payload type.
        /// </summary>
        protected static void ValidateNamePayloadType(GedcomVersion version)
        {
            // Try some valid name values.
            ValidateValidNamePayload(version, "John Smith");
            ValidateValidNamePayload(version, "John /Smith/");
            ValidateValidNamePayload(version, "John /Smith/ Jr.");

            // Try some invalid name values.
            ValidateInvalidNamePayload(version, "/");
            ValidateInvalidNamePayload(version, "a/b/c/d");
            ValidateInvalidNamePayload(version, "a\tb");
        }

        protected static string GetGedcomVersionHeaderAdditions(GedcomVersion version)
        {
            if (version != GedcomVersion.V551)
            {
                return string.Empty;
            }
            return @"
2 FORM LINEAGE-LINKED
1 SOUR Test
1 CHAR ASCII
1 SUBM @S1@
0 @S1@ SUBM
1 NAME Test";
        }

        protected static void ValidateValidDateValuePayload(GedcomVersion version, string value)
        {
            if (version == GedcomVersion.All)
            {
                ValidateValidDateValuePayload(GedcomVersion.V551, value);
                ValidateValidDateValuePayload(GedcomVersion.V70, value);
                ValidateValidDateValuePayload(GedcomVersion.V71, value);
                return;
            }
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS " + GetGedcomVersionString(version) + GetGedcomVersionHeaderAdditions(version) + @"
0 @I1@ INDI
1 DEAT
2 DATE " + value + @"
0 TRLR
");
        }

        protected static void ValidateInvalidDateValuePayload(GedcomVersion version, string value)
        {
            if (version == GedcomVersion.All)
            {
                ValidateInvalidDateValuePayload(GedcomVersion.V551, value);
                ValidateInvalidDateValuePayload(GedcomVersion.V70, value);
                ValidateInvalidDateValuePayload(GedcomVersion.V71, value);
                return;
            }

            string versionString = GetGedcomVersionString(version);
            string additions = GetGedcomVersionHeaderAdditions(version);
            int additionalLines = additions.Split('\n').Count() - 1;

            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS " + versionString + additions + @"
0 @I1@ INDI
1 DEAT
2 DATE " + value + @"
0 TRLR
", "Line " + (6 + additionalLines) + ": \"" + value + "\" is not a valid date value");
        }

        private void ValidateInvalidExactDatePayload(GedcomVersion version, string value)
        {
            string versionString = GetGedcomVersionString(version);
            string additions = GetGedcomVersionHeaderAdditions(version);

            ValidateGedcomText(@"0 HEAD
1 DATE " + value + @"
1 GEDC
2 VERS " + versionString + additions + @"
", "Line 2: \"" + value + "\" is not a valid exact date");
        }

        private void ValidateValidExactDatePayload(GedcomVersion version, string value)
        {
            string versionString = GetGedcomVersionString(version);
            string additions = GetGedcomVersionHeaderAdditions(version);

            ValidateGedcomText(@"0 HEAD
1 DATE " + value + @"
1 GEDC
2 VERS " + versionString + additions + @"
0 TRLR
");
        }

        /// <summary>
        /// Validate exact date payload type.
        /// </summary>
        protected void ValidateExactDatePayloadType(GedcomVersion version)
        {
            // Try some valid date values.
            ValidateValidExactDatePayload(version, "3 DEC 2023");
            ValidateValidExactDatePayload(version, "03 DEC 2023");

            // Try some invalid date values.
            ValidateInvalidExactDatePayload(version, "invalid");
            ValidateInvalidExactDatePayload(version, "3 dec 2023");
            ValidateInvalidExactDatePayload(version, "3 JUNE 2023");
            ValidateInvalidExactDatePayload(version, "DEC 2023");
            ValidateInvalidExactDatePayload(version, "2023");
        }

        protected static void ValidateInvalidDatePeriodPayload(GedcomVersion version, string value)
        {
            if (version == GedcomVersion.All)
            {
                ValidateInvalidDatePeriodPayload(GedcomVersion.V551, value);
                ValidateInvalidDatePeriodPayload(GedcomVersion.V70, value);
                ValidateInvalidDatePeriodPayload(GedcomVersion.V71, value);
                return;
            }

            string versionString = GetGedcomVersionString(version);
            string additions = GetGedcomVersionHeaderAdditions(version);
            int additionalLines = additions.Split('\n').Count() - 1;

            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS " + versionString + additions + @"
0 @I1@ SOUR
1 DATA
2 EVEN MARR
3 DATE " + value + @"
0 TRLR
", "Line " + (7 + additionalLines) + ": \"" + value + "\" is not a valid date period");
        }

        protected static void ValidateValidDatePeriodPayload(GedcomVersion version, string value)
        {
            if (version == GedcomVersion.All)
            {
                ValidateValidDatePeriodPayload(GedcomVersion.V551, value);
                ValidateValidDatePeriodPayload(GedcomVersion.V70, value);
                ValidateValidDatePeriodPayload(GedcomVersion.V71, value);
                return;
            }

            string versionString = GetGedcomVersionString(version);
            string additions = GetGedcomVersionHeaderAdditions(version);

            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS " + versionString + additions + @"
0 @I1@ SOUR
1 DATA
2 EVEN MARR
3 DATE " + value + @"
0 TRLR
");
        }

        private void ValidateInvalidTimePayload(GedcomVersion version, string value)
        {
            string versionString = GetGedcomVersionString(version);
            string additions = GetGedcomVersionHeaderAdditions(version);

            ValidateGedcomText(@"0 HEAD
1 DATE 1 DEC 2023
2 TIME " + value + @"
1 GEDC
2 VERS " + versionString + additions + @"
0 TRLR
", "Line 3: \"" + value + "\" is not a valid time");
        }

        private void ValidateValidTimePayload(GedcomVersion version, string value)
        {
            string versionString = GetGedcomVersionString(version);
            string additional = GetGedcomVersionHeaderAdditions(version);

            ValidateGedcomText(@"0 HEAD
1 DATE 1 DEC 2023
2 TIME " + value + @"
1 GEDC
2 VERS " + versionString + additional + @"
0 TRLR
");
        }

        /// <summary>
        /// Validate Time payload type.
        /// </summary>
        protected void ValidateTimePayloadType(GedcomVersion version)
        {
            // Try some valid time values.
            ValidateValidTimePayload(version, "02:50");
            ValidateValidTimePayload(version, "2:50");
            ValidateValidTimePayload(version, "2:50:00.00Z");

            // Try some invalid time values.
            ValidateInvalidTimePayload(version, " ");
            ValidateInvalidTimePayload(version, "invalid");
            ValidateInvalidTimePayload(version, "000:00");
            ValidateInvalidTimePayload(version, "24:00:00");
            ValidateInvalidTimePayload(version, "2:5");
            ValidateInvalidTimePayload(version, "2:60");
            ValidateInvalidTimePayload(version, "2:00:60");
        }

        /// <summary>
        /// Validate payload as a pointer to recordType.
        /// </summary>
        protected void ValidateXrefPayloadType(string versionString)
        {
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS " + versionString + @"
1 SUBM @S1@
0 @S1@ SUBM
1 NAME Test
1 OBJE @O1
0 TRLR
", "Line 7: Payload must be a pointer");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS " + versionString + @"
1 SUBM
0 TRLR
", "Line 4: Payload must be a pointer");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS " + versionString + @"
1 SUBM S1@
0 TRLR
", "Line 4: Payload must be a pointer");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS " + versionString + @"
1 SUBM @S1@
0 TRLR
", "Line 4: @S1@ has no associated record");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS " + versionString + @"
1 SUBM @I1@
0 @I1@ INDI
0 TRLR
", "Line 4: SUBM points to a INDI record");
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS " + versionString + @"
1 SUBM @I1@
0 @I1@ _SUBM
0 TRLR
", "Line 4: SUBM points to a _SUBM record");

            // We can't validate the record type for an
            // undocumented extension.
            ValidateGedcomText(@"0 HEAD
1 GEDC
2 VERS " + versionString + @"
1 _SUBM @I1@
0 @I1@ INDI
0 TRLR
");
        }
    }

    [TestClass]
    public class SchemaTestsCommon : SchemaTestsUtilities
    {

        [TestMethod]
        public void ValidateHeaderAndTrailer()
        {
            // Missing HEAD.
            ValidateGedcomText("0 TRLR\n", "Line 1: HEAD must be the first record");

            // No records.
            ValidateGedcomText("", "Missing TRLR record");
        }

        /// <summary>
        /// Validate date value payload type.
        /// </summary>
        public static void ValidateCommonDateValuePayloadType(GedcomVersion version)
        {
            // Try some valid dates.
            ValidateValidDateValuePayload(version, "3 DEC 2023");
            ValidateValidDateValuePayload(version, "DEC 2023");
            ValidateValidDateValuePayload(version, "2023");

            // Try some valid date periods.
            ValidateValidDateValuePayload(version, "TO 3 DEC 2023");
            ValidateValidDateValuePayload(version, "TO DEC 2023");
            ValidateValidDateValuePayload(version, "TO 2023");
            ValidateValidDateValuePayload(version, "FROM 03 DEC 2023");
            ValidateValidDateValuePayload(version, "FROM 2000 TO 2020");
            ValidateValidDateValuePayload(version, "FROM MAR 2000 TO JUN 2000");
            ValidateValidDateValuePayload(version, "FROM 30 NOV 2000 TO 1 DEC 2000");

            // Try some valid date ranges.
            ValidateValidDateValuePayload(version, "BEF 3 DEC 2023");
            ValidateValidDateValuePayload(version, "BEF DEC 2023");
            ValidateValidDateValuePayload(version, "BEF 2023");
            ValidateValidDateValuePayload(version, "AFT 03 DEC 2023");
            ValidateValidDateValuePayload(version, "BET 2000 AND 2020");
            ValidateValidDateValuePayload(version, "BET MAR 2000 AND JUN 2000");
            ValidateValidDateValuePayload(version, "BET 30 NOV 2000 AND 1 DEC 2000");

            // Try some valid approximate dates.
            ValidateValidDateValuePayload(version, "ABT 3 DEC 2023");
            ValidateValidDateValuePayload(version, "CAL DEC 2023");

            // Try some invalid date values.
            ValidateInvalidDateValuePayload(version, "TO 40 DEC 2023");
            ValidateInvalidDateValuePayload(version, "TO 3 dec 2023");
            ValidateInvalidDateValuePayload(version, "TO 3 JUNE 2023");
            ValidateInvalidDateValuePayload(version, "TO ABC 2023");
            ValidateInvalidDateValuePayload(version, "BEF 40 DEC 2023");
            ValidateInvalidDateValuePayload(version, "BEF 3 dec 2023");
            ValidateInvalidDateValuePayload(version, "BEF 3 JUNE 2023");
            ValidateInvalidDateValuePayload(version, "BEF ABC 2023");
            ValidateInvalidDateValuePayload(version, "BET 2000");
        }

        /// <summary>
        /// Validate date period payload type.
        /// </summary>
        public static void ValidateCommonDatePeriodPayloadType(GedcomVersion version)
        {
            // Try some valid date period values.
            ValidateValidDatePeriodPayload(version, "TO 3 DEC 2023");
            ValidateValidDatePeriodPayload(version, "TO DEC 2023");
            ValidateValidDatePeriodPayload(version, "TO 2023");
            ValidateValidDatePeriodPayload(version, "FROM 03 DEC 2023");
            ValidateValidDatePeriodPayload(version, "FROM 2000 TO 2020");
            ValidateValidDatePeriodPayload(version, "FROM MAR 2000 TO JUN 2000");
            ValidateValidDatePeriodPayload(version, "FROM 30 NOV 2000 TO 1 DEC 2000");

            // Try some invalid date period values.
            ValidateInvalidDatePeriodPayload(version, "2023");
            ValidateInvalidDatePeriodPayload(version, "TO 40 DEC 2023");
            ValidateInvalidDatePeriodPayload(version, "TO 3 dec 2023");
            ValidateInvalidDatePeriodPayload(version, "TO 3 JUNE 2023");
            ValidateInvalidDatePeriodPayload(version, "TO ABC 2023");
        }
    }

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
                "Line 12: \"mp3\" is not a valid value for FORM\nLine 19: \"other\" is not a valid value for FORM\nLine 22: \"other\" is not a valid value for FORM");
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
