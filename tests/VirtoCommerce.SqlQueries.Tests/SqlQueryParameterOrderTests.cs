using System.Collections.ObjectModel;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SqlQueries.Core.Models;
using VirtoCommerce.SqlQueries.Data.Models;
using Xunit;

namespace VirtoCommerce.SqlQueries.Tests;

[Trait("Category", "Unit")]
public class SqlQueryParameterOrderTests
{
    private static SqlQuery Query(params string[] parameterIds) => new()
    {
        Id = "q1",
        Parameters = parameterIds.Select(id => new SqlQueryParameter { Id = id, Name = "name-" + id, Type = "ShortText" }).ToList(),
    };

    private static string[] ParameterIds(SqlQueryEntity entity) =>
        entity.ToModel(new SqlQuery()).Parameters.Select(x => x.Id).ToArray();

    [Fact]
    public void ToModel_ReturnsParametersInSavedOrder_RegardlessOfLoadOrder()
    {
        var entity = new SqlQueryEntity().FromModel(Query("c", "a", "b"), new PrimaryKeyResolvingMap());

        // the database returns rows in no particular order
        entity.Parameters = new ObservableCollection<SqlQueryParameterEntity>(entity.Parameters.OrderBy(x => x.Id));

        Assert.Equal(["c", "a", "b"], ParameterIds(entity));
    }

    [Fact]
    public void Patch_UpdatesOrderOfExistingParameters()
    {
        var stored = new SqlQueryEntity().FromModel(Query("a", "b", "c"), new PrimaryKeyResolvingMap());

        new SqlQueryEntity().FromModel(Query("c", "b"), new PrimaryKeyResolvingMap()).Patch(stored);
        stored.Parameters = new ObservableCollection<SqlQueryParameterEntity>(stored.Parameters.OrderBy(x => x.Id));

        Assert.Equal(["c", "b"], ParameterIds(stored));
    }
}
