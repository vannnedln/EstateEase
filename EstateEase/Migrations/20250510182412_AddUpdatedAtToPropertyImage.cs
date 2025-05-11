﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstateEase.Migrations
{
    /// <inheritdoc />
    public partial class AddUpdatedAtToPropertyImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "PropertyImages",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "PropertyImages");
        }
    }
}
