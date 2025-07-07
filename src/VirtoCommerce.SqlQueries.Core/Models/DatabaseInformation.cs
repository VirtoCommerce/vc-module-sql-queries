using System.Collections.Generic;

namespace VirtoCommerce.SqlQueries.Core.Models;
public class DatabaseInformation
{
    public string DatabaseProvider { get; set; }

    public IList<string> ConnectionStringNames { get; set; } = [];
}
