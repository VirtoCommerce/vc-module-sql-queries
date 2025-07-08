using System;
using System.Collections.Generic;
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
public class SqlQueriesController(ISqlQueryService sqlQueryService, ISqlQuerySearchService sqlQuerySearchService) : Controller
{
    [HttpGet]
    [Route("{id}")]
    [Authorize(Permissions.Read)]
    public async Task<ActionResult<SqlQuery>> GetById([FromRoute] string id)
    {
        var result = await sqlQueryService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [Route("search")]
    [Authorize(Permissions.Read)]
    public async Task<ActionResult<SqlQuerySearchResult>> Search([FromBody] SqlQuerySearchCriteria criteria)
    {
        var result = await sqlQuerySearchService.SearchAsync(criteria);
        return Ok(result);
    }

    [HttpPost]
    [Route("")]
    [Authorize(Permissions.Create)]
    public async Task<ActionResult> Create([FromBody] SqlQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        await sqlQueryService.SaveChangesAsync([query]);
        return NoContent();
    }

    [HttpPut]
    [Route("")]
    [Authorize(Permissions.Update)]
    public async Task<ActionResult<SqlQuery>> Update([FromBody] SqlQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        await sqlQueryService.SaveChangesAsync([query]);
        return Ok(query);
    }

    [HttpDelete]
    [Route("")]
    [Authorize(Permissions.Update)]
    public async Task<ActionResult> Delete([FromQuery] string[] queryIds)
    {
        ArgumentNullException.ThrowIfNull(queryIds);

        await sqlQueryService.DeleteAsync(queryIds);
        return NoContent();
    }

    [HttpPost]
    [Route("reports")]
    [Authorize(Permissions.Reports)]
    public async Task<ActionResult<SqlQuerySearchResult>> OnlyReports([FromBody] SqlQuerySearchCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var result = await sqlQuerySearchService.SearchAsync(criteria);

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
        var query = await sqlQueryService.GetByIdAsync(id);
        var report = await sqlQueryService.GenerateReport(format, query, sqlQueryParameters);
        var fileName = $"{query.Name}.{format}";
        return File(report.Content, report.ContentType, fileName);
    }

    [HttpGet]
    [Route("formats")]
    public ActionResult<IList<string>> GetReportFormats()
    {
        var formats = sqlQueryService.GetFormats();
        return Ok(formats);
    }

    [HttpGet]
    [Route("database-information")]
    [Authorize(Permissions.Read)]
    public ActionResult<DatabaseInformation> GetDatabaseInformation()
    {
        var formats = sqlQueryService.GetDatabaseInformation();
        return Ok(formats);
    }
}
