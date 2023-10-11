// Copyright (c) Armidale Software
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Yaml.Serialization;

namespace Gedcom7
{
    public struct GedcomStructureCountInfo
    {
        public bool Required; // True: Minimum = 1, False: Minimum = 0.
        public bool Singleton; // True: Maximum = 1, False: Maximum = M.
    };

    public class GedcomStructureSchema
    {
        static void AddStrings(List<string> list, Object[] array)
        {
            if (array != null)
            {
                foreach (var value in array)
                {
                    list.Add(value as string);
                }
            }
        }
        static void AddDictionary(Dictionary<string, GedcomStructureCountInfo> dictionary, Dictionary<object, object> input)
        {
            if (input != null)
            {
                foreach (var key in input.Keys)
                {
                    var value = input[key] as string;
                    var info = new GedcomStructureCountInfo();      
                    dictionary[key as string] = info;
                    if (value == "{0:1}")
                    {
                        info.Required = false;
                        info.Singleton = true;
                    } else if (value == "{1:1}")
                    {
                        info.Required = true;
                        info.Singleton = true;
                    } else if (value == "{0:M}")
                    {
                        info.Required = false;
                        info.Singleton = false;
                    } else if (value == "{1:M}")
                    {
                        info.Required = true;
                        info.Singleton = false;
                    } else
                    {
                        throw new Exception();
                    }
                }
            }
        }

        GedcomStructureSchema(string sourceProgram, string tag)
        {
            this.StandardTag = tag;
            this.Specification = new List<string>();
            this.Substructures = new Dictionary<string, GedcomStructureCountInfo>();
            this.Superstructures = new Dictionary<string, GedcomStructureCountInfo>();
        }

        GedcomStructureSchema(Dictionary<object, object> dictionary)
        {
            this.Lang = dictionary["lang"] as string;
            this.Type = dictionary["type"] as string;
            this.Uri = dictionary["uri"] as string;
            this.StandardTag = dictionary["standard tag"] as string;
            this.Label = dictionary["label"] as string;
            this.Payload = dictionary["payload"] as string;
            this.Specification = new List<string>();
            AddStrings(this.Specification, dictionary["specification"] as Object[]);
            this.Substructures = new Dictionary<string, GedcomStructureCountInfo>();
            AddDictionary(this.Substructures, dictionary["substructures"] as Dictionary<object, object>);
            this.Superstructures = new Dictionary<string, GedcomStructureCountInfo>();
            AddDictionary(this.Superstructures, dictionary["superstructures"] as Dictionary<object, object>);
        }
        public string Lang { get; private set; }
        public string Type { get; private set; }
        public string Uri { get; private set; }
        public string StandardTag { get; private set; }
        public List<string> Specification { get; private set; }
        public string Label { get; private set; }
        public string Payload { get; private set; }
        public Dictionary<string, GedcomStructureCountInfo> Substructures { get; private set; }
        public Dictionary<string, GedcomStructureCountInfo> Superstructures { get; private set; }

        static Dictionary<string, GedcomStructureSchema> s_StructureSchemas = new Dictionary<string, GedcomStructureSchema>();
        public static void LoadAll()
        {
            if (s_StructureSchemas.Count > 0)
            {
                return;
            }
            string currentDirectory = Directory.GetCurrentDirectory();
            var path = Path.Combine(currentDirectory, "../../../../external/GEDCOM-registries/structure/standard");
            string[] files = Directory.GetFiles(path);
            foreach (string filename in files)
            {
                var serializer = new YamlSerializer();
                object[] myObject = serializer.DeserializeFromFile(filename);
                var dictionary = myObject[0] as Dictionary<object, object>;
                var schema = new GedcomStructureSchema(dictionary);
                s_StructureSchemas[schema.StandardTag] = schema;
            }
        }
        public static GedcomStructureSchema GetSchema(string sourceProduct, string tag)
        {
            if (!s_StructureSchemas.ContainsKey(tag)) {
                // This is an undocumented extension tag use.
                // Create a new schema for it.
                var schema = new GedcomStructureSchema(sourceProduct, tag);
                s_StructureSchemas[tag] = schema;
            }

            return s_StructureSchemas[tag];
        }
    }
}
