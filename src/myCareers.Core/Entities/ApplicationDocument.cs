using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace myCareers.Core.Entities
{
    public class ApplicationDocument
    {
        [Key]
        public Guid FileId { get; set; }

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string FileUrl { get; set; } = string.Empty;

        public long FileSize { get; set; }

        [Required]
        [MaxLength(10)]
        public string FileType { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string DocumentType { get; set; } = string.Empty;

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        // Foreign Keys
        [Required]
        [ForeignKey(nameof(Applicant))]
        public int ApplicantId { get; set; }

        [ForeignKey(nameof(JobApplication))]
        public int? JobApplicationId { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(ApplicantId))]
        public virtual Applicant Applicant { get; set; } = null!;

        [ForeignKey(nameof(JobApplicationId))]
        public virtual JobApplication? JobApplication { get; set; }
    }
}
