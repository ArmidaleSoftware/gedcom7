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
                case GedcomVersion.V71: return SchemaTests71.VersionString;
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
}
