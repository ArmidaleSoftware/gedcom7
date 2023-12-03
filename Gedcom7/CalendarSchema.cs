// Copyright (c) Armidale Software
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.IO;
using System.Yaml.Serialization;

namespace Gedcom7
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
            GedcomStructureSchema.AddStrings(this.Epochs, dictionary["epochs"] as Object[]);
            this.MonthUris = new List<string>();
            GedcomStructureSchema.AddStrings(this.MonthUris, dictionary["months"] as Object[]);

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

        public static void LoadAll(string gedcomRegistriesPath)
        {
            if (s_CalendarsByTag.Count > 0)
            {
                return;
            }
            MonthSchema.LoadAll(gedcomRegistriesPath);
            var path = Path.Combine(gedcomRegistriesPath, "calendar/standard");
            string[] files = Directory.GetFiles(path);
            foreach (string filename in files)
            {
                var serializer = new YamlSerializer();
                object[] myObject = serializer.DeserializeFromFile(filename);
                var dictionary = myObject[0] as Dictionary<object, object>;
                var schema = new CalendarSchema(dictionary);
                s_CalendarsByTag.Add(schema.StandardTag, schema);
            }
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
