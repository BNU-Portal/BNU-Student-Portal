using Microsoft.Extensions.DependencyInjection;
using YourAPP_Domain.Interfaces;
using YourAPP_Persistence.Repositories;

namespace YourAPP_Persistence.DI
{
    public static class PersistenceServicesRegistration
    {
        public static IServiceCollection AddPersistenceServicesRegistration(this IServiceCollection services)
        {

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;

        }
    }
}
