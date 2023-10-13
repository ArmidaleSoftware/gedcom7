// Copyright (c) Armidale Software
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.IO;

namespace Gedcom7
{
    public class GedcomStructure
    {
        // Data members.
        public List<GedcomStructure> Substructures { get; set; }
        private object _superstructure;
        private GedcomStructure Superstructure
        {
            get
            {
                if (_superstructure is WeakReference<GedcomStructure>)
                {
                    WeakReference<GedcomStructure> wr = _superstructure as WeakReference<GedcomStructure>;
                    GedcomStructure target;
                    if (wr.TryGetTarget(out target))
                    {
                        return target;
                    }
                }
                return null;
            }
        }
        public GedcomFile File
        {
            get
            {
                if (_superstructure is WeakReference<GedcomFile>)
                {
                    WeakReference<GedcomFile> wr = _superstructure as WeakReference<GedcomFile>;
                    GedcomFile target;
                    if (wr.TryGetTarget(out target))
                    {
                        return target;
                    }
                }
                GedcomStructure superstructure = this.Superstructure;
                return superstructure?.File;
            }
        }
        public int LineNumber { get; private set; }
        public int Level { get; private set; }
        public string Xref { get; private set; }
        public GedcomStructureSchema Schema { get; private set; }
        public string Tag
        {
            get
            {
                return this.Schema?.StandardTag;
            }
            set
            {
                string sourceProgram = this.File.SourceProduct?.LineVal;
                string superstructureUri = this.Superstructure?.Schema.Uri;
                this.Schema = GedcomStructureSchema.GetSchema(sourceProgram, superstructureUri, value);
            }
        }
        public bool IsExtensionTag => this.Tag[0] == '_';
        public string LineVal { get; private set; }
        public string OriginalLine { get; private set; }

        // Functions and derived data members.
        public override string ToString() => this.OriginalLine;
        public bool IsNoteType => this.Tag == "NOTE" || this.Tag == "SNOTE";
        public bool IsNamePieceType => this.Tag == "NPFX" || this.Tag == "GIVN" || this.Tag == "NICK" || this.Tag == "SPFX" || this.Tag == "SURN" || this.Tag == "NSFX";
        public GedcomStructure FindFirstSubstructure(string tag) => this.Substructures.Find(x => x.Tag == tag);
        public string TagWithPath
        {
            get
            {
                string result = "";
                GedcomStructure superstructure = this.Superstructure;
                if (superstructure != null)
                {
                    result += superstructure.TagWithPath + ".";
                }
                result += this.Tag;
                return result;
            }
        }

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

        /// <summary>
        /// X may be a substructure of Y if any of the following apply:
        /// (a) X's YAML file has superstructures entry with key Y,
        /// (b) Y's YAML file has substructures entry with key X, or
        /// (c) X is a relocated standard structure (extension tag and standard URI)
        /// </summary>
        bool IsPlacementValid
        {
            get
            {
                if (this.Superstructure == null)
                {
                    return true;
                }

                // See if this structure's YAML file has a superstructures entry
                // for this structure's superstructure.
                if (this.Superstructure.Schema.IsDocumented &&
                    this.Schema.Superstructures.ContainsKey(this.Superstructure.Schema.Uri))
                {
                    return true;
                }

                // See if the superstructure's YAML file has a substructures entry
                // for this structure.
                if (this.Schema.IsDocumented &&
                    this.Superstructure.Schema.Substructures.ContainsKey(this.Schema.Uri))
                {
                    return true;
                }

                // See if this is a relocated standard structure.
                if (this.IsExtensionTag && this.Schema.IsStandard)
                {
                    return true;
                }

                // See if this is an undocumented extension structure.
                if (this.IsExtensionTag && !this.Schema.IsDocumented)
                {
                    return true;
                }

                // See if the superstructure is an undocumented extension structure.
                if (this.Superstructure.IsExtensionTag && !this.Superstructure.Schema.IsDocumented)
                {
                    return true;
                }

                // See if the superstructure's path has an extension in it.
                if (this.Superstructure.Superstructure?.TagWithPath.Contains('_') ?? false)
                {
                    return true;
                }

                // See if this is a CONT pseudostructure under a string structure.
                if ((this.Schema.StandardTag == "CONT") &&
                    (this.Superstructure.Schema.Payload == "http://www.w3.org/2001/XMLSchema#string"))
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsValid
        {
            get
            {
                if (!this.IsPlacementValid)
                {
                    return false;
                }
                Dictionary<string, int> foundCount = new Dictionary<string, int>();
                foreach (var substructure in this.Substructures)
                {
                    if (!substructure.IsValid)
                    {
                        return false;
                    }
                    if (substructure.Schema.IsDocumented)
                    {
                        if (foundCount.ContainsKey(substructure.Schema.Uri))
                        {
                            foundCount[substructure.Schema.Uri]++;
                        }
                        else
                        {
                            foundCount[substructure.Schema.Uri] = 1;
                        }
                    }
                }

                // TRLR and CONT cannot contain substructures.
                if ((this.Tag == "CONT" || this.Tag == "TRLR")
                    && (this.Substructures.Count > 0))
                {
                    return false;
                }

                // Check cardinality of permitted substructures.
                foreach (var substructureSchemaPair in this.Schema.Substructures)
                {
                    string uri = substructureSchemaPair.Key;
                    GedcomStructureCountInfo countInfo = substructureSchemaPair.Value;
                    if (countInfo.Required && !foundCount.ContainsKey(uri))
                    {
                        // Missing required substructure.
                        return false;
                    }
                    if (countInfo.Singleton && foundCount.ContainsKey(uri) &&
                        (foundCount[uri] > 1))
                    {
                        // Contains multiple when only a singleton is permitted.
                        return false;
                    }
                }

                return true;
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

        public string SpacedLineVal => " " + this.LineVal + " ";

        public GedcomStructure(GedcomFile file, int lineNumber, string line, List<GedcomStructure> structurePath)
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

            // Update path to current structure.
            structurePath.RemoveRange(this.Level, structurePath.Count - this.Level);
            structurePath.Add(this);

            if (this.Level > 0)
            {
                GedcomStructure superstructure = structurePath[this.Level - 1];
                this._superstructure = new WeakReference<GedcomStructure>(superstructure);
                superstructure.Substructures.Add(this);
            }
            else
            {
                this._superstructure = new WeakReference<GedcomFile>(file);
            }

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

        /// <summary>
        /// Compare two name piece structures to see if one is a subset of the other.
        /// </summary>
        /// <param name="superset">Potential superset to compare against</param>
        /// <param name="subset">Potential subset to compare</param>
        /// <returns>positive if similar, negative if dissimilar</returns>
        static float ScoreNamePiece(GedcomStructure superset, GedcomStructure subset)
        {
            GedcomFile file = superset.File;
            if (file == null)
            {
                return 0;
            }
            return file.GetUnmatchedSpacedLineVal(superset).Contains(subset.SpacedLineVal) ? 1 : -1;
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
            GedcomStructure sharedNoteRecord = this.File?.FindRecord("SNOTE", this.LineVal);
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
            if (IsNamePieceType && other.IsNamePieceType)
            {
                return Math.Max(ScoreNamePiece(this, other), ScoreNamePiece(other, this));
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

        /// <summary>
        /// Find the best match in a given list of possibilities.
        /// </summary>
        /// <param name="others">List of possibilities to look for a match in</param>
        /// <param name="returnScore">Score of best match</param>
        /// <returns>Best match</returns>
        public GedcomStructure FindBestMatch(List<GedcomStructure> others, out float returnScore)
        {
            returnScore = 0;
            GedcomFile file = this.File;
            if (file == null)
            {
                return null;
            }
            float bestScore = 0;
            GedcomStructure bestOther = null;
            foreach (GedcomStructure other in others)
            {
                GedcomFile otherFile = other.File;
                if (otherFile == null)
                {
                    return null;
                }
                float score = ScoreMatch(other);
                GedcomStructureMatchInfo otherMatchInfo = otherFile.GetMatchInfo(other);
                if (otherFile.GetIsMatchComplete(otherMatchInfo) && (otherMatchInfo.Score >= score))
                {
                    // Already matched something else.
                    continue;
                }
                if (bestOther == null || bestScore < score)
                {
                    bestScore = score;
                    bestOther = other;
                }
            }
            returnScore = bestScore;
            return bestOther;
        }
    }

    public class GedcomStructureMatchInfo
    {
        // Data members.

        /// <summary>
        /// GEDCOM structure looking for matches.
        /// </summary>
        public GedcomStructure Structure { get; set; }
        public List<WeakReference<GedcomStructure>> MatchStructures { get; set; }
        
        /// <summary>
        /// Comparison score.
        /// </summary>
        public float Score { get; set; }

        // Constructor.
        public GedcomStructureMatchInfo(GedcomStructure structure)
        {
            this.MatchStructures = new List<WeakReference<GedcomStructure>>();
            this.Structure = structure;
            this.Score = 0;
        }
    }
}
