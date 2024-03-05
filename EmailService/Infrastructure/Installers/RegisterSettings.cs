using EmailService.Contracts;
using EmailService.Infrastructure.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmailService.Infrastructure.Installers
{
    internal class RegisterSettings : IServiceRegistration
    {
        public void RegisterAppServices(IServiceCollection services, IConfiguration config)
        {
            services.Configure<MailServerConfig>(config.GetSection("MailServer"));
            services.Configure<AuthKeys>(config.GetSection("AuthKeys"));
        }
    }
}
