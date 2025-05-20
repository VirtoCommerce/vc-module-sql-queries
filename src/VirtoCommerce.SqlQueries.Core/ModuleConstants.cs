using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.SqlQueries.Core;

public static class ModuleConstants
{
    public static class Security
    {
        public static class Permissions
        {
            public const string Access = "sql-queries:access";
            public const string Create = "sql-queries:create";
            public const string Read = "sql-queries:read";
            public const string Update = "sql-queries:update";
            public const string Delete = "sql-queries:delete";

            public static string[] AllPermissions { get; } =
            [
                Access,
                Create,
                Read,
                Update,
                Delete,
            ];
        }
    }
}
