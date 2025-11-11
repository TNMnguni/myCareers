using myCareers.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myCareers.Application.DTOs.JobPosting
{
    public class UpdateJobPostingDto
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Job title is required")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Job description is required")]
        [MinLength(50)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Department { get; set; }

        [MaxLength(100)]
        public string? Location { get; set; }

        [MaxLength(100)]
        public string? SalaryRange { get; set; }

        [Required]
        public EmploymentType EmploymentType { get; set; }

        [Required]
        public ExperienceLevel ExperienceLevel { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime ClosingDate { get; set; }
    }
}
