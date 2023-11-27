// Copyright (c) Armidale Software
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.IO;
using System.Yaml.Serialization;

namespace Gedcom7
{
    public class EnumerationSet
    {
        public string Uri { get; private set; }
        public override string ToString() => this.Uri;
        public List<string> ValueUris { get; private set; }
        public List<string> ValueTags { get; private set; }
        EnumerationSet(Dictionary<object, object> dictionary)
        {
            this.Uri = dictionary["uri"] as string;
            this.ValueUris = new List<string>();
            GedcomStructureSchema.AddStrings(this.ValueUris, dictionary["enumeration values"] as Object[]);

            this.ValueTags = new List<string>();
            foreach (var uri in this.ValueUris)
            {
                // First try an enumeration value.
                EnumerationValue value = EnumerationValue.GetEnumerationValue(uri);
                if (value != null)
                {
                    this.ValueTags.Add(value.StandardTag);
                    continue;
                }

                // Now try a structure URI.
                GedcomStructureSchema schema = GedcomStructureSchema.GetSchema(uri);
                this.ValueTags.Add(schema.StandardTag);
            }
        }

        static Dictionary<string, EnumerationSet> s_EnumerationSets = new Dictionary<string, EnumerationSet>();

        public static void LoadAll(string gedcomRegistriesPath)
        {
            if (s_EnumerationSets.Count > 0)
            {
                return;
            }
            EnumerationValue.LoadAll(gedcomRegistriesPath);
            var path = Path.Combine(gedcomRegistriesPath, "enumeration-set/standard");
            string[] files = Directory.GetFiles(path);
            foreach (string filename in files)
            {
                var serializer = new YamlSerializer();
                object[] myObject = serializer.DeserializeFromFile(filename);
                var dictionary = myObject[0] as Dictionary<object, object>;
                var schema = new EnumerationSet(dictionary);
                s_EnumerationSets.Add(schema.Uri, schema);
            }
        }

        public static EnumerationSet GetEnumerationSet(string uri) => s_EnumerationSets[uri];

        public bool IsValidValue(string value)
        {
            return this.ValueTags.Contains(value);
        }
    }
}
