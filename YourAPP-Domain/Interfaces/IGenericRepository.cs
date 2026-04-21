using System;
using System.Collections.Generic;
using System.Text;
using YourAPP_Domain.Entities;

namespace YourAPP_Domain.Interfaces
{
    public interface IGenericRepository<TEntity,T> where TEntity : BaseEntity<T>
    {
        Task<IEnumerable<TEntity>> GetAllAsync();


        //Task<IEnumerable<TEntity>> GetAllAsync(ISpecifications<TEntity, Tkey> specifications);

        //to get it with the specifications and the specifications will be used to filter the data and
        //return only the data that we need and also to include the related data if we need it and also to order the data
        //if we need it and also to paginate the data if we need it
        //Task<int> CountAsync(ISpecifications<TEntity, Tkey> specifications);

        //Task<TEntity> GetByIdAsync(ISpecifications<TEntity, Tkey> specifications);


        Task<TEntity> GetByIdAsync(T id);

        Task AddAsync(TEntity entity);

        void Remove(TEntity entity);

        void Update(TEntity entity);
    }
}
