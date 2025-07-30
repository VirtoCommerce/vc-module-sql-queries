using System.ComponentModel.DataAnnotations;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Domain;
using VirtoCommerce.SqlQueries.Core.Models;
using static VirtoCommerce.Platform.Data.Infrastructure.DbContextBase;

namespace VirtoCommerce.SqlQueries.Data.Models;

public class SqlQueryParameterEntity : Entity, IDataEntity<SqlQueryParameterEntity, SqlQueryParameter>
{
    [Required]
    [StringLength(Length128)]
    public string Name { get; set; }

    [Required]
    [StringLength(Length64)]
    public string Type { get; set; }

    [Required]
    [StringLength(Length128)]
    public string SqlQueryId { get; set; }
    public virtual SqlQueryEntity SqlQuery { get; set; }

    public SqlQueryParameter ToModel(SqlQueryParameter model)
    {
        model.Id = Id;

        model.Name = Name;
        model.Type = Type;

        return model;
    }

    public SqlQueryParameterEntity FromModel(SqlQueryParameter model, PrimaryKeyResolvingMap pkMap)
    {
        Id = model.Id;

        Name = model.Name;
        Type = model.Type;

        return this;
    }

    public void Patch(SqlQueryParameterEntity target)
    {
        target.Name = Name;
        target.Type = Type;
    }
}
