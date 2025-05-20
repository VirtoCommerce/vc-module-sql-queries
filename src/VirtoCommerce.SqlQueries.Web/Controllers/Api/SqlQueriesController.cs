using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Permissions = VirtoCommerce.SqlQueries.Core.ModuleConstants.Security.Permissions;

namespace VirtoCommerce.SqlQueries.Web.Controllers.Api;

[Authorize]
[Route("api/sql-queries")]
public class SqlQueriesController : Controller
{

}
