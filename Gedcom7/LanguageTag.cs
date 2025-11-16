// Copyright (c) Armidale Software
// SPDX-License-Identifier: MIT
using GedcomCommon;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Gedcom7
{
    public class LanguageTag
    {

        /// <summary>
        /// Test whether a given string is a valid language tag.
        /// </summary>
        /// <param name="value">String to test</param>
        /// <returns>true if valid, false if not</returns>
        private static bool IsValidLanguageTag(string value)
        {
            if (value == null || value.Length == 0) return false;
            Regex regex = new Regex(@"^\w+(-\w+)*$");
            return regex.IsMatch(value);
        }

        /// <summary>
        /// Validates the language tag in the provided GedcomStructure.
        /// </summary>
        /// <param name="s">The GedcomStructure to validate.</param>
        /// <returns>An error message string if validation fails, or null if validation succeeds.</returns>
        public static string ValidateLanguageTag(GedcomStructure s)
        {
            Debug.Assert(s.File.GedcomVersion != GedcomVersion.V551, "LanguageTag.IsValidLanguageTag called with wrong GedcomVersion");
            if (!IsValidLanguageTag(s.LineVal))
            {
                return s.ErrorMessage("\"" + s.LineVal + "\" is not a valid language tag");
            }
            return null;
        }
    }
}
