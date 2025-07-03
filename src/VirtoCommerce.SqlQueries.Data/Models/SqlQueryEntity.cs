using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Domain;
using VirtoCommerce.SqlQueries.Core.Models;

namespace VirtoCommerce.SqlQueries.Data.Models;
public class SqlQueryEntity : AuditableEntity, IDataEntity<SqlQueryEntity, SqlQuery>
{
    [Required]
    public string Name { get; set; }

    public string Description { get; set; }

    [Required]
    public string Query { get; set; }

    public string ConnectionStringName { get; set; }

    public ObservableCollection<SqlQueryParameterEntity> Parameters { get; set; } =
            new NullCollection<SqlQueryParameterEntity>();

    public SqlQuery ToModel(SqlQuery model)
    {
        model.Id = Id;
        model.CreatedBy = CreatedBy;
        model.CreatedDate = CreatedDate;
        model.ModifiedBy = ModifiedBy;
        model.ModifiedDate = ModifiedDate;

        model.Name = Name;
        model.Query = Query;
        model.ConnectionStringName = ConnectionStringName;

        model.Parameters = Parameters.Select(x => x.ToModel(AbstractTypeFactory<SqlQueryParameter>.TryCreateInstance())).ToList();

        return model;
    }

    public SqlQueryEntity FromModel(SqlQuery model, PrimaryKeyResolvingMap pkMap)
    {
        pkMap.AddPair(model, this);

        Id = model.Id;
        CreatedBy = model.CreatedBy;
        CreatedDate = model.CreatedDate;
        ModifiedBy = model.ModifiedBy;
        ModifiedDate = model.ModifiedDate;

        Name = model.Name;
        Query = model.Query;
        ConnectionStringName = model.ConnectionStringName;

        if (model.Parameters != null)
        {
            Parameters = new ObservableCollection<SqlQueryParameterEntity>(model.Parameters.Select(x => AbstractTypeFactory<SqlQueryParameterEntity>.TryCreateInstance().FromModel(x, pkMap)));
        }

        return this;
    }

    public void Patch(SqlQueryEntity target)
    {
        target.Name = Name;
        target.Query = Query;
        target.ConnectionStringName = ConnectionStringName;

        if (!Parameters.IsNullCollection())
        {
            Parameters.Patch(target.Parameters, (sourceParameter, targetParameter) => { sourceParameter.Patch(targetParameter); });
        }
    }
}
