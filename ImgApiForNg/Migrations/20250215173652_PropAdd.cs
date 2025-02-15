using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImgApiForNg.Migrations
{
    /// <inheritdoc />
    public partial class PropAdd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Props",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    fileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    fileType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    fileSize = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    fileUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DownloadToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DownloadTokenExpiration = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Props", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Props");
        }
    }
}
