using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImgApiForNg.Migrations
{
    /// <inheritdoc />
    public partial class StrAd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "picturebyte",
                table: "Persons");

            migrationBuilder.AddColumn<string>(
                name: "picturestring",
                table: "Persons",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "picturestring",
                table: "Persons");

            migrationBuilder.AddColumn<byte[]>(
                name: "picturebyte",
                table: "Persons",
                type: "varbinary(max)",
                nullable: true);
        }
    }
}
