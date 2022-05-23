// Copyright (c) Armidale Software
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;

namespace Gedcom7
{
    public class GedcomStructure
    {
        public override string ToString() => this.OriginalLine;
        WeakReference<GedcomStructure> Superstructure { get; set; }
        WeakReference<GedcomFile> File { get; set; }
        public int LineNumber { get; private set; }
        public int Level { get; private set; }
        public string Xref { get; private set; }
        public string Tag { get; private set; }
        bool IsNoteType => this.Tag == "NOTE" || this.Tag == "SNOTE";
        public GedcomStructure FindFirstSubstructure(string tag) => this.Substructures.Find(x => x.Tag == tag);
        public string TagWithPath
        {
            get
            {
                string result = "";
                if (this.Superstructure != null)
                {
                    GedcomStructure superstructure;
                    if (this.Superstructure.TryGetTarget(out superstructure))
                    {
                        result += superstructure.TagWithPath + ".";
                    }
                }
                result += this.Tag;
                return result;
            }
        }
        public string LineVal { get; private set; }
        public string OriginalLine { get; private set; }
        public bool IsExemptFromMatching
        {
            get
            {
                // Two files are expected to have different HEAD.SOUR and HEAD.DEST structures.
                if (this.TagWithPath == "HEAD.SOUR" ||
                    this.TagWithPath == "HEAD.DEST" ||
                    this.TagWithPath == "HEAD.DATE")
                {
                    return true;
                }
                return false;
            }
        }
        public string LineWithPath
        {
            get
            {
                string result = "Line " + this.LineNumber.ToString() + ": " + this.Level.ToString();
                if (this.Xref != null)
                {
                    result += " " + this.Xref;
                }
                result += " " + this.TagWithPath;
                if (this.LineVal != null)
                {
                    result += " " + this.LineVal;
                }
                return result;
            }
        }
        List<GedcomStructure> Substructures { get; set; }
        WeakReference<GedcomStructure> MatchStructure { get; set; }

        public GedcomStructure(GedcomFile file, int lineNumber, string line, List<GedcomStructure> structurePath)
        {
            this.File = new WeakReference<GedcomFile>(file);
            this.LineNumber = lineNumber;
            this.OriginalLine = line;

            // Parse line into Level, Xref, Tag, Pointer, and LineVal.
            string[] tokens = line.Split(' ');
            if (line == null)
            {
                return;
            }
            int index = 0;
            this.Level = Convert.ToInt32(tokens[index++]);
            if (tokens.Length > index && tokens[index][0] == '@')
            {
                this.Xref = tokens[index++];
            }
            if (tokens.Length > index)
            {
                this.Tag = tokens[index++];
                if (tokens.Length > index)
                {
                    int offset = line.IndexOf(this.Tag);
                    this.LineVal = line.Substring(offset + this.Tag.Length + 1);
                }
            }
            this.Substructures = new List<GedcomStructure>();

            // Update path to current structure.
            structurePath.RemoveRange(this.Level, structurePath.Count - this.Level);
            structurePath.Add(this);

            if (this.Level > 0)
            {
                GedcomStructure superstructure = structurePath[this.Level - 1];
                this.Superstructure = new WeakReference<GedcomStructure>(superstructure);
                superstructure.Substructures.Add(this);
            }
        }

        /// <summary>
        /// Check whether a line value is a pointer.
        /// </summary>
        /// <param name="lineval">Line value to check</param>
        /// <returns>true if a pointer, false if not</returns>
        static bool IsPointer(string lineval)
        {
            return (lineval != null && lineval.Length > 2 && lineval[0] == '@' && lineval[1] != '@');
        }

        /// <summary>
        /// Compare two LineStr values.
        /// </summary>
        /// <param name="a">First value to compare</param>
        /// <param name="b">Second value to compare</param>
        /// <returns>positive if similar, negative if dissimilar</returns>
        static float ScoreLineStr(string a, string b)
        {
            if (a != b)
            {
                // TODO: use a more intelligent scoring algorithm.
                return -1;
            }
            return 1;
        }

        float ScoreSubstructures(List<GedcomStructure> others)
        {
            float cumulativeScore = 0;
            foreach (GedcomStructure structure in this.Substructures)
            {
                float score;
                structure.FindBestMatch(others, out score);
                cumulativeScore += score;
            }
            return cumulativeScore;
        }

        /// <summary>
        /// Score an SNOTE structure against a NOTE structure.  This allows for the
        /// case where one has been converted to the other without loss of information.
        /// </summary>
        /// <param name="sharedNote">NOTE structure</param>
        /// <returns>negative if dissimilar, positive if similar</returns>
        float ScoreSharedNoteVsNote(GedcomStructure note)
        {
            // Find the record that the shared note points to.
            GedcomFile file;
            if (!this.File.TryGetTarget(out file))
            {
                return 0;
            }
            GedcomStructure sharedNoteRecord = file.FindRecord("SNOTE", this.LineVal);
            if (sharedNoteRecord == null)
            {
                return 0;
            }

            float cumulativeScore = ScoreLineStr(sharedNoteRecord.LineVal, note.LineVal);
            cumulativeScore += sharedNoteRecord.ScoreSubstructures(note.Substructures);
            return cumulativeScore;
        }

        /// <summary>
        /// Compute a score of how closely another structure matches this one.
        /// </summary>
        /// <param name="other"></param>
        /// <returns>0 if not comparable, negative if dissimilar, positive if similar</returns>
        float ScoreMatch(GedcomStructure other)
        {
            if (this.Tag != other.Tag)
            {
                if (IsNoteType && other.IsNoteType)
                {
                    return (this.Tag == "SNOTE") ? ScoreSharedNoteVsNote(other) : other.ScoreSharedNoteVsNote(this);
                }
                return 0;
            }

            // Score line values.
            if (IsPointer(this.LineVal) != IsPointer(other.LineVal))
            {
                return float.MinValue;
            }
            float cumulativeScore = (IsPointer(this.LineVal)) ? 1 : ScoreLineStr(this.LineVal, other.LineVal);

            // Score substructures.
            cumulativeScore += this.ScoreSubstructures(other.Substructures);

            return cumulativeScore;
        }

        void SaveSharedNoteVsNoteMatch(GedcomStructure note)
        {
            // Find the record that the shared note points to.
            GedcomFile file;
            this.File.TryGetTarget(out file);
            GedcomStructure sharedNoteRecord = file.FindRecord("SNOTE", this.LineVal);
            sharedNoteRecord.MatchStructure = new WeakReference<GedcomStructure>(note);

            // Save substructure matches.
            foreach (GedcomStructure sub in sharedNoteRecord.Substructures)
            {
                float score;
                GedcomStructure otherSub = sub.FindBestMatch(note.Substructures, out score);
                if (score > 0)
                {
                    sub.SaveMatch(otherSub);
                }
            }
        }

        /// <summary>
        /// Remember that this structure matches another structure.
        /// </summary>
        /// <param name="other">Matching structure</param>
        void SaveMatch(GedcomStructure other)
        {
            this.MatchStructure = new WeakReference<GedcomStructure>(other);
            other.MatchStructure = new WeakReference<GedcomStructure>(this);

            if (this.Tag != other.Tag)
            {
                // Handle some special cases.
                if (this.IsNoteType) {
                    if (this.Tag == "SNOTE")
                    {
                        this.SaveSharedNoteVsNoteMatch(other);
                    }
                    else
                    {
                        other.SaveSharedNoteVsNoteMatch(this);
                    }
                }
            }

            // Save substructure matches.
            foreach (GedcomStructure sub in this.Substructures)
            {
                float subScore;
                GedcomStructure otherSub = sub.FindBestMatch(other.Substructures, out subScore);
                if (subScore > 0)
                {
                    sub.SaveMatch(otherSub);
                }
            }
        }

        /// <summary>
        /// Forget that this structure matches another structure.
        /// </summary>
        public void ClearMatch()
        {
            this.MatchStructure = null;
            foreach (GedcomStructure sub in this.Substructures)
            {
                sub.ClearMatch();
            }
        }

        /// <summary>
        /// Find the best match in a given list of possibilities.
        /// </summary>
        /// <param name="others">List of possibilities to look for a match in</param>
        /// <param name="returnScore">Score of best match</param>
        /// <returns>Best match</returns>
        public GedcomStructure FindBestMatch(List<GedcomStructure> others, out float returnScore)
        {
            float bestScore = 0;
            GedcomStructure bestOther = null;
            foreach (GedcomStructure other in others)
            {
                if (other.MatchStructure != null)
                {
                    // Already matched something else.
                    continue;
                }
                float score = ScoreMatch(other);
                if (bestOther == null || bestScore < score)
                {
                    bestScore = score;
                    bestOther = other;
                }
            }
            returnScore = bestScore;
            return bestOther;
        }

        /// <summary>
        /// Find the best match and remember it.
        /// </summary>
        /// <param name="others">List of possibilities to match against</param>
        public void FindAndSaveBestMatch(List<GedcomStructure> others)
        {
            float score;
            GedcomStructure other = FindBestMatch(others, out score);
            if (score > 0)
            {
                SaveMatch(other);
            }
        }

        /// <summary>
        /// Append non-matching structures to a given list.
        /// </summary>
        /// <param name="list">List to update</param>
        public void AppendNonMatchingStructures(List<GedcomStructure> list)
        {
            if (this.IsExemptFromMatching)
            {
                return;
            }
            if (this.MatchStructure == null)
            {
                list.Add(this);
            }
            foreach (GedcomStructure substructure in this.Substructures)
            {
                substructure.AppendNonMatchingStructures(list);
            }
        }
    }
}
