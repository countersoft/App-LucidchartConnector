using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Countersoft.Gemini.Commons.Entity;
using LucidChart;

namespace LucidChart
{
    public class LucidChartModel
    {
        public string IssueId { get; set; }
        public string AppId { get; set; }
        public string ControlId { get; set; }
        public string ProjectId { get; set; }
        public string ProjectCode { get; set; }
        public IssueWidgetData<List<LucidChartData>> LucidChartData { get; set; }
        public bool HasData { get; set; }
        public bool IsGeminiLicenseFree {get; set;}
        public bool IsGeminiTrial { get; set; }
        public string Url { get; set; }
    }
}
