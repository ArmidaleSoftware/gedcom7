﻿// Copyright (c) Armidale Software
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Gedcom7
{
    public class GedcomFile
    {
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
                structure.FindAndSaveBestMatch(otherFile.Records);
            }

            var report = new GedcomComparisonReport(this.LineCount);
            foreach (GedcomStructure structure in this.Records)
            {
                structure.AppendNonMatchingStructures(report.StructuresRemoved);
            }
            foreach (GedcomStructure structure in otherFile.Records)
            {
                structure.AppendNonMatchingStructures(report.StructuresAdded);
            }
            this.ResetComparison();
            otherFile.ResetComparison();

            return report;
        }

        public void ResetComparison()
        {
            foreach (GedcomStructure structure in this.Records)
            {
                structure.ClearMatch();
            }
        }
    }
}
