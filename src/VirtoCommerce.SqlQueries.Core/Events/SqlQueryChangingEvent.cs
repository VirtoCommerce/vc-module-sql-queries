using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.SqlQueries.Core.Models;

namespace VirtoCommerce.SqlQueries.Core.Events;
public class SqlQueryChangingEvent : GenericChangedEntryEvent<SqlQuery>
{
    public SqlQueryChangingEvent(IEnumerable<GenericChangedEntry<SqlQuery>> changedEntries) : base(changedEntries)
    {
    }
}
