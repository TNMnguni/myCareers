using System.ComponentModel.DataAnnotations;
using myCareers.Core.Enums;
using myCareers.Core.Entities;

namespace myCareers.Application.DTOs.JobApplication
{
    // Main DTO that holds all sections
    public class CreateJobApplicationDto
    {
        // Section 1: Position
        public int JobPostingId { get; set; }
        public PositionInfoDto PositionInfo { get; set; } = new();

        // Section 2: Personal Info
        public PersonalInfoDto PersonalInfo { get; set; } = new();

        // Section 3: Contact Details
        public ContactDetailsDto ContactDetails { get; set; } = new();

        // Section 4: Language Proficiency
        public List<LanguageProficiencyDto> LanguageProficiencies { get; set; } = new();

        // Section 5: Qualifications
        public List<QualificationDto> Qualifications { get; set; } = new();

        // Section 7: Work Experience
        public List<WorkExperienceDto> WorkExperience { get; set; } = new();

        // Section 8: References
        public List<ReferenceDto> References { get; set; } = new();

        // Section 9: Documents (handled separately via file upload)
       
        public DocumentsDto? Documents { get; set; }
    }

    public class PositionInfoDto
    {
        [Required]
        public string JobTitle { get; set; } = string.Empty;
        [Required]
        public string Department { get; set; } = string.Empty;
        [Required]
        public string ReferenceNumber { get; set; } = string.Empty;
        [Required]
        [MaxLength(50)]
        public string NoticePeriod { get; set; } = string.Empty;
    }

    public class PersonalInfoDto
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public DateTime? DateOfBirth { get; set; }

        [Required]
        [MaxLength(50)]
        public string IdPassportNumber { get; set; } = string.Empty;

        [Required]
        public Race Race { get; set; }

        [Required]
        public Gender Gender { get; set; }

        public bool HasDisability { get; set; }
        public string? DisabilityDetails { get; set; }

        public bool IsSouthAfricanCitizen { get; set; }
        public string? Nationality { get; set; }
        public bool? HasValidWorkPermit { get; set; }

        public bool HasCriminalRecord { get; set; }
        public string? CriminalRecordDetails { get; set; }

        public bool HasPendingCriminalCase { get; set; }
        public string? PendingCriminalCaseDetails { get; set; }

        public bool HasBeenDismissed { get; set; }
        public string? DismissalDetails { get; set; }

        public bool HasPendingDisciplinary { get; set; }
        public string? PendingDisciplinaryDetails { get; set; }

        public bool HasResignedPendingDisciplinary { get; set; }
        public bool HasBeenDischargedIllHealth { get; set; }

        public bool ConductsBusinessWithState { get; set; }
        public string? BusinessWithStateDetails { get; set; }

        public bool WillRelinquishBusinessInterests { get; set; }

        public int? PrivateSectorExperienceYears { get; set; }
        public int? PublicSectorExperienceYears { get; set; }

        public bool RequiresOfficialRegistration { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public string? RegistrationNumber { get; set; }
    }

    public class ContactDetailsDto
    {
        [Required]
        [MaxLength(200)]
        public string PhysicalAddress { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string PostalCode { get; set; } = string.Empty;

        [Required]
        [Phone]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Phone]
        [MaxLength(20)]
        public string? AlternativePhoneNumber { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PreferredLanguage { get; set; } = string.Empty;

        [Required]
        public string PreferredCommunicationMethod { get; set; } = string.Empty;
    }

    public class LanguageProficiencyDto
    {
        [Required]
        public string Language { get; set; } = string.Empty;

        [Required]
        public string SpeakLevel { get; set; } = string.Empty;

        [Required]
        public string WriteReadLevel { get; set; } = string.Empty;
    }

    public class QualificationDto
    {
        [Required]
        public string InstitutionName { get; set; } = string.Empty;

        [Required]
        public string QualificationName { get; set; } = string.Empty;

        [Required]
        public int YearObtained { get; set; }

        public string? FieldOfStudy { get; set; }
    }

    public class WorkExperienceDto
    {
        [Required]
        public string EmployerName { get; set; } = string.Empty;

        [Required]
        public string Position { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public bool IsCurrentPosition { get; set; }

        public string? ReasonForLeaving { get; set; }

        public string? KeyResponsibilities { get; set; }
    }

    public class ReferenceDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Relationship { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }

        public string? Organization { get; set; }
    }

    public class DocumentsDto
    {
        public List<Guid> UploadedDocumentIds { get; set; } = new List<Guid>();
    }

    public class JobApplicationResult
    {
        public bool Success { get; set; }
        public JobApplicationDto? Application { get; set; }
        public List<string> Errors { get; set; } = new();
        public string? Message { get; set; }
    }
}