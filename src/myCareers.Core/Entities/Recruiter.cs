using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace myCareers.Core.Entities
{
    public class Recruiter
    {

        public int Id { get; set; }  // Its own primary key
        public int UserId { get; set; }  // Foreign key to User

        [MaxLength(200)]
        public string? Department { get; set; }

        [MaxLength(100)]
        public string? JobTitle { get; set; }

        [MaxLength(20)]
        public string? EmployeeId { get; set; }

        public DateTime? StartDate { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;
        public virtual ICollection<JobPosting> JobPostings { get; set; } = new List<JobPosting>();
    }
}
