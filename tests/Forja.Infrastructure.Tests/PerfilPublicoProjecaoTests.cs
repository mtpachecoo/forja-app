using Forja.Domain.Usuarios;
using FluentAssertions;

namespace Forja.Infrastructure.Tests;

/// <summary>
/// Trava por reflexão contra vazamento de campo privado de <see cref="Usuario"/> pela projeção pública
/// <see cref="PerfilPublico"/>. O record é a única forma de um dado de <see cref="Usuario"/> chegar ao
/// perfil público — a allowlist abaixo precisa ser atualizada explicitamente sempre que um campo novo
/// for adicionado ao record, o que é exatamente a trava desejada: ninguém expõe um campo por hábito/
/// copy-paste sem parar pra decidir se é seguro.
/// </summary>
[TestClass]
public class PerfilPublicoProjecaoTests
{
    private static readonly string[] CamposSeguros =
    [
        nameof(PerfilPublico.UsuarioId),
        nameof(PerfilPublico.Nome),
        nameof(PerfilPublico.Badges),
        nameof(PerfilPublico.ContribuicoesAprovadas),
        nameof(PerfilPublico.NivelReputacao),
    ];

    [TestMethod]
    public void PerfilPublico_ExpoeSomenteOsCamposDaAllowlist()
    {
        var camposDoRecord = typeof(PerfilPublico).GetProperties().Select(p => p.Name);

        camposDoRecord.Should().BeEquivalentTo(CamposSeguros);
    }

    [TestMethod]
    public void PerfilPublico_NaoExpoeEmailNemQualquerOutroCampoDeUsuario()
    {
        var camposDoRecord = typeof(PerfilPublico).GetProperties().Select(p => p.Name).ToHashSet();
        var camposDeUsuarioAlemDoNome = typeof(Usuario).GetProperties()
            .Select(p => p.Name)
            .Where(nome => nome != nameof(Usuario.Nome) && nome != nameof(Usuario.Id));

        foreach (var campoPrivado in camposDeUsuarioAlemDoNome)
        {
            camposDoRecord.Should().NotContain(campoPrivado, $"'{campoPrivado}' é um campo privado de Usuario e não deve vazar em PerfilPublico");
        }
    }
}
