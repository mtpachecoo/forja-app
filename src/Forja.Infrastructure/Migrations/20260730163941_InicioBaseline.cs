using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forja.Infrastructure.Migrations
{
    /// <summary>
    /// Baseline do schema existente no Postgres/Neon no momento da adoção do EF Core Migrations.
    /// Up/Down propositalmente vazios: o schema já existe (ver <c>schema-atual.sql</c>) — esta
    /// migration só registra o ponto de partida em <c>__EFMigrationsHistory</c>, sem recriar nada.
    /// </summary>
    public partial class InicioBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
