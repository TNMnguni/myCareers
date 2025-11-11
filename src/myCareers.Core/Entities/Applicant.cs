using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myCareers.Core.Entities
{
    public class Applicant 
    {
        public int Id { get; set; }  // Its own primary key
        public int UserId { get; set; }  // Foreign key to User

        [MaxLength(20)]
        public string? IdNumber { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [MaxLength(200)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(20)]
        public string? PostalCode { get; set; }

        [MaxLength(100)]
        public string? Nationality { get; set; }

        [MaxLength(50)]
        public string? Gender { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        public virtual ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
        public virtual ICollection<ApplicationDocument> Documents { get; set; } = new List<ApplicationDocument>();
    }
}
