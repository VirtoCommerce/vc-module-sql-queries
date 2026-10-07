using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SqlQueries.Core;
using VirtoCommerce.SqlQueries.Core.Models;
using VirtoCommerce.SqlQueries.Core.Services;
using VirtoCommerce.SqlQueries.Data.Services;
using Xunit;

namespace VirtoCommerce.SqlQueries.Tests;

[Trait("Category", "Unit")]
public class SqlQueryServiceCommandTimeoutTests
{
    private const string ConnectionStringName = "SqlQueries.Test";
    private const string ReportFormat = "test";

    [Fact]
    public async Task ExecuteQuery_UsesConfiguredCommandTimeout()
    {
        var connection = new RecordingDbConnection();
        var service = CreateService(connection, configuredTimeout: 120);

        await service.ExecuteQuery(new SqlQueryPreviewRequest { Query = "SELECT 1", ConnectionStringName = ConnectionStringName });

        Assert.Equal(120, connection.LastCommand.CommandTimeout);
    }

    [Fact]
    public async Task GenerateReport_UsesConfiguredCommandTimeout()
    {
        var connection = new RecordingDbConnection();
        var service = CreateService(connection, configuredTimeout: 120);
        var query = new SqlQuery { Query = "SELECT 1", ConnectionStringName = ConnectionStringName };

        await service.GenerateReport(query, [], ReportFormat, new SqlQueryReportContext());

        Assert.Equal(120, connection.LastCommand.CommandTimeout);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task ExecuteQuery_MissingOrNonPositiveSetting_FallsBackToDefaultTimeout(int? configuredTimeout)
    {
        var connection = new RecordingDbConnection();
        var service = CreateService(connection, configuredTimeout);

        await service.ExecuteQuery(new SqlQueryPreviewRequest { Query = "SELECT 1", ConnectionStringName = ConnectionStringName });

        Assert.Equal(ModuleConstants.Settings.General.DefaultCommandTimeout, connection.LastCommand.CommandTimeout);
    }

    private static TestSqlQueryService CreateService(RecordingDbConnection connection, int? configuredTimeout)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                [$"ConnectionStrings:{ConnectionStringName}"] = "Server=fake",
            })
            .Build();

        return new TestSqlQueryService(connection, configuration, new FakeSettingsManager(configuredTimeout));
    }

    private sealed class TestSqlQueryService(DbConnection connection, IConfiguration configuration, ISettingsManager settingsManager)
        : SqlQueryService(null, null, null, [new FakeReportGenerator()], configuration, settingsManager)
    {
        protected override DbContext GetDbContext(string connectionStringName)
        {
            var options = new DbContextOptionsBuilder<DbContext>().UseSqlServer(connection).Options;
            return new DbContext(options);
        }
    }

    private sealed class FakeSettingsManager(int? commandTimeout) : ISettingsManager
    {
        public Task<ObjectSettingEntry> GetObjectSettingAsync(string name, string objectType = null, string objectId = null)
        {
            var descriptor = ModuleConstants.Settings.General.CommandTimeout;
            Assert.Equal(descriptor.Name, name);

            return Task.FromResult(new ObjectSettingEntry(descriptor) { Value = commandTimeout });
        }

        public async Task<IEnumerable<ObjectSettingEntry>> GetObjectSettingsAsync(IEnumerable<string> names, string objectType = null, string objectId = null)
        {
            var result = new List<ObjectSettingEntry>();
            foreach (var name in names)
            {
                result.Add(await GetObjectSettingAsync(name, objectType, objectId));
            }

            return result;
        }

        public Task SaveObjectSettingsAsync(IEnumerable<ObjectSettingEntry> objectSettings) => throw new NotSupportedException();

        public Task RemoveObjectSettingsAsync(IEnumerable<ObjectSettingEntry> objectSettings) => throw new NotSupportedException();

        public IEnumerable<SettingDescriptor> AllRegisteredSettings => ModuleConstants.Settings.AllSettings;

        public void RegisterSettings(IEnumerable<SettingDescriptor> settings, string moduleId = null) => throw new NotSupportedException();

        public void RegisterSettingsForType(IEnumerable<SettingDescriptor> settings, string typeName) => throw new NotSupportedException();

        public IEnumerable<SettingDescriptor> GetSettingsForType(string typeName) => throw new NotSupportedException();

        public IDictionary<string, string[]> GetSettingTypeAssignments() => throw new NotSupportedException();
    }

    private sealed class FakeReportGenerator : ISqlQueryReportGenerator
    {
        public string Format => ReportFormat;
        public string ContentType => "text/plain";

        public SqlQueryReport GenerateReport(DataTable table, SqlQueryReportContext context) => new() { ContentType = ContentType, Content = [] };
    }

    private sealed class RecordingDbConnection : DbConnection
    {
        private ConnectionState _state = ConnectionState.Closed;

        public RecordingDbCommand LastCommand { get; private set; }

        [AllowNull]
        public override string ConnectionString { get; set; } = "Server=fake";
        public override string Database => "fake";
        public override string DataSource => "fake";
        public override string ServerVersion => "1.0";
        public override ConnectionState State => _state;

        public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();
        public override void Open() => _state = ConnectionState.Open;
        public override void Close() => _state = ConnectionState.Closed;

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => new FakeDbTransaction(this, isolationLevel);

        protected override DbCommand CreateDbCommand()
        {
            LastCommand = new RecordingDbCommand { Connection = this };
            return LastCommand;
        }
    }

    private sealed class FakeDbTransaction(DbConnection connection, IsolationLevel isolationLevel) : DbTransaction
    {
        public override IsolationLevel IsolationLevel => isolationLevel;
        protected override DbConnection DbConnection => connection;

        public override void Commit()
        {
            // Nothing to commit: the fake connection holds no data.
        }

        public override void Rollback()
        {
            // Nothing to roll back: the fake connection holds no data.
        }
    }

    private sealed class RecordingDbCommand : DbCommand
    {
        // Mirrors the ADO.NET provider default, so an unset timeout is observable as 30.
        public override int CommandTimeout { get; set; } = 30;

        [AllowNull]
        public override string CommandText { get; set; } = string.Empty;
        public override CommandType CommandType { get; set; } = CommandType.Text;
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection DbConnection { get; set; }
        protected override DbParameterCollection DbParameterCollection => throw new NotSupportedException();
        protected override DbTransaction DbTransaction { get; set; }

        public override void Cancel()
        {
            // Nothing to cancel: the fake command completes synchronously.
        }

        public override int ExecuteNonQuery() => 0;
        public override object ExecuteScalar() => null;

        public override void Prepare()
        {
            // Nothing to prepare for the fake command.
        }

        protected override DbParameter CreateDbParameter() => throw new NotSupportedException();

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            var table = new DataTable();
            table.Columns.Add("Value", typeof(int));
            table.Rows.Add(1);
            return table.CreateDataReader();
        }
    }
}
