using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using myCareers.Application.Interfaces;
using myCareers.Core.Enterfaces;
using myCareers.Infrastructure.Data;
using myCareers.Infrastructure.Repositories;
using myCareers.Infrastructure.Services;


namespace myCareers.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Database - Register DbContext
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            services.AddDbContext<myCareersDbContext>(options => options.UseSqlServer(connectionString,b => b.MigrationsAssembly(typeof(myCareersDbContext).Assembly.FullName)));

            // Repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IPasswordResetRepository, PasswordResetRepository>();

            // Services
            services.AddScoped<IJwtTokenService, JwtTokenService>();

            return services;
        }
    }
}
