using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SqlQueries.Core.Models;
using VirtoCommerce.SqlQueries.Core.Services;
using Permissions = VirtoCommerce.SqlQueries.Core.ModuleConstants.Security.Permissions;

namespace VirtoCommerce.SqlQueries.Web.Controllers.Api;

[Authorize]
[Route("api/sql-queries")]
public class SqlQueriesController : Controller
{
    private readonly ISqlQueryService _sqlQueryService;
    private readonly ISqlQuerySearchService _sqlQuerySearchService;

    public SqlQueriesController(ISqlQueryService sqlQueryService, ISqlQuerySearchService sqlQuerySearchService)
    {
        _sqlQueryService = sqlQueryService;
        _sqlQuerySearchService = sqlQuerySearchService;
    }

    [HttpGet]
    [Route("{id}")]
    [Authorize(Permissions.Read)]
    public async Task<ActionResult<SqlQuery>> GetById([FromRoute] string id)
    {
        var result = await _sqlQueryService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [Route("search")]
    [Authorize(Permissions.Read)]
    public async Task<ActionResult<SqlQuerySearchResult>> Search([FromBody] SqlQuerySearchCriteria criteria)
    {
        var result = await _sqlQuerySearchService.SearchAsync(criteria);
        return Ok(result);
    }

    [HttpPost]
    [Route("")]
    [Authorize(Permissions.Create)]
    public async Task<ActionResult> Create([FromBody] SqlQuery query)
    {
        if (query == null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        await _sqlQueryService.SaveChangesAsync([query]);
        return NoContent();
    }

    [HttpPut]
    [Route("")]
    [Authorize(Permissions.Update)]
    public async Task<ActionResult<SqlQuery>> Update([FromBody] SqlQuery query)
    {
        if (query == null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        await _sqlQueryService.SaveChangesAsync([query]);
        return Ok(query);
    }

    [HttpDelete]
    [Route("")]
    [Authorize(Permissions.Update)]
    public async Task<ActionResult> Delete([FromQuery] string[] queryIds)
    {
        if (queryIds == null)
        {
            throw new ArgumentNullException(nameof(queryIds));
        }

        await _sqlQueryService.DeleteAsync(queryIds);
        return NoContent();
    }

    [HttpPost]
    [Route("reports")]
    [Authorize(Permissions.Reports)]
    public async Task<ActionResult<SqlQuerySearchResult>> OnlyReports([FromBody] SqlQuerySearchCriteria criteria)
    {
        var result = await _sqlQuerySearchService.SearchAsync(criteria);
        foreach (var item in result.Results)
        {
            item.Query = null;
        }
        return Ok(result);
    }

    [HttpPost]
    [Route("execute/{id}/{format}")]
    [Authorize(Permissions.Reports)]
    public async Task<ActionResult<SqlQuerySearchResult>> ExecuteReport([FromRoute] string format, [FromRoute] string id, [FromBody] IList<SqlQueryParameter> sqlQueryParameters)
    {
        var query = await _sqlQueryService.GetByIdAsync(id);
        FillParameters(query, sqlQueryParameters);
        var report = await _sqlQueryService.GenerateReport(query, format);
        var fileName = $"{query.Name}.{format}";
        return File(report.Content, report.ContentType, fileName);
    }

    [HttpGet]
    [Route("formats")]
    public ActionResult<IList<string>> GetReportFormats()
    {
        var formats = _sqlQueryService.GetFormats();
        return Ok(formats);
    }

    [HttpGet]
    [Route("database-information")]
    [Authorize(Permissions.Read)]
    public ActionResult<DatabaseInformation> GetDatabaseInformation()
    {
        var formats = _sqlQueryService.GetDatabaseInformation();
        return Ok(formats);
    }

    private static void FillParameters(SqlQuery query, IList<SqlQueryParameter> parameters)
    {
        if (query == null)
        {
            throw new ArgumentNullException(nameof(query));
        }
        if (parameters == null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        foreach (var parameter in parameters)
        {
            var existingParameter = query.Parameters.FirstOrDefault(p => p.Name.Equals(parameter.Name, StringComparison.OrdinalIgnoreCase));
            if (existingParameter != null)
            {
                existingParameter.Value = parameter.Value;
            }
        }
    }
}
