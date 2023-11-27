// Copyright (c) Armidale Software
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;

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
                string superstructureUri = (this.Level == 0) ? GedcomStructureSchema.RecordSuperstructureUri : this.Superstructure?.Schema?.Uri;
                this.Schema = GedcomStructureSchema.GetSchema(sourceProgram, superstructureUri, value);
            }
        }
        public bool IsExtensionTag => (this.Tag.Length > 0) && (this.Tag[0] == '_');
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
        /// <returns>Error message, or null on success</returns>
        string ValidatePlacement()
        {
            if (this.Superstructure == null)
            {
                return (this.Schema.Superstructures.Count == 0) ? null :
                    ErrorMessage(this.Tag + " is not a valid record");
            }

            // See if this structure's YAML file has a superstructures entry
            // for this structure's superstructure.
            if (this.Superstructure.Schema.IsDocumented &&
                this.Schema.Superstructures.ContainsKey(this.Superstructure.Schema.Uri))
            {
                return null;
            }

            // See if the superstructure's YAML file has a substructures entry
            // for this structure.
            if (this.Schema.IsDocumented &&
                this.Superstructure.Schema.Substructures.ContainsKey(this.Schema.Uri))
            {
                return null;
            }

            // See if this is a relocated standard structure.
            if (this.IsExtensionTag && this.Schema.IsStandard)
            {
                return null;
            }

            // See if this is an undocumented extension structure.
            if (this.IsExtensionTag && !this.Schema.IsDocumented)
            {
                return null;
            }

            // See if the superstructure is an undocumented extension structure.
            if (this.Superstructure.IsExtensionTag && !this.Superstructure.Schema.IsDocumented)
            {
                return null;
            }

            // See if the superstructure's path has an extension in it.
            if (this.Superstructure.Superstructure?.TagWithPath.Contains('_') ?? false)
            {
                return null;
            }

            // See if this is a CONT pseudostructure under a string structure.
            if ((this.Schema.StandardTag == "CONT") &&
                (this.Superstructure.Schema.Payload == "http://www.w3.org/2001/XMLSchema#string"))
            {
                return null;
            }

            return ErrorMessage(this.Tag + " is not a valid substructure of " + this.Superstructure.Tag);
        }

        /// <summary>
        /// Validate this structure.
        /// </summary>
        /// <returns>List of 0 or more error messages</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();
            string placementError = ValidatePlacement();
            if (placementError != null)
            {
                errors.Add(placementError);
            }

            Dictionary<string, int> foundCount = new Dictionary<string, int>();
            foreach (var substructure in this.Substructures)
            {
                errors.AddRange(substructure.Validate());
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
                errors.Add(ErrorMessage(this.Tag + " must not contain substructures"));
            }

            // Check cardinality of permitted substructures.
            foreach (var substructureSchemaPair in this.Schema.Substructures)
            {
                string uri = substructureSchemaPair.Key;
                GedcomStructureCountInfo countInfo = substructureSchemaPair.Value;
                if (countInfo.Required && !foundCount.ContainsKey(uri))
                {
                    // Missing required substructure.
                    errors.Add(ErrorMessage(this.Tag + " is missing a substructure of type " + uri));
                }
                if (countInfo.Singleton && foundCount.ContainsKey(uri) &&
                    (foundCount[uri] > 1))
                {
                    // Contains multiple when only a singleton is permitted.
                    errors.Add(ErrorMessage(this.Tag + " does not permit multiple substructures of type " + uri));
                }
            }

            // Check pointers.
            if (this.Schema.HasPointer && (this.LineVal != "@VOID@"))
            {
                // Try to resolve the pointer.
                string xref = this.LineVal;
                GedcomFile file = this.File;
                GedcomStructure record = file.FindRecord(xref);
                if (record == null)
                {
                    errors.Add(ErrorMessage(xref + " has no associated record"));
                }
                else
                {
                    string expectedRecordUri = this.Schema.Payload.Substring(2, this.Schema.Payload.Length - 4);

                    // Verify the record type.
                    if (record.Schema.Uri != expectedRecordUri)
                    {
                        errors.Add(ErrorMessage(this.Tag + " points to a " + record.Tag + " record"));
                    }
                }
            }

            return errors;
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

        /// <summary>
        /// Test whether a given string is a valid media type.
        /// </summary>
        /// <param name="value">String to test</param>
        /// <returns>true if valid, false if not</returns>
        private static bool IsValidMediaType(string value)
        {
            if (value == null || value.Length == 0) return false;
            int slashes = 0;
            foreach (char c in value)
            {
                if (c == '/') slashes++;
                else if (!Char.IsLetterOrDigit(c))
                {
                    return false;
                }
            }
            if ((value[0] == '/') || (value[value.Length - 1] == '/') || (slashes != 1))
            {
                return false;
            }
            return true;
        }

        public string SpacedLineVal => " " + this.LineVal + " ";

        /// <summary>
        /// Parse a line of text into a GEDCOM structure.
        /// </summary>
        /// <param name="file">GEDCOM file the line came from</param>
        /// <param name="lineNumber">Line number in the file</param>
        /// <param name="line">Line text</param>
        /// <param name="structurePath">Prior structure path</param>
        /// <returns>Error message, or null on success</returns>
        public string Parse(GedcomFile file, int lineNumber, string line, List<GedcomStructure> structurePath)
        {
            this.LineNumber = lineNumber;
            this.OriginalLine = line;
            this.Substructures = new List<GedcomStructure>();

            // Parse line into Level, Xref, Tag, Pointer, and LineVal.
            if (line == null)
            {
                return ErrorMessage("No line text");
            }
            string[] tokens = line.Split(' ');
            int index = 0;

            GedcomVersion gedcomVersion = file.GedcomVersion;
            if (gedcomVersion != GedcomVersion.V70)
            {
                // Prior to GEDCOM 7, leading whitespace was allowed.
                while (tokens[index].Length == 0)
                {
                    index++;
                }
            }

            int level;
            if (Int32.TryParse(tokens[index++], out level))
            {
                this.Level = level;
            }
            else
            {
                return ErrorMessage("Line must start with an integer");
            }

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

                if ((tokens.Length > index) && (tokens[index].Length > 0) && (tokens[index][0] == '@'))
                {
                    this.Xref = tokens[index++];
                    if ((this.Xref.Length < 3) || !this.Xref.EndsWith('@'))
                    {
                        return ErrorMessage("Xref must start and end with @");
                    }
                    string value = this.Xref.Substring(1, this.Xref.Length - 2);
                    if (file.GedcomVersion == GedcomVersion.V70)
                    {
                        if (this.Xref == "@VOID@")
                        {
                            return ErrorMessage("Xref must not be @VOID@");
                        }
                        foreach (var c in value)
                        {
                            if (!(Char.IsUpper(c) || Char.IsDigit(c) || c == '_'))
                            {
                                return ErrorMessage("Invalid character '" + c + "' in Xref \"" + this.Xref + "\"");
                            }
                        }
                    }
                    else
                    {
                        if (!Char.IsLetterOrDigit(value[0]))
                        {
                            return ErrorMessage("Xref \"" + this.Xref + "\" does not start with a letter or digit");
                        }
                        if (value.Contains('_'))
                        {
                            return ErrorMessage("Invalid character '_' in Xref \"" + this.Xref + "\"");
                        }
                    }
                }
            }

            if (tokens.Length > index)
            {
                this.Tag = tokens[index++];
                if (this.Tag == "")
                {
                    return ErrorMessage("Tag must not be empty");
                }
                if (this.Schema != null && !this.Schema.IsDocumented && this.Schema.IsStandard && this.Level == 0)
                {
                    return ErrorMessage("Undocumented standard record");
                }

                if (tokens.Length > index)
                {
                    int offset = line.IndexOf(this.Tag);
                    this.LineVal = line.Substring(offset + this.Tag.Length + 1);
                    if (this.LineVal == "")
                    {
                        // An empty payload is not valid after a space.
                        return ErrorMessage("An empty payload is not valid after a space");
                    }
                    if (this.Tag == "CONT")
                    {
                        // We don't currently do any validation on the payload.
                        return null;
                    }
                    if (!this.Schema?.IsDocumented ?? false)
                    {
                        // We can't do validation on undocumented structures, whether
                        // extension or standard (under an extension).
                        return null;
                    }
                }

                // TODO: use a payload-specific parser subclass instead of a string.
                string payloadType = this.Schema?.Payload;
                switch (payloadType)
                {
                    case "http://www.w3.org/2001/XMLSchema#nonNegativeInteger":
                        {
                            UInt32 value;
                            if (!UInt32.TryParse(this.LineVal, out value))
                            {
                                return ErrorMessage("\"" + this.LineVal + "\" is not a non-negative integer");
                            }
                            break;
                        }
                    case null:
                        {
                            if (this.LineVal != null)
                            {
                                return ErrorMessage(this.Tag + " payload must be null");
                            }
                            break;
                        }
                    case "Y|<NULL>":
                        {
                            if (this.LineVal != null && this.LineVal != "Y")
                            {
                                return ErrorMessage(this.Tag + " payload must be 'Y' or empty");
                            }
                            break;
                        }
                    case "http://www.w3.org/2001/XMLSchema#string":
                        // We currently don't do any further validation.
                        break;
                    case "https://gedcom.io/terms/v7/type-Date#exact":
                        // TODO: validate exact date payload
                        break;
                    case "https://gedcom.io/terms/v7/type-Date":
                        // TODO: validate Date payload
                        break;
                    case "https://gedcom.io/terms/v7/type-Date#period":
                        // TODO: validate date period payload
                        break;
                    case "https://gedcom.io/terms/v7/type-Time":
                        // TODO: validate Time payload
                        break;
                    case "https://gedcom.io/terms/v7/type-Name":
                        // TODO: validate Name payload
                        break;
                    case "https://gedcom.io/terms/v7/type-Enum":
                        if (!this.Schema.EnumerationSet.IsValidValue(this.LineVal))
                        {
                            return ErrorMessage("\"" + this.LineVal + "\" is not a valid value for " + this.Tag);
                        }
                        break;
                    case "https://gedcom.io/terms/v7/type-List#Text":
                        // We currently don't do any further validation.
                        break;
                    case "https://gedcom.io/terms/v7/type-List#Enum":
                        {
                            string[] values = this.LineVal.Split(',');
                            foreach (var value in values)
                            {
                                string trimmedValue = value.Trim();
                                if (!this.Schema.EnumerationSet.IsValidValue(trimmedValue))
                                {
                                    return ErrorMessage("\"" + trimmedValue + "\" is not a valid value for " + this.Tag);
                                }
                            }
                        }
                        break;
                    case "http://www.w3.org/ns/dcat#mediaType":
                        {
                            if (!IsValidMediaType(this.LineVal))
                            {
                                return ErrorMessage("\"" + this.LineVal + "\" is not a valid media type");
                            }
                            if (this.Tag == "MIME" && this.LineVal != "text/plain" && this.LineVal != "text/html")
                            {
                                return ErrorMessage(this.Tag + " payload must be text/plain or text/html");
                            }
                            break;
                        }
                    case "http://www.w3.org/2001/XMLSchema#Language":
                        // TODO: validate Language payload
                        break;
                    case "https://gedcom.io/terms/v7/type-Age":
                        // TODO: parse Age payload
                        break;
                    default:
                        if (this.Schema?.HasPointer ?? false)
                        {
                            string recordType = payloadType.Substring(2, payloadType.Length - 4);
                            if (this.LineVal == null || this.LineVal.Length < 3 || this.LineVal[0] != '@' || this.LineVal[this.LineVal.Length - 1] != '@')
                            {
                                return ErrorMessage("Payload must be a pointer");
                            }
                            break;
                        }
                        return ErrorMessage("TODO: unrecognized payload type");
                }
            }

            string error = null;
            if (this.Level == 0)
            {
                if ((this.Tag == "HEAD") != (file.Records.Count == 0))
                {
                    return ErrorMessage("HEAD must be the first record");
                }
                if (file.Records.ContainsKey("TRLR"))
                {
                    return ErrorMessage("Duplicate TRLR record");
                }

                // An xref is disallowed for HEAD and TRLR and required for all others.
                string tag = this.Tag;
                if ((tag == "HEAD" || tag == "TRLR") != (this.Xref == null))
                {
                    if (this.Xref == null)
                    {
                        error = ErrorMessage("Missing Xref for this record");
                    } else
                    {
                        error = ErrorMessage("Xref is not valid for this record");
                    }
                }
                if (this.Xref != null)
                {
                    if (file.Records.ContainsKey(this.Xref))
                    {
                        return ErrorMessage("Duplicate Xref " + this.Xref);
                    }
                    file.Records[this.Xref] = this;
                }
                else
                {
                    file.Records[this.Tag] = this;
                }
            }

            return error;
        }

        public string ErrorMessage(string message)
        {
            return String.Format("Line {0}: {1}", this.LineNumber, message);
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
            GedcomStructure sharedNoteRecord = this.File?.FindRecord(this.LineVal);
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
