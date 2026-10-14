using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.SqlQueries.Data.MySql.Migrations
{
    /// <inheritdoc />
    public partial class AddSqlQueryParameterSortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "SqlQueryParameter",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "SqlQueryParameter");
        }
    }
}
