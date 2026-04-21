using BNU_Student_Portal_Domain.Interfaces;
using BNU_Student_Portal_Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace BNU_Student_Portal_Persistence.DI
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
