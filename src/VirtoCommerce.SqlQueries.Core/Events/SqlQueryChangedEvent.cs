using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.SqlQueries.Core.Models;

namespace VirtoCommerce.SqlQueries.Core.Events;

public class SqlQueryChangedEvent : GenericChangedEntryEvent<SqlQuery>
{
    public SqlQueryChangedEvent(IEnumerable<GenericChangedEntry<SqlQuery>> changedEntries) : base(changedEntries)
    {
    }
}
