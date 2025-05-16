// Copyright (c) Armidale Software
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.IO;
using System.Yaml.Serialization;

namespace GedcomCommon
{
    public class EnumerationValue
    {
        public string Uri { get; private set; }
        public override string ToString() => this.Uri;
        public string StandardTag { get; private set; }
        public List<string> Specification { get; private set; }
        public List<string> ValueOf { get; private set; }

        EnumerationValue(Dictionary<object, object> dictionary)
        {
            this.Uri = dictionary["uri"] as string;
            this.StandardTag = dictionary["standard tag"] as string;
            this.Specification = new List<string>();
            GedcomStructureSchema.AddStrings(this.Specification, dictionary["specification"] as Object[]);
            this.ValueOf = new List<string>();
            GedcomStructureSchema.AddStrings(this.ValueOf, dictionary["value of"] as Object[]);
        }

        static Dictionary<string, EnumerationValue> s_EnumerationValues = new Dictionary<string, EnumerationValue>();

        public static void LoadAll(string gedcomRegistriesPath)
        {
            if (s_EnumerationValues.Count > 0)
            {
                return;
            }
            string path = Path.Combine(gedcomRegistriesPath, "enumeration/standard");
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
                var serializer = new YamlSerializer();
                object[] myObject = serializer.DeserializeFromFile(filename);
                var dictionary = myObject[0] as Dictionary<object, object>;
                var schema = new EnumerationValue(dictionary);
                s_EnumerationValues.Add(schema.Uri, schema);
            }
        }

        public static EnumerationValue GetEnumerationValue(string uri) => s_EnumerationValues.ContainsKey(uri) ? s_EnumerationValues[uri] : null;
    }
}
