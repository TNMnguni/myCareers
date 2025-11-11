using Microsoft.Extensions.DependencyInjection;
using myCareers.Application.Interfaces;
using myCareers.Application.Mappings;
using myCareers.Application.Services;


namespace myCareers.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Register application services
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IPasswordResetService, PasswordResetService>();
            services.AddScoped<IJobPostingService, JobPostingService>();
            services.AddScoped<IJobApplicationService, JobApplicationService>();


            services.AddAutoMapper(config =>
            {
                config.AddProfile<MappingProfile>();
            });
            return services;
        }
    }
}
