using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StokTakip.Migrations
{
    /// <inheritdoc />
    public partial class DatabaseSon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KategoriAdı",
                table: "Kategoriler");

            migrationBuilder.RenameColumn(
                name: "ID",
                table: "Kategoriler",
                newName: "Id");

            migrationBuilder.AddColumn<string>(
                name: "Aciklama",
                table: "Kategoriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "KategoriAd",
                table: "Kategoriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_KategoriId",
                table: "Products",
                column: "KategoriId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Kategoriler_KategoriId",
                table: "Products",
                column: "KategoriId",
                principalTable: "Kategoriler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Kategoriler_KategoriId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Products_KategoriId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Aciklama",
                table: "Kategoriler");

            migrationBuilder.DropColumn(
                name: "KategoriAd",
                table: "Kategoriler");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Kategoriler",
                newName: "ID");

            migrationBuilder.AddColumn<string>(
                name: "KategoriAdı",
                table: "Kategoriler",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
