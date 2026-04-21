using System;
using System.Collections.Generic;
using System.Text;
using YourAPP_Domain.Entities;

namespace YourAPP_Domain.Interfaces
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync();

        IGenericRepository<TEntity, TKey> GetRepository<TEntity, TKey>() where TEntity : BaseEntity<TKey>;
    }
}
