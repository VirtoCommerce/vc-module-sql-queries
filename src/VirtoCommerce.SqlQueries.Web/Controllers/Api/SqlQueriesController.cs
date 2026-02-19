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
public class SqlQueriesController(
    ISqlQueryService sqlQueryService,
    ISqlQuerySearchService sqlQuerySearchService,
    IAuthorizationService authorizationService) : Controller
{
    [HttpGet]
    [Route("{id}")]
    [Authorize(Permissions.Read)]
    public async Task<ActionResult<SqlQuery>> GetById([FromRoute] string id)
    {
        var result = await sqlQueryService.GetByIdAsync(id);

        if (!await HasEditPermission())
        {
            result.Query = null;
        }

        return Ok(result);
    }

    [HttpPost]
    [Route("search")]
    [Authorize(Permissions.Read)]
    public async Task<ActionResult<SqlQuerySearchResult>> Search([FromBody] SqlQuerySearchCriteria criteria)
    {
        var result = await sqlQuerySearchService.SearchAsync(criteria);

        if (!await HasEditPermission())
        {
            foreach (var item in result.Results)
            {
                item.Query = null;
            }
        }

        return Ok(result);
    }

    [HttpPost]
    [Route("")]
    [Authorize(Permissions.Create)]
    public async Task<SqlQuery> Create([FromBody] SqlQuery query)
    {
        await sqlQueryService.SaveChangesAsync([query]);
        return query;
    }

    [HttpPut]
    [Route("")]
    [Authorize(Permissions.Update)]
    public async Task<SqlQuery> Update([FromBody] SqlQuery query)
    {
        await sqlQueryService.SaveChangesAsync([query]);
        return query;
    }

    [HttpDelete]
    [Route("")]
    [Authorize(Permissions.Delete)]
    public async Task<ActionResult> Delete([FromQuery] string[] ids)
    {
        await sqlQueryService.DeleteAsync(ids);
        return NoContent();
    }

    [HttpPost]
    [Route("reports")]
    [Authorize(Permissions.Read)]
    public async Task<ActionResult<SqlQuerySearchResult>> OnlyReports([FromBody] SqlQuerySearchCriteria criteria)
    {
        var result = await sqlQuerySearchService.SearchAsync(criteria);

        foreach (var item in result.Results)
        {
            item.Query = null;
        }

        return Ok(result);
    }

    [HttpPost]
    [Route("execute/{id}/{format}")]
    [Authorize(Permissions.Read)]
    public async Task<ActionResult<SqlQuerySearchResult>> ExecuteReport([FromRoute] string id, [FromRoute] string format, [FromBody] IList<SqlQueryParameter> sqlQueryParameters)
    {
        var query = await sqlQueryService.GetByIdAsync(id);
        var context = new SqlQueryReportContext
        {
            Name = query.Name,
            Description = query.Description,
            UserName = User.Identity?.Name,
        };
        var report = await sqlQueryService.GenerateReport(query, sqlQueryParameters, format, context);
        var fileName = $"{query.Name}_{DateTime.UtcNow:yyyy-MM-dd}.{format}";
        return File(report.Content, report.ContentType, fileName);
    }

    [HttpPost]
    [Route("execute-preview")]
    [Authorize(Permissions.Create)]
    [Authorize(Permissions.Update)]
    public async Task<ActionResult<SqlQueryExecuteResult>> ExecuteQuery([FromBody] SqlQueryPreviewRequest request)
    {
        var result = await sqlQueryService.ExecuteQuery(request);
        return Ok(result);
    }

    [HttpPost]
    [Route("execute-query/{id}")]
    [Authorize(Permissions.Read)]
    public async Task<ActionResult<SqlQueryExecuteResult>> ExecuteQueryById([FromRoute] string id, [FromBody] SqlQueryExecuteRequest request)
    {
        var query = await sqlQueryService.GetByIdAsync(id);

        if (query == null)
        {
            return NotFound();
        }

        var executeRequest = new SqlQueryPreviewRequest
        {
            Query = query.Query,
            ConnectionStringName = query.ConnectionStringName,
            Parameters = request.Parameters,
            MaxRows = request.MaxRows,
        };

        var result = await sqlQueryService.ExecuteQuery(executeRequest);
        return Ok(result);
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

    private async Task<bool> HasEditPermission()
    {
        return (await authorizationService.AuthorizeAsync(User, Permissions.Create)).Succeeded
            || (await authorizationService.AuthorizeAsync(User, Permissions.Update)).Succeeded;
    }
}
