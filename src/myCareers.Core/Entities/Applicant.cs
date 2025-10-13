using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myCareers.Core.Entities
{
    public class Applicant
    {
        public int Id { get; set; }

        public int UserId { get; set; }

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

        // Navigation properties
        public User User { get; set; } = null!;
    }
}
