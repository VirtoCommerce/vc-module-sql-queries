using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using VirtoCommerce.SqlQueries.Data.Repositories;

namespace VirtoCommerce.SqlQueries.Data.SqlServer;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SqlQueriesDbContext>
{
    public SqlQueriesDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<SqlQueriesDbContext>();
        var connectionString = args.Length != 0 ? args[0] : "Server=(local);User=virto;Password=virto;Database=VirtoCommerce3;";

        builder.UseSqlServer(
            connectionString,
            options => options.MigrationsAssembly(typeof(SqlServerDataAssemblyMarker).Assembly.GetName().Name));

        return new SqlQueriesDbContext(builder.Options);
    }
}
