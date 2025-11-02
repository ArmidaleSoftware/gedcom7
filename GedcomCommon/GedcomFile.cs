// Copyright (c) Armidale Software
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace GedcomCommon
{
    public enum GedcomVersion
    {
        Unknown = 0,
        V551,
        V70,
        V71,
        All // Only used in tests, never for an actual file.
    }
    public class GedcomFile
    {
        public GedcomFile()
        {
            this.GedcomVersion = GedcomVersion.Unknown;
        }

        // Data members.
        Dictionary<GedcomStructure, GedcomStructureMatchInfo> StructureMatchDictionary = new Dictionary<GedcomStructure, GedcomStructureMatchInfo>();
        public string Path { get; private set; }
        public int LineCount { get; private set; }
        public List<GedcomStructure> GetRecordsAsList() => Records.Values.ToList();
        public Dictionary<string, GedcomStructure> Records = new Dictionary<string, GedcomStructure>();
        public GedcomStructure Head { get; set; }
        public GedcomStructure Trlr { get; set; }
        public override string ToString() { return this.Path; }
        public GedcomStructure SourceProduct => Head?.FindFirstSubstructure("SOUR");
        public string SourceProductVersion => SourceProduct?.FindFirstSubstructure("VERS")?.LineVal;
        public string Date => Head?.FindFirstSubstructure("DATE")?.LineVal;
        public GedcomVersion GedcomVersion { get; set; }

        /// <summary>
        /// Program and version that generated this GEDCOM file.
        /// </summary>
        public string SourceProgram
        {
            get
            {
                GedcomStructure sourceProgram = this.SourceProduct;
                if (sourceProgram == null)
                {
                    return null;
                }
                return sourceProgram.LineVal + " " + sourceProgram.FindFirstSubstructure("VERS")?.LineVal;
            }
        }

        /// <summary>
        /// Find the record with a given xref.
        /// </summary>
        /// <param name="xref">Xref of record to find</param>
        /// <returns>Record, or null if not found</returns>
        public GedcomStructure FindRecord(string xref)
        {
            if (!this.Records.ContainsKey(xref))
            {
                return null;
            }
            return this.Records[xref];
        }

        /// <summary>
        /// Load a GEDCOM file from a stream.
        /// </summary>
        /// <param name="reader">The stream to read from</param>
        /// <param name="gedcomVersion">GEDCOM version to read, if known</param>
        /// <param name="gedcomRegistriesPath">GEDCOM registries path, or null</param>
        /// <returns>List of 0 or more error messages</returns>
        public List<string> LoadFromStreamReader(StreamReader reader, GedcomVersion gedcomVersion = GedcomVersion.Unknown, string gedcomRegistriesPath = null)
        {
            string line;

            if (gedcomVersion != GedcomVersion.Unknown)
            {
                this.GedcomVersion = gedcomVersion;
            }
            else
            {
                // Do GEDCOM version detection.
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains("1 GEDC"))
                    {
                        break;
                    }
                }
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains("2 VERS"))
                    {
                        if (line.Contains("7.0"))
                        {
                            this.GedcomVersion = GedcomVersion.V70;
                        }
                        else if (line.Contains("7.1"))
                        {
                            this.GedcomVersion = GedcomVersion.V71;
                        }
                        else if (line.Contains("5.5.1"))
                        {
                            this.GedcomVersion = GedcomVersion.V551;
                        }
                        break;
                    }
                }

                // Reset the stream to the beginning.
                reader.BaseStream.Position = 0;
                reader.DiscardBufferedData();
            }

            GedcomStructureSchema.LoadAll(this.GedcomVersion, gedcomRegistriesPath);

            // Consume the BOM if any.
            if (reader.Peek() == 65279)
            {
                reader.Read();
            }

            // Now read the contents.
            var errors = new List<string>();
            var structurePath = new List<GedcomStructure>();
            this.LineCount = 0;
            while ((line = reader.ReadLine()) != null)
            {
                this.LineCount++;
                var s = new GedcomStructure();
                string error = s.Parse(this, this.LineCount, line, structurePath);
                if (error != null)
                {
                    errors.Add(error);
                }
            }
            return errors;
        }

        /// <summary>
        /// Load a GEDCOM file from a specified path.
        /// </summary>
        /// <param name="pathToFile">Path to file to load</param>
        /// <param name="gedcomRegistriesPath">GEDCOM registries path</param>
        /// <returns>List of 0 or more error messages</returns>
        public List<string> LoadFromPath(string pathToFile, string gedcomRegistriesPath = null)
        {
            // Validate that extension is .ged.
            if (!pathToFile.EndsWith(".ged", StringComparison.InvariantCultureIgnoreCase))
            {
                return new List<string>() { pathToFile + " must have a .ged extension" };
            }

            this.Path = pathToFile;
            if (!File.Exists(pathToFile))
            {
                return new List<string>() { "File not found: " + pathToFile };
            }
            using (var reader = new StreamReader(pathToFile))
            {
                return LoadFromStreamReader(reader, GedcomVersion.Unknown, gedcomRegistriesPath);
            }
        }

        /// <summary>
        /// Load a GEDCOM file from a specified URL.
        /// </summary>
        /// <param name="url">URL to file to load</param>
        /// <param name="gedcomRegistriesPath">GEDCOM registries path</param>
        /// <returns>List of 0 or more error messages</returns>
        public List<string> LoadFromUrl(string url, string gedcomRegistriesPath = null)
        {
            this.Path = url;

            var net = new System.Net.WebClient();
            var data = net.DownloadData(url);
            using (var content = new System.IO.MemoryStream(data))
            {
                using (var reader = new StreamReader(content))
                {
                    return LoadFromStreamReader(reader, GedcomVersion.Unknown, gedcomRegistriesPath);
                }
            }
        }

        /// <summary>
        /// Load a set of string content into this GEDCOM file.
        /// </summary>
        /// <param name="stringContent">File content</param>
        /// <returns>List of 0 or more error messages</returns>
        public List<string> LoadFromString(string stringContent)
        {
            using (var content = new MemoryStream(Encoding.UTF8.GetBytes(stringContent ?? "")))
            {
                using (var reader = new StreamReader(content))
                {
                    return LoadFromStreamReader(reader);
                }
            }
        }

        /// <summary>
        /// Generate a comparison report after comparing two GEDCOM files.
        /// </summary>
        /// <param name="otherFile">File to compare against</param>
        /// <returns></returns>
        public GedcomComparisonReport Compare(GedcomFile otherFile)
        {
            // First do a pass trying to match structures in each file.
            foreach (var keyValuePair in this.Records)
            {
                FindAndSaveBestMatch(keyValuePair.Value, otherFile.GetRecordsAsList());
            }

            var report = new GedcomComparisonReport(this.LineCount);
            foreach (var keyValuePair in this.Records)
            {
                AppendNonMatchingStructures(keyValuePair.Value, report.StructuresRemoved);
            }
            foreach (var keyValuePair in otherFile.Records)
            {
                otherFile.AppendNonMatchingStructures(keyValuePair.Value, report.StructuresAdded);
            }
            this.ResetComparison();
            otherFile.ResetComparison();

            return report;
        }

        public void ResetComparison()
        {
            foreach (var keyValuePair in this.Records)
            {
                ClearMatch(keyValuePair.Value);
            }
        }

        public GedcomStructureMatchInfo GetMatchInfo(GedcomStructure structure)
        {
#if DEBUG
            GedcomFile file = structure.File;
            Debug.Assert(file == this);
#endif
            if (!this.StructureMatchDictionary.ContainsKey(structure))
            {
                this.StructureMatchDictionary[structure] = new GedcomStructureMatchInfo(structure);
            }
            return this.StructureMatchDictionary[structure];
        }

        /// <summary>
        /// Find the best match and remember it.
        /// </summary>
        /// <param name="current">GEDCOM structure to find a match for</param>
        /// <param name="others">List of possibilities to match against</param>
        public void FindAndSaveBestMatch(GedcomStructure current, List<GedcomStructure> others)
        {
            float score;
            GedcomStructure other = current.FindBestMatch(others, out score);
            if (score > 0)
            {
                SaveMatch(current, other, score);
            }
        }

        /// <summary>
        /// Remember that this structure matches another structure.
        /// </summary>
        /// <param name="current">Current structure</param>
        /// <param name="other">Matching structure</param>
        /// <param name="score">Score</param>
        public void SaveMatch(GedcomStructure current, GedcomStructure other, float score)
        {
            GedcomStructureMatchInfo currentMatchInfo = GetMatchInfo(current);
            GedcomFile otherFile = other.File;
            GedcomStructureMatchInfo otherMatchInfo = otherFile.GetMatchInfo(other);
            if (otherFile.GetIsMatchComplete(otherMatchInfo))
            {
                // We just found a better match for 'other' than
                // what it previously had stored.  Remove the other's
                // previous match(es).
                foreach (var otherMatchWeak in otherMatchInfo.MatchStructures)
                {
                    GedcomStructure otherMatch2;
                    bool ok = otherMatchWeak.TryGetTarget(out otherMatch2);
                    if (ok)
                    {
                        ClearMatch(otherMatch2);
                    }
                }
                ClearMatch(other);
            }

            currentMatchInfo.MatchStructures.Add(new WeakReference<GedcomStructure>(other));
            otherMatchInfo.MatchStructures.Add(new WeakReference<GedcomStructure>(current));
            currentMatchInfo.Score += score;
            otherMatchInfo.Score += score;

            if (current.Tag != other.Tag)
            {
                // Handle some special cases.
                if (current.IsNoteType)
                {
                    if (current.Tag == "SNOTE")
                    {
                        SaveSharedNoteVsNoteMatch(current, other);
                    }
                    else
                    {
                        otherFile.SaveSharedNoteVsNoteMatch(other, current);
                    }
                }
            }

            // Save substructure matches.
            foreach (GedcomStructure sub in current.Substructures)
            {
                GedcomStructureMatchInfo subMatchInfo = GetMatchInfo(sub);
                while (!GetIsMatchComplete(subMatchInfo))
                {
                    float subScore;
                    GedcomStructure otherSub = sub.FindBestMatch(other.Substructures, out subScore);
                    if (subScore <= 0)
                    {
                        break;
                    }
                    SaveMatch(sub, otherSub, subScore);
                }
            }
        }

        /// <summary>
        /// Forget that this structure matches another structure.
        /// </summary>
        /// <param name="current">Current structure</param>
        public void ClearMatch(GedcomStructure current)
        {
            this.StructureMatchDictionary.Remove(current);
            foreach (GedcomStructure sub in current.Substructures)
            {
                ClearMatch(sub);
            }
        }

        /// <summary>
        /// Get the LineVal payload that does not yet have any match.  This is used with
        /// name pieces where the payload may be split among multiple matching structures.
        /// </summary>
        public string GetUnmatchedSpacedLineVal(GedcomStructure current)
        {
            string spacedValue = current.SpacedLineVal;
            string remainder = spacedValue;
            GedcomStructureMatchInfo currentMatch = GetMatchInfo(current);
            foreach (WeakReference<GedcomStructure> matchReference in currentMatch.MatchStructures)
            {
                GedcomStructure match;
                if (matchReference.TryGetTarget(out match))
                {
                    string spacedMatchValue = match.SpacedLineVal;
                    if (spacedMatchValue.Contains(spacedValue))
                    {
                        // This structure's payload is a substring of the match payload.
                        return " ";
                    }
                    remainder = remainder.Replace(spacedMatchValue, " ");
                }
            }
            return remainder;
        }

        void SaveSharedNoteVsNoteMatch(GedcomStructure current, GedcomStructure note)
        {
            // Find the record that the shared note points to.
            GedcomStructure sharedNoteRecord = FindRecord(current.LineVal);
            GedcomStructureMatchInfo sharedNoteRecordMatch = GetMatchInfo(sharedNoteRecord);
            sharedNoteRecordMatch.MatchStructures.Add(new WeakReference<GedcomStructure>(note));

            // Save substructure matches.
            foreach (GedcomStructure sub in sharedNoteRecord.Substructures)
            {
                float score;
                GedcomStructure otherSub = sub.FindBestMatch(note.Substructures, out score);
                if (score > 0)
                {
                    SaveMatch(sub, otherSub, score);
                }
            }
        }

        /// <summary>
        /// Append non-matching structures to a given list.
        /// </summary>
        /// <param name="current">Current GEDCOM structure</param>
        /// <param name="list">List to update</param>
        public void AppendNonMatchingStructures(GedcomStructure current, List<GedcomStructure> list)
        {
            if (current.IsExemptFromMatching)
            {
                return;
            }
            GedcomStructureMatchInfo currentMatchInfo = GetMatchInfo(current);
            if (!GetIsMatchComplete(currentMatchInfo))
            {
                list.Add(current);
            }
            foreach (GedcomStructure substructure in current.Substructures)
            {
                AppendNonMatchingStructures(substructure, list);
            }
        }

        /// <summary>
        /// Check whether a given structure has been fully matched within the other file.
        /// </summary>
        public bool GetIsMatchComplete(GedcomStructureMatchInfo current)
        {
            if (current == null)
            {
                return false;
            }
            return (current.Structure.IsNamePieceType) ? (GetUnmatchedSpacedLineVal(current.Structure) == " ") : (current.MatchStructures.Count > 0);
        }

        /// <summary>
        /// Check whether this file is valid GEDCOM.
        /// </summary>
        /// <returns>List of 0 or more error messages</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();

            // The file must start with HEAD and end with TRLR.
            if (this.Trlr == null)
            {
                errors.Add("Missing TRLR record");
            }

            foreach (var keyValuePair in this.Records)
            {
                var record = keyValuePair.Value;
                var recordErrors = record.Validate();
                errors.AddRange(recordErrors);
            }

            return errors;
        }

        /// <summary>
        /// Get a list of URI references used by the GEDCOM file.
        /// </summary>
        /// <returns></returns>
        public List<string> GetReferencedFiles()
        {
            var referencedFiles = new List<string>();

            List<GedcomStructure> records = GetRecordsAsList();
            foreach (var record in records)
            {
                record.AddReferencedFiles(referencedFiles);
            }
            return referencedFiles;
        }
    }
}
