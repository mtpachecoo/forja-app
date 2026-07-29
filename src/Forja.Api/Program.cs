using System.Security.Claims;
using Forja.Api;
using Forja.Api.Auth;
using Forja.Api.ExceptionHandling;
using Forja.Application.Estudo;
using Forja.Application.Questoes;
using Forja.Application.Usuarios;
using Forja.Infrastructure;
using Forja.Infrastructure.Ia;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddIa(builder.Configuration);
builder.Services.AddNeonAuthJwtBearer(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IQuestaoService, QuestaoService>();
builder.Services.AddScoped<IRespostaService, RespostaService>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/me", async (ClaimsPrincipal user, IUsuarioService usuarioService, CancellationToken cancellationToken) =>
{
    var usuario = await usuarioService.ResolverUsuarioAutenticadoAsync(user, cancellationToken);
    return Results.Ok(new UsuarioPerfilResponse(
        usuario.Id,
        usuario.Nome,
        usuario.Email,
        usuario.Nivel.ToString(),
        usuario.TempoDisponivelMinDia,
        usuario.FusoHorario));
}).RequireAuthorization();

app.MapGet("/questoes", async (
    Guid? carreiraId,
    Guid? bancaId,
    Guid? disciplinaId,
    IQuestaoService questaoService,
    CancellationToken cancellationToken) =>
{
    var questoes = await questaoService.BuscarAsync(carreiraId, bancaId, disciplinaId, cancellationToken);
    return Results.Ok(questoes.Select(QuestaoResponse.De));
}).RequireAuthorization();

app.MapGet("/questoes/{id:guid}", async (Guid id, IQuestaoService questaoService, CancellationToken cancellationToken) =>
{
    var questao = await questaoService.ObterPorIdAsync(id, cancellationToken);
    return Results.Ok(QuestaoResponse.De(questao));
}).RequireAuthorization();

app.MapPost("/respostas", async (
    RegistrarRespostaRequest request,
    ClaimsPrincipal user,
    IUsuarioService usuarioService,
    IRespostaService respostaService,
    CancellationToken cancellationToken) =>
{
    var usuario = await usuarioService.ResolverUsuarioAutenticadoAsync(user, cancellationToken);
    var resultado = await respostaService.RegistrarRespostaAsync(
        usuario.Id,
        request.QuestaoId,
        request.RespostaDada,
        request.TempoRespostaMs,
        request.PomodoroId,
        request.EhRevisao,
        cancellationToken);

    return Results.Ok(new RegistrarRespostaResponse(
        resultado.Resposta.Correta,
        resultado.Resposta.Pontuada,
        resultado.Resposta.PontosConcedidos,
        resultado.Pontuacao.PontosTotal,
        resultado.Pontuacao.PontosSemanaAtual,
        resultado.Questao.Gabarito,
        resultado.Questao.Explicacao));
}).RequireAuthorization();

app.Run();
