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

        public static void LoadAll(GedcomVersion version, string gedcomRegistriesPath)
        {
            if (s_CalendarsByTag.Count > 0)
            {
                return;
            }
            MonthSchema.LoadAll(gedcomRegistriesPath);

            // Read the manifest file to get the list of calendar files for this version.
            var standardManifestPath = Path.Combine(gedcomRegistriesPath, "manifest", "standard", "manifest-" + GedcomStructureSchema.GetGedcomVersionString(version) + "-en-US.tsv");
            var calendarFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(standardManifestPath))
            {
                using var manifestReader = new StreamReader(standardManifestPath);

                // Skip header line.
                manifestReader.ReadLine();
                string line;
                while ((line = manifestReader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.StartsWith("calendar/standard/", StringComparison.OrdinalIgnoreCase))
                    {
                        // Convert the manifest path to just the filename for comparison.
                        var filename = Path.GetFileName(line);
                        calendarFiles.Add(filename);
                    }
                }
            }

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
                // Only load files that are in the manifest.
                var justFilename = Path.GetFileName(filename);
                if (!calendarFiles.Contains(justFilename))
                {
                    continue;
                }

                var deserializer = new DeserializerBuilder().Build();
                using var reader = new StreamReader(filename);
                var dictionary = deserializer.Deserialize<Dictionary<object, object>>(reader);
                var schema = new CalendarSchema(dictionary);
                s_CalendarsByTag.Add(schema.StandardTag, schema);
            }
        }

        public static void AddOldCalendar(Dictionary<object, object> dictionary)
        {
            var schema = new CalendarSchema(dictionary);
            s_CalendarsByTag.Add(schema.StandardTag, schema);
        }

        public static bool IsValidCalendarTag(string tag) => s_CalendarsByTag.ContainsKey(tag);

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
