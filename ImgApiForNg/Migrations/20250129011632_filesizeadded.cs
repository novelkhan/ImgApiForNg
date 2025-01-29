using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImgApiForNg.Migrations
{
    /// <inheritdoc />
    public partial class filesizeadded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "filesize",
                table: "Men",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "filesize",
                table: "Men");
        }
    }
}
