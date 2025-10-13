using Microsoft.Extensions.DependencyInjection;
using myCareers.Application.Interfaces;
using myCareers.Application.Mappings;
using myCareers.Application.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace myCareers.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Register application services
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IPasswordResetService, PasswordResetService>();

            services.AddAutoMapper(config =>
            {
                config.AddProfile<MappingProfile>();
            });
            return services;
        }
    }
}
