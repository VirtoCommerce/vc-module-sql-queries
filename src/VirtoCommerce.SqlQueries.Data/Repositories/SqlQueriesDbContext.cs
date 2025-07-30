using System.Reflection;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.Platform.Data.Infrastructure;
using VirtoCommerce.SqlQueries.Data.Models;

namespace VirtoCommerce.SqlQueries.Data.Repositories;

public class SqlQueriesDbContext : DbContextBase
{
    public SqlQueriesDbContext(DbContextOptions<SqlQueriesDbContext> options)
        : base(options)
    {
    }

    protected SqlQueriesDbContext(DbContextOptions options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SqlQueryEntity>().ToTable("SqlQuery").HasKey(x => x.Id);
        modelBuilder.Entity<SqlQueryEntity>().Property(x => x.Id).HasMaxLength(IdLength).ValueGeneratedOnAdd();

        modelBuilder.Entity<SqlQueryParameterEntity>().ToTable("SqlQueryParameter").HasKey(x => x.Id);
        modelBuilder.Entity<SqlQueryParameterEntity>().Property(x => x.Id).HasMaxLength(IdLength).ValueGeneratedOnAdd();
        modelBuilder.Entity<SqlQueryParameterEntity>().HasOne(x => x.SqlQuery).WithMany(x => x.Parameters)
                    .HasForeignKey(x => x.SqlQueryId).IsRequired().OnDelete(DeleteBehavior.Cascade);

        switch (Database.ProviderName)
        {
            case "Pomelo.EntityFrameworkCore.MySql":
                modelBuilder.ApplyConfigurationsFromAssembly(Assembly.Load("VirtoCommerce.SqlQueries.Data.MySql"));
                break;
            case "Npgsql.EntityFrameworkCore.PostgreSQL":
                modelBuilder.ApplyConfigurationsFromAssembly(Assembly.Load("VirtoCommerce.SqlQueries.Data.PostgreSql"));
                break;
            case "Microsoft.EntityFrameworkCore.SqlServer":
                modelBuilder.ApplyConfigurationsFromAssembly(Assembly.Load("VirtoCommerce.SqlQueries.Data.SqlServer"));
                break;
        }
    }
}
