namespace Mediscribe_AI.Models.ResponseVM
{
    public class SoapNotesResponse
    {
        public string Subjective { get; set; }
        public string Objective { get; set; }
        public string Assessment { get; set; }
        public PlanDetail Plan { get; set; }

        public string HtmlFormat { get; set; }
    }
    public class PlanDetail
    {
        public List<string> Investigations { get; set; } = new();
        public List<string> Medications { get; set; } = new();
        public List<string> LifestyleAdvice { get; set; } = new();
        public List<string> Referrals { get; set; } = new();
        public List<string> Monitoring { get; set; } = new();
    }
}
