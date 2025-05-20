using System.Reflection;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.Platform.Data.Infrastructure;

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

        //modelBuilder.Entity<SqlQueriesEntity>().ToTable("SqlQueries").HasKey(x => x.Id);
        //modelBuilder.Entity<SqlQueriesEntity>().Property(x => x.Id).HasMaxLength(IdLength).ValueGeneratedOnAdd();

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
