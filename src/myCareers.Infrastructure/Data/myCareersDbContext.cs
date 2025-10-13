using Microsoft.EntityFrameworkCore;
using myCareers.Core.Entities;
using myCareers.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myCareers.Infrastructure.Data
{
    public class myCareersDbContext : DbContext
    {
        public myCareersDbContext(DbContextOptions<myCareersDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Recruiter> Recruiters { get; set; }
        public DbSet<Applicant> Applicants{ get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.Role).HasConversion<string>();
            });

            // RecruiterProfile configuration
            modelBuilder.Entity<Recruiter>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.User)
                      .WithOne(u => u.Recruiter)
                      .HasForeignKey<Recruiter>(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.Department).HasMaxLength(200);
                entity.Property(e => e.JobTitle).HasMaxLength(100);
                entity.Property(e => e.EmployeeId).HasMaxLength(20);
            });

            // ApplicantProfile configuration
            modelBuilder.Entity<Applicant>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.User)
                      .WithOne(u => u.Applicant)
                      .HasForeignKey<Applicant>(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.IdNumber).HasMaxLength(20);
                entity.Property(e => e.Address).HasMaxLength(200);
                entity.Property(e => e.City).HasMaxLength(100);
                entity.Property(e => e.PostalCode).HasMaxLength(20);
                entity.Property(e => e.Nationality).HasMaxLength(100);
                entity.Property(e => e.Gender).HasMaxLength(50);
            });

            // Seed data
            SeedData(modelBuilder);
        }

        private static void SeedData(ModelBuilder modelBuilder)
        {
            // Seed admin user with static datetime
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Email = "admin@dirco.gov.za",
                    PasswordHash = "$2a$11$eH8xczGmS9ZW1mPJh.pnUuF5J8VzM2K9N7xH5rF4N8uP6Y2sL9vGC", // Pre-hashed "Admin@123"
                    FirstName = "System",
                    LastName = "Administrator",
                    Role = UserRole.Administrator,
                    IsActive = true,
                    IsEmailConfirmed = true,
                    CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) // Static date
                }
            );
        }
    }
}
