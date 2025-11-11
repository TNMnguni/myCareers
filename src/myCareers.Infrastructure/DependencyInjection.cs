using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using myCareers.Application.Configuration;
using myCareers.Core.Interfaces;
using myCareers.Infrastructure.Data;
using myCareers.Infrastructure.Repositories;
using myCareers.Infrastructure.Services;



namespace myCareers.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Configuration
            services.Configure<FileStorageSettings>(options =>
   configuration.GetSection("FileStorage").Bind(options));

            // Database
            services.AddDbContext<myCareersDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(myCareersDbContext).Assembly.FullName)));

            // Repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IPasswordResetRepository, PasswordResetRepository>();
            services.AddScoped<IJobPostingRepository, JobPostingRepository>();
            services.AddScoped<IJobApplicationRepository, JobApplicationRepository>();
            services.AddScoped<IApplicationDocumentRepository, ApplicationDocumentRepository>();
            services.AddScoped<IRecruiterRepository, RecruiterRepository>(); //I added this line

            // Infrastructure Services
            services.AddScoped<Application.Interfaces.IJwtTokenService, JwtTokenService>();
            services.AddScoped<Application.Interfaces.IFileUploadService, FileUploadService>();
           

            return services;
        }
    }
}