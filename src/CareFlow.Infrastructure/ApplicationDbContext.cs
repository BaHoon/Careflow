using Microsoft.EntityFrameworkCore;
using CareFlow.Core.Models;

namespace CareFlow.Infrastructure
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // 定义实体表
        public DbSet<SoftDeleteEntity> SoftDeleteEntities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 全局软删除查询过滤器
            modelBuilder.Entity<SoftDeleteEntity>().HasQueryFilter(e => !e.IsDeleted);
        }
    }
}
