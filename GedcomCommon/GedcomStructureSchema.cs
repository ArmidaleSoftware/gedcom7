// Copyright (c) Armidale Software
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GedcomCommon
{
    public struct GedcomStructureCountInfo
    {
        public bool Required; // True: Minimum = 1, False: Minimum = 0.
        public int Maximum; // 1, 3, int.MaxValue
        public override string ToString()
        {
            return "{" + (Required ? "1" : "0") + ":" + (Maximum == int.MaxValue ? "M" : Maximum.ToString()) + "}";
        }
    }
    public struct GedcomStructureSchemaKey
    {
        public GedcomVersion GedcomVersion;
        public string SourceProgram; // null (wildcard) for standard tags.
        public string SuperstructureUri; // null (wildcard) for undocumented extensions, "-" for records.
        public string Tag;

        // GEDCOM 5.5.1 can have two URIs per superstructure+tag pair, one for a pointer and a non-pointer.
        public bool IsPointer;
        public override string ToString()
        {
            return SourceProgram + "|" + SuperstructureUri + "|" + Tag;
        }
    }
    public class GedcomStructureSchema
    {
        public static void AddStrings(List<string> list, List<object> array)
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
                    if (value == "{0:1}")
                    {
                        info.Required = false;
                        info.Maximum = 1;
                    }
                    else if (value == "{1:1}")
                    {
                        info.Required = true;
                        info.Maximum = 1;
                    }
                    else if (value == "{0:M}")
                    {
                        info.Required = false;
                        info.Maximum = int.MaxValue;
                    }
                    else if (value == "{1:M}")
                    {
                        info.Required = true;
                        info.Maximum = int.MaxValue;
                    }
                    else if (value == "{0:3}")
                    {
                        info.Required = false;
                        info.Maximum = 3;
                    }
                    else
                    {
                        throw new Exception();
                    }
                    dictionary[key as string] = info;
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

        /// <summary>
        /// Check whether this schema is a standard schema (even if relocated).
        /// </summary>
        public bool IsStandard => (this.StandardTag[0] != '_');
        public bool IsDocumented => (this.Uri != null);

        public override string ToString()
        {
            return this.StandardTag;
        }

        public static bool UriHasVersion(string uri, GedcomVersion version)
        {
            Debug.Assert(version != GedcomVersion.Unknown);
            if (uri.Contains("/v5.5.1/"))
            {
                return (version == GedcomVersion.V551 || version == GedcomVersion.All);
            }
            if (uri.Contains("/v7.1/"))
            {
                return (version == GedcomVersion.V71 || version == GedcomVersion.All);
            }

            // V7 URIs are valid for both V7.0 and V7.1.
            // TODO: ignore V7 URIs if subsumed by a V7.1 URI.
            return (version == GedcomVersion.V70 || version == GedcomVersion.V71 || version == GedcomVersion.All);
        }

        /// <summary>
        /// Check whether this schema applies to a given GEDCOM version.
        /// </summary>
        /// <param name="version">GEDCOM version</param>
        /// <returns>true if applies, false if not</returns>
        public bool HasVersion(GedcomVersion version)
        {
            Debug.Assert(version != GedcomVersion.Unknown);
            if (IsDocumented && !UriHasVersion(this.Uri, version))
            {
                return false;
            }
            foreach (string uri in Substructures.Keys)
            {
                if (!UriHasVersion(uri, version))
                {
                    return false;
                }
            }
            return true;
        }

        GedcomStructureSchema(Dictionary<object, object> dictionary)
        {
            this.Lang = dictionary["lang"] as string;
            this.Type = dictionary["type"] as string;
            this.Uri = dictionary["uri"] as string;
            this.StandardTag = dictionary["standard tag"] as string;
            this.Label = dictionary["label"] as string;
            this.Payload = dictionary["payload"] as string;
            if (dictionary.ContainsKey("enumeration set"))
            {
                this.EnumerationSetUri = dictionary["enumeration set"] as string;
            }
            this.Specification = new List<string>();
            AddStrings(this.Specification, dictionary["specification"] as List<Object>);
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
        public string EnumerationSetUri { get; private set; }
        public EnumerationSet EnumerationSet => EnumerationSet.GetEnumerationSet(EnumerationSetUri);
        public bool HasPointer => (this.Payload != null) && this.Payload.StartsWith("@<") && this.Payload.EndsWith(">@");
        public Dictionary<string, GedcomStructureCountInfo> Substructures { get; private set; }
        public Dictionary<string, GedcomStructureCountInfo> Superstructures { get; private set; }

        static Dictionary<GedcomStructureSchemaKey, GedcomStructureSchema> s_StructureSchemas551 = new Dictionary<GedcomStructureSchemaKey, GedcomStructureSchema>();
        static Dictionary<GedcomStructureSchemaKey, GedcomStructureSchema> s_StructureSchemas70 = new Dictionary<GedcomStructureSchemaKey, GedcomStructureSchema>();
        static Dictionary<GedcomStructureSchemaKey, GedcomStructureSchema> s_StructureSchemas71 = new Dictionary<GedcomStructureSchemaKey, GedcomStructureSchema>();
        static Dictionary<GedcomStructureSchemaKey, GedcomStructureSchema>[] s_StructureSchemasByVersion = new Dictionary<GedcomStructureSchemaKey, GedcomStructureSchema>[]
        {
            null,
            s_StructureSchemas551,
            s_StructureSchemas70,
            s_StructureSchemas71
        };
        static Dictionary<string, string> s_StructureSchemaAliases = new System.Collections.Generic.Dictionary<string, string>();
        static Dictionary<string, GedcomStructureSchema> s_StructureSchemasByUri = new Dictionary<string, GedcomStructureSchema>();

        public const string RecordSuperstructureUri = "TOP";

        /// <summary>
        /// Add a schema.
        /// </summary>
        /// <param name="version">GEDCOM version</param>
        /// <param name="sourceProgram">null (wildcard) for standard tags, else extension</param>
        /// <param name="superstructureUri">null (wildcard) for undocumented tags, RecordSuperstructureUri for records, else URI of superstructure schema</param>
        /// <param name="tag">Tag</param>
        /// <param name="schema">Schema</param>
        public static void AddSchema(GedcomVersion version, string sourceProgram, string superstructureUri, string tag, GedcomStructureSchema schema)
        {
            Debug.Assert(sourceProgram != string.Empty); // Use wildcard for all.
            GedcomStructureSchemaKey structureSchemaKey = new GedcomStructureSchemaKey();
            structureSchemaKey.GedcomVersion = version;
            structureSchemaKey.SourceProgram = sourceProgram;
            structureSchemaKey.SuperstructureUri = superstructureUri;
            structureSchemaKey.Tag = tag;
            structureSchemaKey.IsPointer = (version == GedcomVersion.V551) && (schema.Payload != null) && schema.Payload.StartsWith('@');

            var structureSchemas = s_StructureSchemasByVersion[(int)version];
            Debug.Assert(!structureSchemas.ContainsKey(structureSchemaKey), $"No structure schema for {sourceProgram} {superstructureUri} {tag} {structureSchemaKey.IsPointer}");
            structureSchemas[structureSchemaKey] = schema;
        }

        /// <summary>
        /// Add a schema.
        /// </summary>
        /// <param name="version">GEDCOM version</param>
        /// <param name="sourceProgram">null (wildcard) for standard tags, else extension</param>
        /// <param name="tag">Tag</param>
        /// <param name="uri">Structure URI</param>
        public static void AddSchema(GedcomVersion version, string sourceProgram, string tag, string uri)
        {
            var structureSchemaKey = new GedcomStructureSchemaKey();
            structureSchemaKey.GedcomVersion = version;
            structureSchemaKey.SourceProgram = sourceProgram;
            // Leave SuperstructureUri as null for a wildcard.
            structureSchemaKey.Tag = tag;
            structureSchemaKey.IsPointer = false;

            // The spec says:
            //    "The schema structure may contain the same tag more than once with different URIs.
            //    Reusing tags in this way must not be done unless the concepts identified by those
            //    URIs cannot appear in the same place in a dataset..."
            // But for now we just overwrite it in the index for SCHMA defined schemas.

            if (s_StructureSchemasByUri.ContainsKey(uri))
            {
                // This is an alias.
                s_StructureSchemaAliases[tag] = uri;
                return;
            }

            var schema = new GedcomStructureSchema(sourceProgram, tag);
            schema.Uri = uri;
            s_StructureSchemasByVersion[(int)version][structureSchemaKey] = schema;
        }

        public static void LoadAll(GedcomVersion version, string gedcomRegistriesPath = null)
        {
            Debug.Assert(version != GedcomVersion.Unknown && version != GedcomVersion.All);
            if (s_StructureSchemasByVersion[(int)version].Count > 0)
            {
                // Already loaded.
                return;
            }
            if (gedcomRegistriesPath == null)
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                gedcomRegistriesPath = Path.Combine(baseDirectory, "..", "..", "..", "..", "..", "gedcom7", "external", "GEDCOM-registries");
            }
            var path = Path.Combine(gedcomRegistriesPath, "structure", "standard");
            string[] files = Directory.GetFiles(path);
            foreach (string filename in files)
            {
                var deserializer = new DeserializerBuilder().Build();
                using var reader = new StreamReader(filename);
                var dictionary = deserializer.Deserialize<Dictionary<object, object>>(reader);
                var schema = new GedcomStructureSchema(dictionary);
                if (!schema.HasVersion(version))
                {
                    continue;
                }
                s_StructureSchemasByUri[schema.Uri] = schema;
                if (schema.Superstructures.Count == 0)
                {
                    AddSchema(version, null, RecordSuperstructureUri, schema.StandardTag, schema);
                }
                else
                {
                    foreach (var superstructureUri in schema.Superstructures.Keys)
                    {
                        AddSchema(version, null, superstructureUri, schema.StandardTag, schema);
                    }
                }
            }
            EnumerationSet.LoadAll(version, gedcomRegistriesPath);
            CalendarSchema.LoadAll(version, gedcomRegistriesPath);
        }

        public static GedcomStructureSchema GetSchema(string uri) => s_StructureSchemasByUri.ContainsKey(uri) ? s_StructureSchemasByUri[uri] : null;

        /// <summary>
        /// Get a GEDCOM structure schema.
        /// </summary>
        /// <param name="version">GEDCOM version</param>
        /// <param name="sourceProgram">source program string, or null for wildcard</param>
        /// <param name="superstructureUri">superstructure URI, or null for wildcard</param>
        /// <param name="tag">GEDCOM tag</param>
        /// <param name="isPointer">True if payload is a pointer</param>
        /// <returns>Schema object</returns>
        public static GedcomStructureSchema GetSchema(GedcomVersion version, string sourceProgram, string superstructureUri, string tag, bool isPointer)
        {
            // First look for a schema with a wildcard source program.
            GedcomStructureSchemaKey structureSchemaKey = new GedcomStructureSchemaKey();
            structureSchemaKey.GedcomVersion = version;
            structureSchemaKey.SuperstructureUri = superstructureUri;
            structureSchemaKey.Tag = tag;
            structureSchemaKey.IsPointer = (version == GedcomVersion.V551) ? isPointer : false;

            var structureSchemas = s_StructureSchemasByVersion[(int)version];
            if (structureSchemas.ContainsKey(structureSchemaKey))
            {
                return structureSchemas[structureSchemaKey];
            }

            // Now look for a schema specific to the source program
            // and superstructure URI, which would be a documented
            // extension tag.
            if (sourceProgram == null)
            {
                sourceProgram = "Unknown";
            }
            structureSchemaKey.SourceProgram = sourceProgram;
            if (structureSchemas.ContainsKey(structureSchemaKey))
            {
                return structureSchemas[structureSchemaKey];
            }

            // Now look for a schema specific to the source program
            // and wildcard superstructure URI, which would be an
            // undocumented extension tag.
            structureSchemaKey.SuperstructureUri = null;
            if (structureSchemas.ContainsKey(structureSchemaKey))
            {
                return structureSchemas[structureSchemaKey];
            }

            // Now look for a schema alias defined in HEAD.SCHMA.
            GedcomStructureSchema schema;
            if (s_StructureSchemaAliases.ContainsKey(tag))
            {
                string uri = s_StructureSchemaAliases[tag];
                if (s_StructureSchemasByUri.TryGetValue(uri, out schema))
                {
                    return schema;
                }
            }

            // Create a new schema for it.
            structureSchemaKey.SuperstructureUri = superstructureUri;
            schema = new GedcomStructureSchema(sourceProgram, tag);
            structureSchemas[structureSchemaKey] = schema;
            return structureSchemas[structureSchemaKey];
        }
    }
}
