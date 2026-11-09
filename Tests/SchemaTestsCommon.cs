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
}
