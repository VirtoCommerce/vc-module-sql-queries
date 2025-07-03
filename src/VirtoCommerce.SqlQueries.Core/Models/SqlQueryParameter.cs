using System;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.SqlQueries.Core.Models;
public class SqlQueryParameter : Entity, ICloneable
{
    public string Name { get; set; }
    public object Value { get; set; }
    public string Type { get; set; }
    public object Clone()
    {
        var result = MemberwiseClone() as SqlQueryParameter;
        return result;
    }
}
