using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicStore.WebApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class add_usertokenv1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isActive",
                table: "UserTokens",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isActive",
                table: "UserTokens");
        }
    }
}
