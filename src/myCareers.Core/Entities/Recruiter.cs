using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myCareers.Core.Entities
{
    public class Recruiter
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        [MaxLength(200)]
        public string? Department { get; set; }

        [MaxLength(100)]
        public string? JobTitle { get; set; }

        [MaxLength(20)]
        public string? EmployeeId { get; set; }

        public DateTime? StartDate { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
    }
}
