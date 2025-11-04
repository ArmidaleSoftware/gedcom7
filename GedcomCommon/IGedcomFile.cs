// Copyright (c) Armidale Software
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.IO;

namespace GedcomCommon
{
    public interface IGedcomFile
    {
        GedcomStructure SourceProduct { get; }
        GedcomVersion GedcomVersion { get; }
        GedcomStructure FindRecord(string xref);
        Dictionary<string, GedcomStructure> Records { get; }
        string SourceProductVersion { get; }
        string Date { get; }
        GedcomStructure Head { get; set; }
        GedcomStructure Trlr { get; set; }
        string GetUnmatchedSpacedLineVal(GedcomStructure current);
        GedcomStructureMatchInfo GetMatchInfo(GedcomStructure structure);
        bool GetIsMatchComplete(GedcomStructureMatchInfo current);
        GedcomComparisonReport Compare(IGedcomFile otherFile);
        List<string> LoadFromUrl(string url, string gedcomRegistriesPath = null);
        List<string> LoadFromStreamReader(StreamReader reader, GedcomVersion gedcomVersion = GedcomVersion.Unknown, string gedcomRegistriesPath = null);
        List<string> GetReferencedFiles();
        List<string> Validate();
    }
}
