// Copyright (c) Armidale Software
// SPDX-License-Identifier: MIT
using GedcomCommon;
using System;
using System.Diagnostics;

namespace Gedcom551
{
    public class LanguageId
    {

        /// <summary>
        /// Test whether a given string is a valid GEDCOM 5.5.1 language value.
        /// </summary>
        /// <param name="value">String to test</param>
        /// <returns>true if valid, false if not</returns>
        private static bool IsValidLanguageId(string value)
        {
            string[] languages =
            {
                "Afrikaans", "Albanian", "Anglo-Saxon", "Catalan", "Catalan_Spn",
                "Czech", "Danish", "Dutch", "English", "Esperanto", "Estonian",
                "Faroese", "Finnish", "French", "German", "Hawaiian", "Hungarian",
                "Icelandic", "Indonesian", "Italian", "Latvian", "Lithuanian",
                "Navaho", "Norwegian", "Polish", "Portuguese", "Romanian",
                "Serbo_Croa", "Slovak", "Slovene", "Spanish", "Swedish",
                "Turkish", "Wendic", "Amharic", "Arabic", "Armenian",
                "Assamese", "Belorusian", "Bengali", "Braj", "Bulgarian",
                "Burmese", "Cantonese", "Church-Slavic", "Dogri", "Georgian",
                "Greek", "Gujarati", "Hebrew", "Hindi", "Japanese", "Kannada",
                "Khmer", "Konkani", "Korean", "Lahnda", "Lao", "Macedonian",
                "Maithili", "Malayalam", "Mandrin", "Manipuri", "Marathi",
                "Mewari", "Nepali", "Oriya", "Pahari", "Pali", "Panjabi",
                "Persian", "Prakrit", "Pusto", "Rajasthani", "Russian",
                "Sanskrit", "Serb", "Tagalog", "Tamil", "Telugu", "Thai",
                "Tibetan", "Ukrainian", "Urdu", "Vietnamese", "Yiddish ",
                "Mandarin"
            };
            return Array.Exists(languages, lang => lang.Equals(value, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="s">Gedcom structure</param>
        /// <returns></returns>
        public static string ValidateLanguageId(GedcomStructure s)
        {
            Debug.Assert(s.File.GedcomVersion == GedcomVersion.V551, "LanguageTag.IsValidLanguageTag called with wrong GedcomVersion");
            if (!IsValidLanguageId(s.LineVal))
            {
                return s.ErrorMessage("\"" + s.LineVal + "\" is not a valid language ID");
            }
            return null;
        }
    }
}
