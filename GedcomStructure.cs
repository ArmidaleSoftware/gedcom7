// Copyright (c) Armidale Software
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;

namespace Gedcom7
{
    public class GedcomStructure
    {
        public override string ToString() { return this.OriginalLine; }
        WeakReference<GedcomStructure> Superstructure { get; set; }
        int LineNumber { get; set; }
        public int Level { get; private set; }
        string Xref { get; set; }
        string Tag { get; set; }
        string TagWithPath
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
        string LineVal { get; set; }
        string OriginalLine { get; set; }
        public bool IsExemptFromMatching
        {
            get
            {
                // Two files are expected to have different HEAD.SOUR structures.
                if (this.TagWithPath == "HEAD.SOUR")
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

        public GedcomStructure(int lineNumber, string line, List<GedcomStructure> structurePath)
        {
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
        /// Compute a score of how closely another structure matches this one.
        /// </summary>
        /// <param name="other"></param>
        /// <returns>0 if not comparable, negative if dissimilar, positive if similar</returns>
        float ScoreMatch(GedcomStructure other)
        {
            if (this.Tag != other.Tag)
            {
                return 0;
            }

            // Score line values.
            if (IsPointer(this.LineVal) != IsPointer(other.LineVal))
            {
                return float.MinValue;
            }
            if (!IsPointer(this.LineVal) && this.LineVal != other.LineVal)
            {
                // TODO: use a more intelligent scoring algorithm.
                return float.MinValue;
            }

            // Score substructures.
            float cumulativeScore = 1;
            foreach (GedcomStructure structure in this.Substructures)
            {
                float subScore;
                GedcomStructure sub = structure.FindBestMatch(other.Substructures, out subScore);
                cumulativeScore += subScore;
            }
            return cumulativeScore;
        }

        /// <summary>
        /// Remember that this structure matches another structure.
        /// </summary>
        /// <param name="other">Matching structure</param>
        void SaveMatch(GedcomStructure other)
        {
            this.MatchStructure = new WeakReference<GedcomStructure>(other);
            other.MatchStructure = new WeakReference<GedcomStructure>(this);

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
        /// Find the best match in a given list of possibilities.
        /// </summary>
        /// <param name="others">List of possibilities to look for a match in</param>
        /// <param name="returnScore">Score of best match</param>
        /// <returns>Best match</returns>
        public GedcomStructure FindBestMatch(List<GedcomStructure> others, out float returnScore)
        {
            float bestScore = float.MinValue;
            GedcomStructure bestOther = null;
            foreach (GedcomStructure other in others)
            {
                if (other.MatchStructure != null)
                {
                    // Already matched something else.
                    continue;
                }
                float score = ScoreMatch(other);
                if (bestScore < score)
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
