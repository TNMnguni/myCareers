using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using myCareers.Core.Entities;
using myCareers.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myCareers.Infrastructure.Data
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(myCareersDbContext context, ILogger logger)
        {
            try
            {
                // Check if admin exists
                var adminExists = await context.Users.AnyAsync(u => u.Email == "admin@dirco.gov.za");

                if (!adminExists)
                {
                    logger.LogInformation("Seeding admin user...");

                    var adminUser = new User
                    {
                        Email = "admin@dirco.gov.za",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                        FirstName = "Admin",
                        LastName = "DIRCO",
                        Role = UserRole.Administrator,
                        IsActive = true,
                        IsEmailConfirmed = true,
                        CreatedDate = DateTime.UtcNow
                    };

                    context.Users.Add(adminUser);
                    await context.SaveChangesAsync();

                    logger.LogInformation("Admin user seeded successfully with email: admin@dirco.gov.za");
                    logger.LogInformation("Admin password: Admin@123");
                }
                else
                {
                    logger.LogInformation("Admin user already exists.");

                    // Optionally reset admin password if needed (for development only)
                    var admin = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@dirco.gov.za");
                    if (admin != null)
                    {
                        logger.LogInformation("Admin user found. ID: {Id}, IsActive: {IsActive}", admin.Id, admin.IsActive);

                        // Uncomment the following lines to reset admin password
                        /*
                        admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
                        await context.SaveChangesAsync();
                        logger.LogInformation("Admin password has been reset to: Admin@123");
                        */
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }
    }
}
