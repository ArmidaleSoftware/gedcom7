// Copyright (c) Armidale Software
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.IO;
using System.Yaml.Serialization;

namespace Gedcom7
{
    public class MonthSchema
    {
        public string Uri { get; private set; }
        public override string ToString() => this.Uri;
        public string StandardTag { get; private set; }
        public string Label { get; private set; }
        public List<string> Specification { get; private set; }
        public List<string> Calendars { get; private set; }
        MonthSchema(Dictionary<object, object> dictionary)
        {
            this.Uri = dictionary["uri"] as string;
            this.Label = dictionary["label"] as string;
            this.StandardTag = dictionary["standard tag"] as string;
            this.Specification = new List<string>();
            GedcomStructureSchema.AddStrings(this.Specification, dictionary["specification"] as Object[]);
            this.Calendars = new List<string>();
            GedcomStructureSchema.AddStrings(this.Calendars, dictionary["calendars"] as Object[]);
        }

        static Dictionary<string, MonthSchema> s_Months = new Dictionary<string, MonthSchema>();

        public static void LoadAll(string gedcomRegistriesPath)
        {
            if (s_Months.Count > 0)
            {
                return;
            }
            var path = Path.Combine(gedcomRegistriesPath, "month/standard");
            string[] files = Directory.GetFiles(path);
            foreach (string filename in files)
            {
                var serializer = new YamlSerializer();
                object[] myObject = serializer.DeserializeFromFile(filename);
                var dictionary = myObject[0] as Dictionary<object, object>;
                var schema = new MonthSchema(dictionary);
                s_Months.Add(schema.Uri, schema);
            }
        }

        public static MonthSchema GetMonth(string uri) => s_Months[uri];
    }
}
