using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APICatalogo.Migrations
{
    /// <inheritdoc />
    public partial class Populacategorias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("INSERT INTO Categorias(Nome, ImageURL) VALUES ('Bebidas','bebidas.jpg')");
            migrationBuilder.Sql("INSERT INTO Categorias(Nome, ImageURL) VALUES ('Lanches','lanches.jpg')");
            migrationBuilder.Sql("INSERT INTO Categorias(Nome, ImageURL) VALUES ('Sobremesas','sobremesas.jpg')");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM Categorias");
        }
    }
}
