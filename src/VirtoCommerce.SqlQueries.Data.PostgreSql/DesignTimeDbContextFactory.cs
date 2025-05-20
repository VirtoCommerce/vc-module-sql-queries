using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using VirtoCommerce.SqlQueries.Data.Repositories;

namespace VirtoCommerce.SqlQueries.Data.PostgreSql;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SqlQueriesDbContext>
{
    public SqlQueriesDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<SqlQueriesDbContext>();
        var connectionString = args.Length != 0 ? args[0] : "Server=localhost;Username=virto;Password=virto;Database=VirtoCommerce3;";

        builder.UseNpgsql(
            connectionString,
            options => options.MigrationsAssembly(typeof(PostgreSqlDataAssemblyMarker).Assembly.GetName().Name));

        return new SqlQueriesDbContext(builder.Options);
    }
}
