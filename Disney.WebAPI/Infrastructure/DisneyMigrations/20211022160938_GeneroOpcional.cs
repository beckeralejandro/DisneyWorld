using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Disney.WebAPI.Infrastructure.DisneyMigrations
{
    public partial class GeneroOpcional : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pelicula_Genero_GeneroId",
                table: "Pelicula");

            migrationBuilder.AlterColumn<int>(
                name: "GeneroId",
                table: "Pelicula",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Pelicula_Genero_GeneroId",
                table: "Pelicula",
                column: "GeneroId",
                principalTable: "Genero",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pelicula_Genero_GeneroId",
                table: "Pelicula");

            migrationBuilder.AlterColumn<int>(
                name: "GeneroId",
                table: "Pelicula",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Pelicula_Genero_GeneroId",
                table: "Pelicula",
                column: "GeneroId",
                principalTable: "Genero",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
