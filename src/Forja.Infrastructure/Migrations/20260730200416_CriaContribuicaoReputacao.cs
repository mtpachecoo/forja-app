using System;
using Forja.Domain.Contribuicao;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forja.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CriaContribuicaoReputacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Só as chaves "Npgsql:Enum:nome" (sem ponto) — a variante com ponto ("Npgsql:Enum:nome.nome")
            // é um artefato de snapshot que o gerador de SQL interpreta como "criar um schema chamado
            // nome contendo um tipo nome.nome", gerando CREATE SCHEMA espúrio. Confirmado contra o Neon
            // real: a variante com ponto para os enums novos (status_contribuicao/tipo_contribuicao)
            // criou dois schemas indevidos que precisaram ser limpos manualmente.
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:nivel_usuario", "avancado,iniciante,intermediario")
                .Annotation("Npgsql:Enum:origem_questao", "gerada_ia,inedita,reproduzida_prova_oficial")
                .Annotation("Npgsql:Enum:status_contribuicao", "aprovada,em_revisao,rejeitada")
                .Annotation("Npgsql:Enum:status_item_plano", "concluido,pendente")
                .Annotation("Npgsql:Enum:status_questao", "aprovada,em_revisao,rascunho,rejeitada")
                .Annotation("Npgsql:Enum:tipo_contribuicao", "pdf,video")
                .Annotation("Npgsql:Enum:tipo_fonte", "edital,lei,prova")
                .Annotation("Npgsql:Enum:tipo_questao", "certo_errado,multipla_escolha")
                .OldAnnotation("Npgsql:Enum:nivel_usuario", "avancado,iniciante,intermediario")
                .OldAnnotation("Npgsql:Enum:origem_questao", "gerada_ia,inedita,reproduzida_prova_oficial")
                .OldAnnotation("Npgsql:Enum:status_item_plano", "concluido,pendente")
                .OldAnnotation("Npgsql:Enum:status_questao", "aprovada,em_revisao,rascunho,rejeitada")
                .OldAnnotation("Npgsql:Enum:tipo_fonte", "edital,lei,prova")
                .OldAnnotation("Npgsql:Enum:tipo_questao", "certo_errado,multipla_escolha");

            migrationBuilder.CreateTable(
                name: "contribuicoes_conteudo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    topico_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<TipoContribuicao>(type: "tipo_contribuicao", nullable: false),
                    link = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<StatusContribuicao>(type: "status_contribuicao", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    moderado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    moderado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contribuicoes_conteudo", x => x.id);
                    table.ForeignKey(
                        name: "fk_contribuicoes_conteudo_topicos_topico_id",
                        column: x => x.topico_id,
                        principalTable: "topicos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_contribuicoes_conteudo_usuarios_moderado_por",
                        column: x => x.moderado_por,
                        principalTable: "usuarios",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_contribuicoes_conteudo_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reputacao_contribuicao",
                columns: table => new
                {
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pontos_contribuicao = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reputacao_contribuicao", x => x.usuario_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contribuicoes_conteudo_moderado_por",
                table: "contribuicoes_conteudo",
                column: "moderado_por");

            migrationBuilder.CreateIndex(
                name: "ix_contribuicoes_conteudo_topico_id",
                table: "contribuicoes_conteudo",
                column: "topico_id");

            migrationBuilder.CreateIndex(
                name: "ix_contribuicoes_conteudo_usuario_id",
                table: "contribuicoes_conteudo",
                column: "usuario_id");

            // Medalha de marco concedida na primeira contribuição aprovada de cada usuário — dado de
            // referência, não schema (ver Forja.Domain.Gamificacao.MedalhasConhecidas para o Guid fixo
            // compartilhado com ContribuicaoService).
            migrationBuilder.InsertData(
                table: "medalhas",
                columns: ["id", "nome", "criterio"],
                values: new object[] { new Guid("9f9c9b0e-0f3d-4a3b-9b0e-0f3d4a3b9b0e"), "Primeira Contribuição Aprovada", "Ter uma contribuição de conteúdo aprovada por um moderador." });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "medalhas",
                keyColumn: "id",
                keyValue: new Guid("9f9c9b0e-0f3d-4a3b-9b0e-0f3d4a3b9b0e"));

            migrationBuilder.DropTable(
                name: "contribuicoes_conteudo");

            migrationBuilder.DropTable(
                name: "reputacao_contribuicao");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:nivel_usuario", "avancado,iniciante,intermediario")
                .Annotation("Npgsql:Enum:origem_questao", "gerada_ia,inedita,reproduzida_prova_oficial")
                .Annotation("Npgsql:Enum:status_item_plano", "concluido,pendente")
                .Annotation("Npgsql:Enum:status_questao", "aprovada,em_revisao,rascunho,rejeitada")
                .Annotation("Npgsql:Enum:tipo_fonte", "edital,lei,prova")
                .Annotation("Npgsql:Enum:tipo_questao", "certo_errado,multipla_escolha")
                .OldAnnotation("Npgsql:Enum:nivel_usuario", "avancado,iniciante,intermediario")
                .OldAnnotation("Npgsql:Enum:origem_questao", "gerada_ia,inedita,reproduzida_prova_oficial")
                .OldAnnotation("Npgsql:Enum:status_contribuicao", "aprovada,em_revisao,rejeitada")
                .OldAnnotation("Npgsql:Enum:status_item_plano", "concluido,pendente")
                .OldAnnotation("Npgsql:Enum:status_questao", "aprovada,em_revisao,rascunho,rejeitada")
                .OldAnnotation("Npgsql:Enum:tipo_contribuicao", "pdf,video")
                .OldAnnotation("Npgsql:Enum:tipo_fonte", "edital,lei,prova")
                .OldAnnotation("Npgsql:Enum:tipo_questao", "certo_errado,multipla_escolha");
        }
    }
}
