using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstateEase.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIsMainProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMain",
                table: "PropertyImages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMain",
                table: "PropertyImages",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
