namespace DotNetAI.Model
{
    public class CV
    {
        public string? Name { get; set; }
        public string? Role { get; set; }
        public string? Overview { get; set; }
        public List<string>? TechnicalSkills { get; set; }
        public List<WorkExperience>? WorkExperiences { get; set; }
        public List<string>? Certifications { get; set; }
        public List<string>? Awards { get; set; }
    }

    public class WorkExperience
    {
        public string? Project { get; set; }
        public string? Position { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public string? Description { get; set; }
        public string? AdditionalDetail { get; set; }
    }
}
