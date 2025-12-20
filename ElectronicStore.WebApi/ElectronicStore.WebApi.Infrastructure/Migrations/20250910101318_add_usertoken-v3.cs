using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicStore.WebApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class add_usertokenv3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "codeRefreshTooken",
                table: "UserTokens",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "codeRefreshTooken",
                table: "UserTokens");
        }
    }
}
