using Forja.Application.Common;
using Forja.Application.Usuarios;
using Microsoft.AspNetCore.Diagnostics;

namespace Forja.Api.ExceptionHandling;

/// <summary>
/// Handler global de exceções não tratadas pelos endpoints. Mapeia exceções de aplicação para o
/// status HTTP correspondente. Registrado via <c>AddExceptionHandler</c> e ativado por
/// <c>app.UseExceptionHandler()</c> em <c>Program.cs</c>.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;

    /// <summary>
    /// Cria uma nova instância do handler.
    /// </summary>
    /// <param name="logger">Logger para registrar a exceção antes de responder.</param>
    /// <param name="problemDetailsService">Serviço usado para escrever a resposta no formato ProblemDetails.</param>
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problemDetailsService)
    {
        _logger = logger;
        _problemDetailsService = problemDetailsService;
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, mensagem) = exception switch
        {
            NotFoundException notFound => (StatusCodes.Status404NotFound, notFound.Message),
            UsuarioNaoAutenticadoException => (StatusCodes.Status401Unauthorized, "Não autenticado."),
            ArgumentException argumento => (StatusCodes.Status400BadRequest, argumento.Message),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno."),
        };

        _logger.LogError(exception, "Exceção não tratada: {Mensagem}", mensagem);

        httpContext.Response.StatusCode = statusCode;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = statusCode,
                Detail = mensagem,
            },
        });
    }
}
