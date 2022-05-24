using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Gedcom7
{
    public class GedcomCompatibilityReport
    {
        public GedcomComparisonReport Maximal70Report { get; private set; }
        public GedcomComparisonReport Tree1Report { get; private set; }
        public GedcomComparisonReport Tree2Report { get; private set; }
        public GedcomComparisonReport Memories1Report { get; private set; }
        public GedcomComparisonReport Memories2Report { get; private set; }
        public GedcomComparisonReport LdsReport { get; private set; }

        private int CompatibilityPercentage(GedcomComparisonReport report)
        {
            double lossFraction = 100.0 * report.StructuresRemoved.Count / report.LineCount;
            int compatabilityPercentage = (int)(100 - lossFraction);
            return compatabilityPercentage;
        }

        public int Tree1CompatibilityPercentage => CompatibilityPercentage(this.Tree1Report);
        public int Tree2CompatibilityPercentage => CompatibilityPercentage(this.Tree2Report);
        public int Memories1CompatibilityPercentage => CompatibilityPercentage(this.Memories1Report);
        public int Memories2CompatibilityPercentage => CompatibilityPercentage(this.Memories2Report);
        public int LdsCompatibilityPercentage => CompatibilityPercentage(this.LdsReport);
        public int MaximalCompatibilityPercentage => CompatibilityPercentage(this.Maximal70Report);

        public static string GetOutput(string label, int compatibilityPercentage, GedcomComparisonReport report)
        {
            string output = label + ": " + compatibilityPercentage + "%\n";
            output += "   Structures removed: " + report.StructuresRemoved.Count + "\n";
            foreach (GedcomStructure structure in report.StructuresRemoved)
            {
                output += "      " + structure.LineWithPath + "\n";
            }
            return output;
        }

        private GedcomComparisonReport Compare(string baseline, GedcomFile file)
        {
            var baselineFile = new GedcomFile();
            if (!baselineFile.Load(baseline)) {
                return null;
            }
            GedcomComparisonReport report = baselineFile.Compare(file);
            file.ResetComparison();
            return report;
        }

        public GedcomCompatibilityReport(GedcomFile file, string pathToBaselineFiles)
        {
            this.Maximal70Report = Compare(Path.Combine(pathToBaselineFiles, "maximal70.ged"), file);
            this.Tree1Report = Compare(Path.Combine(pathToBaselineFiles, "maximal70-tree1.ged"), file);
            this.Tree2Report = Compare(Path.Combine(pathToBaselineFiles, "maximal70-tree2.ged"), file);
            this.Memories1Report = Compare(Path.Combine(pathToBaselineFiles, "maximal70-memories1.ged"), file);
            this.Memories2Report = Compare(Path.Combine(pathToBaselineFiles, "maximal70-memories2.ged"), file);
            this.LdsReport = Compare(Path.Combine(pathToBaselineFiles, "maximal70-lds.ged"), file);
        }
    }
}
