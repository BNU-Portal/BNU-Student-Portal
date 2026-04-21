using BNU_Student_Portal_Domain.Entities;
using BNU_Student_Portal_Domain.Interfaces;
using BNU_Student_Portal_Persistence.Data.DbContext;

namespace BNU_Student_Portal_Persistence.Repositories
{
    public class UnitOfWork(BNU_Student_Portal_DbContext context) : IUnitOfWork
    {

        private readonly Dictionary<Type, object> repositories = [];
        public IGenericRepository<TEntity, TKey> GetRepository<TEntity, TKey>() where TEntity : BaseEntity<TKey>
        {

            //check the dictionary if the repository for the given entity type already exists, if it does return it,
            //otherwise create a new one and add it to the dictionary before returning it
            var EntityType = typeof(TEntity);
            if (repositories.TryGetValue(EntityType, out object? repository))
                return (IGenericRepository<TEntity, TKey>)repository;


            var newRepository = new GenericRepository<TEntity, TKey>(context);
            repositories.Add(EntityType, newRepository);
            return newRepository;

        }




        public async Task<int> SaveChangesAsync()
           => await context.SaveChangesAsync();


    }
}
