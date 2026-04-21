using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using YourAPP_Domain.Entities;
using YourAPP_Domain.Interfaces;
using YourAPP_Persistence.Data.DbContext;

namespace YourAPP_Persistence.Repositories
{
    public class GenericRepository<TEntity, TKey>(YourAPPDbContext context) : IGenericRepository<TEntity, TKey> where TEntity : BaseEntity<TKey>
    {
        public async Task<IEnumerable<TEntity>> GetAllAsync()
        {

            //bad Practice

            //if (condition != null)
            //{
            //    return await context.Set<TEntity>().ToListAsync();
            //}
            //if (Includes != null)
            //{
            //    IQueryable<TEntity> EntryPoint = context.Set<TEntity>();
            //    foreach (var includeExp in Includes)
            //    {
            //        EntryPoint =  EntryPoint.Include(includeExp);
            //    }
            //    return await EntryPoint.ToListAsync();
            //}

            return await context.Set<TEntity>().ToListAsync();

        }

        public async Task<TEntity> GetByIdAsync(TKey id)
            => await context.Set<TEntity>().FindAsync(id);

        //public async Task<TEntity?> GetByIdAsync(ISpecifications<TEntity, Tkey> specifications)
        //{
        //    return await SpecificationsEvaluator.CreateQuery(context.Set<TEntity>().AsQueryable(), specifications).FirstOrDefaultAsync();
        //}


        public async Task AddAsync(TEntity entity)
            => await context.Set<TEntity>().AddAsync(entity);


        public void Remove(TEntity entity)
        => context.Set<TEntity>().Remove(entity);

        public void Update(TEntity entity)
        => context.Set<TEntity>().Update(entity);

        //public async Task<IEnumerable<TEntity>> GetAllAsync(ISpecifications<TEntity, Tkey> specifications)
        //{

        //    //bad Practice ==> bec of the fact that we are doing the same thing in the service layer and here in the repository layer which is to check
        //    //if there are any include expressions or not and then we will do the same thing in both cases which is to return a
        //    //list of entities but with different ways of doing it , so we create the evaluator class to do this work for us and we will pass the specifications
        //    //to the evaluator class and it will return the list of entities for us without the need to check anything in the repository layer

        //    //IQueryable<TEntity> query = context.Set<TEntity>();
        //    //if (specifications.IncludeEcplressions != null && specifications.IncludeEcplressions.Any())
        //    //{
        //    //    //IQueryable<TEntity> EntryPoint = context.Set<TEntity>();
        //    //    foreach (var includeExp in specifications.IncludeEcplressions)
        //    //    {
        //    //        query = query.Include(includeExp);
        //    //    }
        //    //    return await query.ToListAsync();
        //    //}
        //    //return await query.ToListAsync();




        //    var Query = SpecificationsEvaluator.CreateQuery(context.Set<TEntity>().AsQueryable(), specifications);

        //    return await Query.ToListAsync();

        //}

        //public async Task<int> CountAsync(ISpecifications<TEntity, Tkey> specifications)
        //{
        //    return await SpecificationsEvaluator.CreateQuery(context.Set<TEntity>().AsQueryable(), specifications).CountAsync();
        //}
    }

}

