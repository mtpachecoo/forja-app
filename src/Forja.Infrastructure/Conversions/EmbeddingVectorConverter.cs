using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Pgvector;

namespace Forja.Infrastructure.Conversions;

/// <summary>
/// Converte <see cref="Forja.Domain.Conteudo.ChunkConteudo.Embedding"/> (<see cref="float"/>[]) de/para
/// a coluna <c>embedding</c> (<c>vector(1024)</c>), usando o tipo <see cref="Vector"/> do pgvector.
/// </summary>
public sealed class EmbeddingVectorConverter : ValueConverter<float[], Vector>
{
    /// <summary>
    /// Cria uma nova instância do conversor.
    /// </summary>
    public EmbeddingVectorConverter() : base(
        v => new Vector(v),
        v => v.ToArray())
    {
    }
}
