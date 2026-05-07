using System;
using System.IO;
using System.Threading.Tasks;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.ExportImport;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Data.MySql.Extensions;
using VirtoCommerce.Platform.Data.PostgreSql.Extensions;
using VirtoCommerce.Platform.Data.SqlServer.Extensions;
using VirtoCommerce.SqlQueries.Core;
using VirtoCommerce.SqlQueries.Core.Services;
using VirtoCommerce.SqlQueries.Data.ExportImport;
using VirtoCommerce.SqlQueries.Data.MySql;
using VirtoCommerce.SqlQueries.Data.PostgreSql;
using VirtoCommerce.SqlQueries.Data.Repositories;
using VirtoCommerce.SqlQueries.Data.Services;
using VirtoCommerce.SqlQueries.Data.SqlServer;

namespace VirtoCommerce.SqlQueries.Web;

public class Module : IModule, IExportSupport, IImportSupport, IHasConfiguration
{
    private IApplicationBuilder _appBuilder;

    public ManifestModuleInfo ModuleInfo { get; set; }
    public IConfiguration Configuration { get; set; }

    public void Initialize(IServiceCollection serviceCollection)
    {
        serviceCollection.AddDbContext<SqlQueriesDbContext>(options =>
        {
            var databaseProvider = Configuration.GetValue("DatabaseProvider", "SqlServer");
            var connectionString = Configuration.GetConnectionString(ModuleInfo.Id) ?? Configuration.GetConnectionString("VirtoCommerce");

            switch (databaseProvider)
            {
                case "MySql":
                    options.UseMySqlDatabase(connectionString, typeof(MySqlDataAssemblyMarker), Configuration);
                    break;
                case "PostgreSql":
                    options.UsePostgreSqlDatabase(connectionString, typeof(PostgreSqlDataAssemblyMarker), Configuration);
                    break;
                default:
                    options.UseSqlServerDatabase(connectionString, typeof(SqlServerDataAssemblyMarker), Configuration);
                    break;
            }
        });

        serviceCollection.AddTransient<ISqlQueriesRepository, SqlQueriesRepository>();
        serviceCollection.AddTransient<Func<ISqlQueriesRepository>>(provider => () => provider.CreateScope().ServiceProvider.GetRequiredService<ISqlQueriesRepository>());

        serviceCollection.AddTransient<ISqlQueryService, SqlQueryService>();
        serviceCollection.AddTransient<ISqlQuerySearchService, SqlQuerySearchService>();

        serviceCollection.AddTransient<ISqlQueryReportGenerator, PdfSqlQueryReportGenerator>();
        serviceCollection.AddTransient<ISqlQueryReportGenerator, CsvSqlQueryReportGenerator>();
        serviceCollection.AddTransient<IHtmlSqlQueryReportGenerator, HtmlSqlQueryReportGenerator>();
        serviceCollection.AddTransient<ISqlQueryReportGenerator, HtmlSqlQueryReportGenerator>();
        serviceCollection.AddTransient<ISqlQueryReportGenerator, XlsxSqlQueryReportGenerator>();

        serviceCollection.AddTransient<SqlQueriesExportImport>();

        serviceCollection.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));
    }

    public void PostInitialize(IApplicationBuilder appBuilder)
    {
        _appBuilder = appBuilder;
        var serviceProvider = appBuilder.ApplicationServices;

        // Register permissions
        var permissionsRegistrar = serviceProvider.GetRequiredService<IPermissionsRegistrar>();
        permissionsRegistrar.RegisterPermissions(ModuleInfo.Id, "SqlQueries", ModuleConstants.Security.Permissions.AllPermissions);

        // Apply migrations
        using var serviceScope = serviceProvider.CreateScope();
        using var dbContext = serviceScope.ServiceProvider.GetRequiredService<SqlQueriesDbContext>();
        dbContext.Database.Migrate();
    }

    public void Uninstall()
    {
        // Nothing to do here
    }

    public Task ExportAsync(Stream outStream, ExportImportOptions options, Action<ExportImportProgressInfo> progressCallback,
        ICancellationToken cancellationToken)
    {
        return _appBuilder.ApplicationServices.GetRequiredService<SqlQueriesExportImport>().DoExportAsync(outStream,
            progressCallback, cancellationToken);
    }

    public Task ImportAsync(Stream inputStream, ExportImportOptions options, Action<ExportImportProgressInfo> progressCallback,
        ICancellationToken cancellationToken)
    {
        return _appBuilder.ApplicationServices.GetRequiredService<SqlQueriesExportImport>().DoImportAsync(inputStream,
            progressCallback, cancellationToken);
    }
}
