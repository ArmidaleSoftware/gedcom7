// Copyright (c) Armidale Software
// SPDX-License-Identifier: MIT
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;

namespace GedcomCommon
{
    public class CalendarSchema
    {
        public string Uri { get; private set; }
        public string Label { get; private set; }
        public string StandardTag { get; private set; }
        public override string ToString() => this.Label;
        public List<string> MonthUris { get; private set; }
        public List<string> MonthTags { get; private set; }
        public List<string> Epochs { get; private set; }
        CalendarSchema(Dictionary<object, object> dictionary)
        {
            this.Uri = dictionary["uri"] as string;
            this.Label = dictionary["label"] as string;
            this.StandardTag = dictionary["standard tag"] as string;
            this.Epochs = new List<string>();
            GedcomStructureSchema.AddStrings(this.Epochs, dictionary["epochs"] as List<Object>);
            this.MonthUris = new List<string>();
            GedcomStructureSchema.AddStrings(this.MonthUris, dictionary["months"] as List<Object>);

            this.MonthTags = new List<string>();
            foreach (var uri in this.MonthUris)
            {
                MonthSchema value = MonthSchema.GetMonth(uri);
                if (value != null)
                {
                    this.MonthTags.Add(value.StandardTag);
                    continue;
                }
            }
        }

        static Dictionary<string, CalendarSchema> s_CalendarsByTag = new Dictionary<string, CalendarSchema>();

        /// <summary>
        /// Check whether this schema applies to a given GEDCOM version.
        /// </summary>
        /// <param name="version">GEDCOM version</param>
        /// <returns>true if applies, false if not</returns>
        private bool HasVersion(GedcomVersion version) => GedcomStructureSchema.UriHasVersion(this.Uri, version);

        public static void LoadAll(GedcomVersion version, string gedcomRegistriesPath)
        {
            if (s_CalendarsByTag.Count > 0)
            {
                return;
            }
            MonthSchema.LoadAll(gedcomRegistriesPath);
            var path = Path.Combine(gedcomRegistriesPath, "calendar/standard");
            string[] files;
            try
            {
                files = Directory.GetFiles(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }
            foreach (string filename in files)
            {
                var deserializer = new DeserializerBuilder().Build();
                using var reader = new StreamReader(filename);
                var dictionary = deserializer.Deserialize<Dictionary<object, object>>(reader);
                var schema = new CalendarSchema(dictionary);
                if (!schema.HasVersion(version))
                {
                    continue;
                }
                s_CalendarsByTag.Add(schema.StandardTag, schema);
            }
        }

        public static void AddOldCalendar(Dictionary<object, object> dictionary)
        {
            var schema = new CalendarSchema(dictionary);
            s_CalendarsByTag.Add(schema.StandardTag, schema);
        }

        public static CalendarSchema GetCalendarByTag(string tag) => s_CalendarsByTag[tag];

        public bool IsValidMonth(string value)
        {
            return this.MonthTags.Contains(value);
        }

        public bool IsValidEpoch(string value)
        {
            return this.Epochs.Contains(value);
        }
    }
}
