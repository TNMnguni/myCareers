namespace myCareers.Core.Entities
{
    public class LanguageProficiencyItem
    {
        public string Language { get; set; } = string.Empty;
        public string SpeakLevel { get; set; } = string.Empty;
        public string WriteReadLevel { get; set; } = string.Empty;
    }

    public class QualificationItem
    {
        public string InstitutionName { get; set; } = string.Empty;
        public string QualificationName { get; set; } = string.Empty;
        public int YearObtained { get; set; }
        public string? FieldOfStudy { get; set; }
    }

    public class WorkExperienceItem
    {
        public string EmployerName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsCurrentPosition { get; set; }
        public string? ReasonForLeaving { get; set; }
        public string? KeyResponsibilities { get; set; }
    }

    public class ReferenceItem
    {
        public string Name { get; set; } = string.Empty;
        public string Relationship { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Organization { get; set; }
    }
}