using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImgApiForNg.Migrations
{
    /// <inheritdoc />
    public partial class SwitchAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FileUrl",
                table: "ChunkedFileRecords",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<byte[]>(
                name: "FileData",
                table: "ChunkedFileRecords",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousStorageType",
                table: "ChunkedFileRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StorageSwitchedAt",
                table: "ChunkedFileRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageType",
                table: "ChunkedFileRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileData",
                table: "ChunkedFileRecords");

            migrationBuilder.DropColumn(
                name: "PreviousStorageType",
                table: "ChunkedFileRecords");

            migrationBuilder.DropColumn(
                name: "StorageSwitchedAt",
                table: "ChunkedFileRecords");

            migrationBuilder.DropColumn(
                name: "StorageType",
                table: "ChunkedFileRecords");

            migrationBuilder.AlterColumn<string>(
                name: "FileUrl",
                table: "ChunkedFileRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
