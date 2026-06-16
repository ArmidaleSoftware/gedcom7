// Copyright (c) Armidale Software
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using YamlDotNet.Serialization;

namespace GedcomCommon
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
            GedcomStructureSchema.AddStrings(this.ValueUris, dictionary["enumeration values"] as List<Object>);

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
                if (schema != null)
                {
                    this.ValueTags.Add(schema.StandardTag);
                }
            }
        }

        static Dictionary<string, EnumerationSet> s_EnumerationSets = new Dictionary<string, EnumerationSet>();

        public static void LoadAll(GedcomVersion version, string gedcomRegistriesPath)
        {
            if (s_EnumerationSets.Count > 0)
            {
                return;
            }
            EnumerationValue.LoadAll(gedcomRegistriesPath);

            // Read the manifest file to get the list of enumeration-set files for this version.
            var standardManifestPath = Path.Combine(gedcomRegistriesPath, "manifest", "standard", "manifest-" + GedcomStructureSchema.GetGedcomVersionString(version) + "-en-US.tsv");
            var enumSetFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(standardManifestPath))
            {
                using var manifestReader = new StreamReader(standardManifestPath);

                // Skip header line.
                manifestReader.ReadLine();
                string line;
                while ((line = manifestReader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.StartsWith("enumeration-set/standard/", StringComparison.OrdinalIgnoreCase))
                    {
                        // Convert the manifest path to just the filename for comparison.
                        var filename = Path.GetFileName(line);
                        enumSetFiles.Add(filename);
                    }
                }
            }

            var path = Path.Combine(gedcomRegistriesPath, "enumeration-set", "standard");
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
                if (!enumSetFiles.Contains(justFilename))
                {
                    continue;
                }

                var deserializer = new DeserializerBuilder().Build();
                using var reader = new StreamReader(filename);
                var dictionary = deserializer.Deserialize<Dictionary<object, object>>(reader);
                var schema = new EnumerationSet(dictionary);
                s_EnumerationSets.Add(schema.Uri, schema);
            }
        }

        public static EnumerationSet GetEnumerationSet(string uri) => s_EnumerationSets[uri];

        public bool IsValidValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }
            return this.ValueTags.Contains(value) || value.StartsWith('_');
        }
    }
}
