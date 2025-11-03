// Copyright (c) Armidale Software
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;

namespace Gedcom551
{
    public class CalendarSchema
    {
        private static void AddOldCalendar(string key, List<string> months)
        {
            var dictionary = new Dictionary<object, object>();
            dictionary["label"] = key;
            dictionary["uri"] = key;
            dictionary["standard tag"] = key;
            dictionary["epochs"] = new List<object> { "B.C." };
            var monthObjects = new List<object>();
            for (int i = 0; i < months.Count; i++)
            {
                string month = months[i];
                if (string.IsNullOrEmpty(month))
                {
                    throw new ArgumentException("Month cannot be null or empty", nameof(months));
                }
                monthObjects.Add("https://gedcom.io/terms/v7/month-" + month);
            }
            dictionary["months"] = monthObjects;
            GedcomCommon.CalendarSchema.AddOldCalendar(dictionary);
        }

        /// <summary>
        /// Manually construct 5.5.1 calendars.
        /// </summary>
        public static void AddOldCalendars()
        {
            AddOldCalendar("@#DFRENCH R@", new List<string>
            {
                "VEND", "BRUM", "FRIM", "NIVO", "PLUV", "VENT",
                "GERM", "FLOR", "PRAI", "MESS", "THER", "FRUC",
                "COMP"
            });
            AddOldCalendar("@#DGREGORIAN@", new List<string>
            {
                "JAN", "FEB", "MAR", "APR", "MAY", "JUN",
                "JUL", "AUG", "SEP", "OCT", "NOV", "DEC"
            });
            AddOldCalendar("@#DHEBREW@", new List<string>
            {
                "TSH", "CSH", "KSL", "TVT", "SHV", "ADR", "ADS",
                "NSN", "IYR", "SVN", "TMZ", "AAV", "ELL"
            });
            AddOldCalendar("@#JULIAN@", new List<string>
            {
                "JAN", "FEB", "MAR", "APR", "MAY", "JUN",
                "JUL", "AUG", "SEP", "OCT", "NOV", "DEC"
            });
        }
    }
}
