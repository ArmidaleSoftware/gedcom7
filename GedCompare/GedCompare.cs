// Copyright (c) Armidale Software
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;

namespace Gedcom7
{
    class GedCompare
    {
        /// <summary>
        /// Load a GEDCOM file into memory.
        /// </summary>
        /// <param name="filename">Path to file to load</param>
        /// <returns></returns>
        static GedcomFile LoadFile(string filename)
        {
            var file = new GedcomFile();
            string error = file.LoadFromPath(filename);
            if (error != null)
            {
                Console.WriteLine("Failed to load " + filename + ": " + error + "\n");
                return null;
            }
            return file;
        }

        static void DumpStructures(List<GedcomStructure> list)
        {
            foreach (GedcomStructure structure in list)
            {
                Console.WriteLine(structure.LineWithPath);
            }
        }

        static int Compare(string filename1, string filename2)
        {
            GedcomFile file1 = LoadFile(filename1);
            if (file1 == null)
            {
                Console.WriteLine("Could not load " + filename1);
                return 1;
            }
            GedcomFile file2 = LoadFile(filename2);
            if (file2 == null)
            {
                Console.WriteLine("Could not load " + filename2);
                return 1;
            }

            GedcomComparisonReport report = file1.Compare(file2);
            Console.WriteLine("Source program: " + file2.SourceProgram + "\n");

            Console.WriteLine("Structures added: " + report.StructuresAdded.Count);
            DumpStructures(report.StructuresAdded);
            Console.WriteLine();

            Console.WriteLine("Structures removed: " + report.StructuresRemoved.Count);
            DumpStructures(report.StructuresRemoved);
            Console.WriteLine();
            return 0;
        }

        static int CheckCompatibility(string filename, bool includePercentage = false)
        {
            GedcomFile file = LoadFile(filename);
            if (file == null)
            {
                Console.WriteLine("Could not load " + filename);
                return 1;
            }
            GedcomCompatibilityReport report = new GedcomCompatibilityReport(file);

            Console.WriteLine("Baseline Version: " + report.BaselineVersion + " (" + report.BaselineDate + ")");
            if (includePercentage)
            {
                Console.WriteLine("Overall compatibility percentage: " + report.Maximal70Report.CompatibilityPercentage + "%");
            }
            Console.WriteLine();
            Console.WriteLine("Category Compatibility");
            Console.WriteLine("------------------------");
            Console.WriteLine(GedcomCompatibilityReport.GetOutput("Tree Level 1", report.Tree1CompatibilityPercentage, report.Tree1Report, includePercentage));
            Console.WriteLine(GedcomCompatibilityReport.GetOutput("Tree Level 2", report.Tree2CompatibilityPercentage, report.Tree2Report, includePercentage));
            Console.WriteLine(GedcomCompatibilityReport.GetOutput("Memories Level 1", report.Memories1CompatibilityPercentage, report.Memories1Report, includePercentage));
            Console.WriteLine(GedcomCompatibilityReport.GetOutput("Memories Level 2", report.Memories2CompatibilityPercentage, report.Memories2Report, includePercentage));
            Console.WriteLine(GedcomCompatibilityReport.GetOutput("Latter-day Saint Services", report.LdsCompatibilityPercentage, report.LdsReport, includePercentage));
            return 0;
        }

        static int Main(string[] args)
        {
            if (args.Length == 2)
            {
                return Compare(args[0], args[1]);
            }
            if (args.Length == 1)
            {
                return CheckCompatibility(args[1]);
            }

            Console.WriteLine("usage: GedCompare <filename1> <filename2>");
            Console.WriteLine("          to simply compare two GEDCOM files");
            Console.WriteLine("       GedCompare <filename>");
            Console.WriteLine("          to generate a FamilySearch GEDCOM 7 compatibility report");
            return 1;
        }
    }
}
