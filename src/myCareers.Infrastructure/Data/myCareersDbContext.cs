using Microsoft.EntityFrameworkCore;
using myCareers.Core.Entities;

namespace myCareers.Infrastructure.Data
{
    public class myCareersDbContext : DbContext
    {
        public myCareersDbContext(DbContextOptions<myCareersDbContext> options)
            : base(options)
        {
        }

        // DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<Applicant> Applicants { get; set; }
        public DbSet<Recruiter> Recruiters { get; set; }
        public DbSet<JobPosting> JobPostings { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }
        public DbSet<ApplicationDocument> ApplicationDocuments { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ========================================
            // Explicit Relationship Configuration
            // ========================================

            // User -> Applicant (One-to-One)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Applicant)
                .WithOne(a => a.User)
                .HasForeignKey<Applicant>(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> Recruiter (One-to-One)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Recruiter)
                .WithOne(r => r.User)
                .HasForeignKey<Recruiter>(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // JobPosting -> Recruiter (Many-to-One)
            modelBuilder.Entity<JobPosting>()
                .HasOne(jp => jp.Recruiter)
                .WithMany(r => r.JobPostings)
                .HasForeignKey(jp => jp.RecruiterId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete


            // JobApplication -> JobPosting (Many-to-One)
            // THIS IS THE CRITICAL FIX FOR YOUR ERROR
            modelBuilder.Entity<JobApplication>()
                .HasOne(ja => ja.JobPosting)
                .WithMany(jp => jp.Applications)
                .HasForeignKey(ja => ja.JobPostingId)
                .OnDelete(DeleteBehavior.Restrict) // Prevent cascade delete
                .IsRequired();

            // JobApplication -> Applicant (Many-to-One)
            modelBuilder.Entity<JobApplication>()
                .HasOne(ja => ja.Applicant)
                .WithMany(a => a.JobApplications)
                .HasForeignKey(ja => ja.ApplicantId)
                .OnDelete(DeleteBehavior.Restrict) // Prevent cascade delete
                .IsRequired();

            // ApplicationDocument -> JobApplication (Many-to-One, Optional)
            modelBuilder.Entity<ApplicationDocument>()
                .HasOne(ad => ad.JobApplication)
                .WithMany(ja => ja.Documents)
                .HasForeignKey(ad => ad.JobApplicationId)
                .OnDelete(DeleteBehavior.SetNull) // Set to null when application deleted
                .IsRequired(false);

            // ApplicationDocument -> Applicant (Many-to-One)
            modelBuilder.Entity<ApplicationDocument>()
                .HasOne(ad => ad.Applicant)
                .WithMany(a => a.Documents)
                .HasForeignKey(ad => ad.ApplicantId)
                .OnDelete(DeleteBehavior.Cascade) // Delete documents when applicant deleted
                .IsRequired();

            // PasswordResetToken -> User (Many-to-One)
            modelBuilder.Entity<PasswordResetToken>()
                .HasOne(prt => prt.User)
                .WithMany()
                .HasForeignKey(prt => prt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ========================================
            // Indexes for Performance
            // ========================================

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<JobPosting>()
                .HasIndex(jp => new { jp.RecruiterId, jp.IsActive });

            modelBuilder.Entity<JobApplication>()
                .HasIndex(ja => new { ja.ApplicantId, ja.JobPostingId })
                .IsUnique(); // Prevent duplicate applications

            modelBuilder.Entity<JobApplication>()
                .HasIndex(ja => ja.Status);

            modelBuilder.Entity<ApplicationDocument>()
                .HasIndex(ad => ad.ApplicantId);

            modelBuilder.Entity<ApplicationDocument>()
                .HasIndex(ad => ad.JobApplicationId);

            modelBuilder.Entity<PasswordResetToken>()
                .HasIndex(prt => prt.Token)
                .IsUnique();

            modelBuilder.Entity<PasswordResetToken>()
               .HasIndex(prt => prt.ExpiresAt);

            // ========================================
            // Value Conversions for Enums
            // ========================================

            modelBuilder.Entity<JobApplication>()
                .Property(ja => ja.Status)
                .HasConversion<int>();

            modelBuilder.Entity<JobApplication>()
                .Property(ja => ja.Race)
                .HasConversion<int>();

            modelBuilder.Entity<JobApplication>()
                .Property(ja => ja.Gender)
                .HasConversion<int>();

            // ========================================
            // Default Values
            // ========================================

            modelBuilder.Entity<JobApplication>()
                .Property(ja => ja.CreatedDate)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<ApplicationDocument>()
                .Property(ad => ad.CreatedOn)
                .HasDefaultValueSql("GETUTCDATE()");

      
        }
    }
}
