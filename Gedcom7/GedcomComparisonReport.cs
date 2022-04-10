// Copyright (c) Armidale Software
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Text;

namespace Gedcom7
{
    public class GedcomComparisonReport
    {
        int LineCount { get; set; }
        public List<GedcomStructure> StructuresAdded { get; set; }
        public List<GedcomStructure> StructuresRemoved { get; set; }
        public int CompliancePercentage
        {
            get
            {
                double lossFraction = 100.0 * this.StructuresRemoved.Count / this.LineCount;
                int compliancePercentage = (int)(100 - lossFraction);
                return compliancePercentage;
            }
        }

        public GedcomComparisonReport(int lineCount)
        {
            this.LineCount = lineCount;
            this.StructuresAdded = new List<GedcomStructure>();
            this.StructuresRemoved = new List<GedcomStructure>();
        }
    }
}
