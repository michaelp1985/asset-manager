using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetContentHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "Assets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assets_ContentHash",
                table: "Assets",
                column: "ContentHash",
                unique: true,
                filter: "\"ContentHash\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Assets_ContentHash",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "Assets");
        }
    }
}
