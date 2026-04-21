using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace BNU_Student_Portal_Persistence.Data.DbContext
{
    public class BNU_Student_Portal_DbContext(DbContextOptions<BNU_Student_Portal_DbContext> options) : Microsoft.EntityFrameworkCore.DbContext(options)
    {


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Additional model configurations can be added here
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(BNU_Student_Portal_DbContext).Assembly);

        }
    }
}
