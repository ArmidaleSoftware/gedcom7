using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Gedcom7
{
    public class GedcomCompatibilityReport
    {
        GedcomComparisonReport Maximal70Report;
        GedcomComparisonReport Tree1Report;
        GedcomComparisonReport Tree2Report;
        GedcomComparisonReport Memories1Report;
        GedcomComparisonReport Memories2Report;
        GedcomComparisonReport LdsReport;

        private int CompatibilityPercentage(GedcomComparisonReport report, GedcomComparisonReport prereqReport = null)
        {
            int deltaLineCount = report.LineCount - ((prereqReport != null) ? prereqReport.LineCount : 0);
            int deltaLossCount = report.StructuresRemoved.Count - ((prereqReport != null) ? prereqReport.StructuresRemoved.Count : 0);
            double lossFraction = 100.0 * deltaLossCount / deltaLineCount;
            int compatabilityPercentage = (int)(100 - lossFraction);
            return compatabilityPercentage;
        }

        public int Tree1CompatibilityPercentage => CompatibilityPercentage(this.Tree1Report);
        public int Tree2CompatibilityPercentage => CompatibilityPercentage(this.Tree2Report, this.Tree1Report);
        public int Memories1CompatibilityPercentage => CompatibilityPercentage(this.Memories1Report, this.Tree1Report);
        public int Memories2CompatibilityPercentage => CompatibilityPercentage(this.Memories2Report, this.Memories1Report);
        public int LdsCompatibilityPercentage => CompatibilityPercentage(this.LdsReport, this.Tree1Report);
        public int MaximalCompatibilityPercentage => CompatibilityPercentage(this.Maximal70Report);

        private GedcomComparisonReport Compare(string baseline, GedcomFile file)
        {
            var baselineFile = new GedcomFile();
            baselineFile.Load(baseline);
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
