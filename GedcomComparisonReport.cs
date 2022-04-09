// Copyright (c) Armidale Software
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Text;

namespace Gedcom7
{
    public class GedcomComparisonReport
    {
        public List<GedcomStructure> StructuresAdded { get; set; }
        public List<GedcomStructure> StructuresRemoved { get; set; }

        public GedcomComparisonReport()
        {
            this.StructuresAdded = new List<GedcomStructure>();
            this.StructuresRemoved = new List<GedcomStructure>();
        }
    }
}
