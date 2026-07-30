using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forja.Infrastructure.Migrations
{
    /// <summary>
    /// Não altera nenhum índice de verdade — os três índices envolvidos já existem no Neon desde antes
    /// da adoção de EF Core Migrations (<c>idx_respostas_usuario_questao</c>,
    /// <c>revisao_espacada_usuario_id_questao_id_key</c>, <c>idx_revisao_pendente</c>), criados fora do
    /// fluxo de migrations, como <c>edital_peso_disciplina</c>. Nunca tinham sido mapeados no modelo
    /// EF, que só conhecia índices "fantasma" assumidos por convenção de FK
    /// (<c>ix_respostas_usuario_usuario_id</c>, <c>ix_revisao_espacada_usuario_id</c>) que também nunca
    /// foram criados de verdade — o <c>Up()</c> do <c>InicioBaseline</c> ficou vazio de propósito, então
    /// essa lacuna entre modelo e banco real ficou latente até agora. Up()/Down() vazios de propósito:
    /// só corrige o snapshot do EF, não recria nada.
    /// </summary>
    public partial class MapeiaIndicesCompostoUsuarioQuestaoExistentes : Migration
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
