using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstateEase.Migrations
{
    /// <inheritdoc />
    public partial class addpropertyEnhanced : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdditionalImagePaths",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "Features",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "Floor",
                table: "Properties");

            migrationBuilder.RenameColumn(
                name: "MainImagePath",
                table: "Properties",
                newName: "PropertyImagePaths");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PropertyImagePaths",
                table: "Properties",
                newName: "MainImagePath");

            migrationBuilder.AddColumn<string>(
                name: "AdditionalImagePaths",
                table: "Properties",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Features",
                table: "Properties",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Floor",
                table: "Properties",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
