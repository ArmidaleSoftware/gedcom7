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
            bool ok = file.Load(filename);
            if (!ok)
            {
                Console.WriteLine("Failed to load " + filename);
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

        static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("usage: GedCompare <filename1> <filename2>");
                return 1;
            }

            GedcomFile file1 = LoadFile(args[0]);
            GedcomFile file2 = LoadFile(args[1]);
            if (file1 == null || file2 == null)
            {
                return 1;
            }

            GedcomComparisonReport report = file1.Compare(file2);
            Console.WriteLine("Structures added: " + report.StructuresAdded.Count);
            DumpStructures(report.StructuresAdded);
            Console.WriteLine();

            Console.WriteLine("Structures removed: " + report.StructuresRemoved.Count);
            DumpStructures(report.StructuresRemoved);
            Console.WriteLine();

            Console.WriteLine("Compliance rating: " + report.CompliancePercentage + "%");

            return 0;
        }
    }
}
