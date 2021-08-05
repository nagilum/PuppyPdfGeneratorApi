using Microsoft.EntityFrameworkCore;
using PuppyPdfGenerator.Database.Tables;
using PuppyPdfGenerator.Tools;

namespace PuppyPdfGenerator.Database
{
    public class DatabaseContext : DbContext
    {
        /// <summary>
        /// Setup database config.
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                $"Data Source={Config.Get("database", "dataSource")};" +
                $"Initial Catalog={Config.Get("database", "initialCatalog")};" +
                $"User ID={Config.Get("database", "userId")};" +
                $"Password={Config.Get("database", "password")};");
        }

        #region DbSets

        public DbSet<Job> Jobs { get; set; }

        public DbSet<JobError> JobErrors { get; set; }

        #endregion
    }
}