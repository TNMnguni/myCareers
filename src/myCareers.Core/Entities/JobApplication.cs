using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using myCareers.Core.Enums;

namespace myCareers.Core.Entities
{
    public class JobApplication
    {
        [Key]
        public int Id { get; set; }

        // Foreign Keys with explicit configuration
        [Required]
        [ForeignKey(nameof(JobPosting))]
        public int JobPostingId { get; set; }

        [Required]
        [ForeignKey(nameof(Applicant))]
        public int ApplicantId { get; set; }

        // Status and Dates
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Draft;
        public DateTime AppliedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedDate { get; set; }
        public DateTime? InterviewDate { get; set; }

        [MaxLength(2000)]
        public string? ReviewNotes { get; set; }

        // Section 1: Position Info (auto-populated)
        [Required]
        [MaxLength(200)]
        public string JobTitle { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Department { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string ReferenceNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string NoticePeriod { get; set; } = string.Empty;

        // Section 2: Personal Info
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }

        [Required]
        [MaxLength(50)]
        public string IdPassportNumber { get; set; } = string.Empty;

        public Race Race { get; set; }
        public Gender Gender { get; set; }
        public bool HasDisability { get; set; }

        [MaxLength(500)]
        public string? DisabilityDetails { get; set; }

        public bool IsSouthAfricanCitizen { get; set; }

        [MaxLength(100)]
        public string? Nationality { get; set; }

        public bool? HasValidWorkPermit { get; set; }

        public bool HasCriminalRecord { get; set; }

        [MaxLength(1000)]
        public string? CriminalRecordDetails { get; set; }

        public bool HasPendingCriminalCase { get; set; }

        [MaxLength(1000)]
        public string? PendingCriminalCaseDetails { get; set; }

        public bool HasBeenDismissed { get; set; }

        [MaxLength(1000)]
        public string? DismissalDetails { get; set; }

        public bool HasPendingDisciplinary { get; set; }

        [MaxLength(1000)]
        public string? PendingDisciplinaryDetails { get; set; }

        public bool HasResignedPendingDisciplinary { get; set; }
        public bool HasBeenDischargedIllHealth { get; set; }

        public bool ConductsBusinessWithState { get; set; }

        [MaxLength(1000)]
        public string? BusinessWithStateDetails { get; set; }

        public bool WillRelinquishBusinessInterests { get; set; }

        public int? PrivateSectorExperienceYears { get; set; }
        public int? PublicSectorExperienceYears { get; set; }

        public bool RequiresOfficialRegistration { get; set; }
        public DateTime? RegistrationDate { get; set; }

        [MaxLength(100)]
        public string? RegistrationNumber { get; set; }

        // Section 3: Contact Details
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
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? AlternativePhoneNumber { get; set; }

        [Required]
        [MaxLength(200)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? PreferredLanguage { get; set; }

        [MaxLength(20)]
        public string? PreferredCommunicationMethod { get; set; }

        // Sections 4-8 stored as JSON for flexibility
        [Column(TypeName = "nvarchar(max)")]
        public string? LanguageProficiencies { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? Qualifications { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? WorkExperience { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? References { get; set; }

        // Section 9: Documents (deprecated - use ApplicationDocument table instead)
        [MaxLength(500)]
        public string? ResumeUrl { get; set; }

        [MaxLength(500)]
        public string? IdDocumentUrl { get; set; }

        [MaxLength(500)]
        public string? QualificationCertificateUrl { get; set; }

        [MaxLength(500)]
        public string? TranscriptUrl { get; set; }

        [MaxLength(500)]
        public string? Z83FormUrl { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? AdditionalDocumentsUrls { get; set; }

        // Audit fields
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }

        // Navigation Properties - EXPLICIT CONFIGURATION
        [ForeignKey(nameof(JobPostingId))]
        public virtual JobPosting JobPosting { get; set; } = null!;

        [ForeignKey(nameof(ApplicantId))]
        public virtual Applicant Applicant { get; set; } = null!;

        // Inverse navigation for documents
        public virtual ICollection<ApplicationDocument> Documents { get; set; } = new List<ApplicationDocument>();
    }
}
