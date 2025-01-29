using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImgApiForNg.Migrations
{
    /// <inheritdoc />
    public partial class manExtends : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "imagebytes",
                table: "Men",
                type: "varbinary(max)",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)");

            migrationBuilder.AddColumn<string>(
                name: "base64string",
                table: "Men",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "filetype",
                table: "Men",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "base64string",
                table: "Men");

            migrationBuilder.DropColumn(
                name: "filetype",
                table: "Men");

            migrationBuilder.AlterColumn<byte[]>(
                name: "imagebytes",
                table: "Men",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)",
                oldNullable: true);
        }
    }
}
