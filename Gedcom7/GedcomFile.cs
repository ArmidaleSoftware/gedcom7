// Copyright (c) Armidale Software
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Gedcom7
{
    public class GedcomFile
    {
        // Data members.
        Dictionary<GedcomStructure, GedcomStructureMatchInfo> StructureMatchDictionary = new Dictionary<GedcomStructure, GedcomStructureMatchInfo>();
        public string Path { get; private set; }
        public int LineCount { get; private set; }
        List<GedcomStructure> Records = new List<GedcomStructure>();
        public override string ToString() { return this.Path; }
        GedcomStructure Head => (this.Records.Count > 0) ? this.Records[0] : null;
        public string Version => Head?.FindFirstSubstructure("SOUR")?.FindFirstSubstructure("VERS")?.LineVal;
        public string Date => Head?.FindFirstSubstructure("DATE")?.LineVal;

        /// <summary>
        /// Program and version that generated this GEDCOM file.
        /// </summary>
        public string SourceProgram
        {
            get
            {
                GedcomStructure sourceProgram = this.Head?.FindFirstSubstructure("SOUR");
                if (sourceProgram == null)
                {
                    return null;
                }
                return sourceProgram.LineVal + " " + sourceProgram.FindFirstSubstructure("VERS")?.LineVal;
            }
        }

        /// <summary>
        /// Find the record with a given tag and xref.
        /// </summary>
        /// <param name="tag">Tag of record to find</param>
        /// <param name="xref">Xref of record to find</param>
        /// <returns>Record, or null if not found</returns>
        public GedcomStructure FindRecord(string tag, string xref)
        {
            foreach (GedcomStructure record in this.Records)
            {
                if (record.Tag == tag && record.Xref == xref)
                {
                    return record;
                }
            }
            return null;
        }

        private void LoadFromStreamReader(StreamReader reader)
        {
            var structurePath = new List<GedcomStructure>();
            string line;
            this.LineCount = 0;
            while ((line = reader.ReadLine()) != null)
            {
                this.LineCount++;
                var s = new GedcomStructure(this, this.LineCount, line, structurePath);
                if (s.Level == 0)
                {
                    Records.Add(s);
                }
            }
        }

        /// <summary>
        /// Load a GEDCOM file from a specified path.
        /// </summary>
        /// <param name="pathToFile">Path to file to load</param>
        /// <returns></returns>
        public bool LoadFromPath(string pathToFile)
        {
            this.Path = pathToFile;
            if (!File.Exists(pathToFile))
            {
                return false;
            }
            using (var reader = new StreamReader(pathToFile))
            {
                LoadFromStreamReader(reader);
            }
            return true;
        }

        /// <summary>
        /// Load a GEDCOM file from a specified URL.
        /// </summary>
        /// <param name="url">URL to file to load</param>
        /// <returns></returns>
        public bool LoadFromUrl(string url)
        {
            this.Path = url;

            var net = new System.Net.WebClient();
            var data = net.DownloadData(url);
            using (var content = new System.IO.MemoryStream(data))
            {
                using (var reader = new StreamReader(content))
                {
                    LoadFromStreamReader(reader);
                }
            }
            return true;
        }

        /// <summary>
        /// Generate a comparison report after comparing two GEDCOM files.
        /// </summary>
        /// <param name="otherFile">File to compare against</param>
        /// <returns></returns>
        public GedcomComparisonReport Compare(GedcomFile otherFile)
        {
            // First do a pass trying to match structures in each file.
            foreach (GedcomStructure structure in this.Records)
            {
                FindAndSaveBestMatch(structure, otherFile.Records);
            }

            var report = new GedcomComparisonReport(this.LineCount);
            foreach (GedcomStructure structure in this.Records)
            {
                AppendNonMatchingStructures(structure, report.StructuresRemoved);
            }
            foreach (GedcomStructure structure in otherFile.Records)
            {
                AppendNonMatchingStructures(structure, report.StructuresAdded);
            }
            this.ResetComparison();
            otherFile.ResetComparison();

            return report;
        }

        public void ResetComparison()
        {
            foreach (GedcomStructure structure in this.Records)
            {
                ClearMatch(structure);
            }
        }

        public GedcomStructureMatchInfo GetMatchInfo(GedcomStructure structure) => this.StructureMatchDictionary[structure];

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
            GedcomStructureMatchInfo currentMatch = new GedcomStructureMatchInfo(current);
            GedcomStructureMatchInfo otherMatch = GetMatchInfo(other);
            if (GetIsMatchComplete(otherMatch))
            {
                // We just found a better match for 'other' than
                // what it previously had stored.  Remove the other's
                // previous match(es).
                foreach (var otherMatchWeak in otherMatch.MatchStructures)
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

            currentMatch.MatchStructures.Add(new WeakReference<GedcomStructure>(other));
            otherMatch.MatchStructures.Add(new WeakReference<GedcomStructure>(current));
            currentMatch.Score += score;
            otherMatch.Score += score;

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
                        SaveSharedNoteVsNoteMatch(other, current);
                    }
                }
            }

            // Save substructure matches.
            foreach (GedcomStructure sub in current.Substructures)
            {
                GedcomStructureMatchInfo subMatch = GetMatchInfo(sub);
                while (!GetIsMatchComplete(subMatch))
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
            GedcomStructure sharedNoteRecord = FindRecord("SNOTE", current.LineVal);
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
        /// Check whether this structure has been fully matched within the other file.
        /// </summary>
        public bool GetIsMatchComplete(GedcomStructureMatchInfo current)
        {
            return (current.Structure.IsNamePieceType) ? (GetUnmatchedSpacedLineVal(current.Structure) == " ") : (current.MatchStructures.Count > 0);
        }
    }
}
