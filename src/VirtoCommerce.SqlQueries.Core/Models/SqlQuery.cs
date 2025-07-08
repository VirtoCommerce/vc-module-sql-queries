using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.SqlQueries.Core.Models;

public class SqlQuery : AuditableEntity, ICloneable
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Query { get; set; }
    public string ConnectionStringName { get; set; }

    public ICollection<SqlQueryParameter> Parameters { get; set; } = new List<SqlQueryParameter>();

    public object Clone()
    {
        var result = MemberwiseClone() as SqlQuery;

        result.Parameters = Parameters?.Select(x => x.Clone()).OfType<SqlQueryParameter>().ToList();

        return result;
    }
}
