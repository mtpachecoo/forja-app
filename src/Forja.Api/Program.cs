using System.Security.Claims;
using Forja.Api;
using Forja.Api.Auth;
using Forja.Api.ExceptionHandling;
using Forja.Application.Desempenho;
using Forja.Application.Duvidas;
using Forja.Application.Estudo;
using Forja.Application.Gamificacao;
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
builder.Services.AddScoped<IRegistrarRespostaComEfeitosService, RegistrarRespostaComEfeitosService>();
builder.Services.AddScoped<IRagService, RagService>();
builder.Services.AddScoped<IPesoDisciplinaService, PesoDisciplinaService>();
builder.Services.AddScoped<IPlanoEstudoService, PlanoEstudoService>();
builder.Services.AddScoped<ISessaoEstudoService, SessaoEstudoService>();
builder.Services.AddScoped<IIniciarSessaoComEfeitosService, IniciarSessaoComEfeitosService>();
builder.Services.AddScoped<IPomodoroService, PomodoroService>();
builder.Services.AddScoped<IRevisaoEspacadaService, RevisaoEspacadaService>();
builder.Services.AddScoped<IStreakService, StreakService>();
builder.Services.AddScoped<IPontuacaoService, PontuacaoService>();
builder.Services.AddScoped<IAnaliseDesempenhoService, AnaliseDesempenhoService>();
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
    IRegistrarRespostaComEfeitosService registrarRespostaComEfeitosService,
    CancellationToken cancellationToken) =>
{
    var usuario = await usuarioService.ResolverUsuarioAutenticadoAsync(user, cancellationToken);
    var resultado = await registrarRespostaComEfeitosService.RegistrarAsync(
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

app.MapPost("/duvidas", async (
    DuvidaRequest request,
    IRagService ragService,
    CancellationToken cancellationToken) =>
{
    var resultado = await ragService.ResponderDuvidaAsync(request.QuestaoId, request.Pergunta, cancellationToken);
    return Results.Ok(new DuvidaResponse(resultado.Resposta, resultado.ChunksUsadosIds));
}).RequireAuthorization();

app.MapPost("/sessao/iniciar", async (
    ClaimsPrincipal user,
    IUsuarioService usuarioService,
    IIniciarSessaoComEfeitosService iniciarSessaoComEfeitosService,
    CancellationToken cancellationToken) =>
{
    var usuario = await usuarioService.ResolverUsuarioAutenticadoAsync(user, cancellationToken);
    var sessao = await iniciarSessaoComEfeitosService.IniciarAsync(usuario.Id, cancellationToken);
    return Results.Ok(SessaoResponse.De(sessao));
}).RequireAuthorization();

app.MapPost("/sessao/{sessaoId:guid}/pomodoro/iniciar", async (
    Guid sessaoId,
    ClaimsPrincipal user,
    IUsuarioService usuarioService,
    IPomodoroService pomodoroService,
    CancellationToken cancellationToken) =>
{
    var usuario = await usuarioService.ResolverUsuarioAutenticadoAsync(user, cancellationToken);
    var pomodoro = await pomodoroService.IniciarPomodoroAsync(usuario.Id, sessaoId, cancellationToken);
    return Results.Ok(PomodoroResponse.De(pomodoro));
}).RequireAuthorization();

app.MapPost("/sessao/{sessaoId:guid}/pomodoro/{pomodoroId:guid}/finalizar", async (
    Guid sessaoId,
    Guid pomodoroId,
    ClaimsPrincipal user,
    IUsuarioService usuarioService,
    IPomodoroService pomodoroService,
    CancellationToken cancellationToken) =>
{
    var usuario = await usuarioService.ResolverUsuarioAutenticadoAsync(user, cancellationToken);
    var resultado = await pomodoroService.FinalizarPomodoroAsync(usuario.Id, sessaoId, pomodoroId, cancellationToken);
    return Results.Ok(FinalizarPomodoroResponse.De(resultado));
}).RequireAuthorization();

app.MapGet("/revisao/pendente", async (
    ClaimsPrincipal user,
    IUsuarioService usuarioService,
    IRevisaoEspacadaService revisaoEspacadaService,
    CancellationToken cancellationToken) =>
{
    var usuario = await usuarioService.ResolverUsuarioAutenticadoAsync(user, cancellationToken);
    var pendentes = await revisaoEspacadaService.ObterPendentesAsync(usuario.Id, cancellationToken);
    return Results.Ok(pendentes.Select(RevisaoPendenteItem.De));
}).RequireAuthorization();

app.MapGet("/ranking/semanal", async (
    Guid? carreiraId,
    IPontuacaoService pontuacaoService,
    CancellationToken cancellationToken) =>
{
    var ranking = await pontuacaoService.ObterRankingSemanalAsync(carreiraId, cancellationToken);
    return Results.Ok(ranking.Select(RankingResponseItem.De));
}).RequireAuthorization();

app.MapGet("/plano/atual", async (
    Guid carreiraId,
    ClaimsPrincipal user,
    IUsuarioService usuarioService,
    IPlanoEstudoService planoEstudoService,
    CancellationToken cancellationToken) =>
{
    var usuario = await usuarioService.ResolverUsuarioAutenticadoAsync(user, cancellationToken);
    var plano = await planoEstudoService.ObterOuGerarPlanoAtualAsync(
        usuario.Id,
        carreiraId,
        usuario.TempoDisponivelMinDia,
        usuario.Nivel,
        cancellationToken);
    return Results.Ok(PlanoAtualResponse.De(plano));
}).RequireAuthorization();

app.MapGet("/desempenho/alertas", async (
    ClaimsPrincipal user,
    IUsuarioService usuarioService,
    IAnaliseDesempenhoService analiseDesempenhoService,
    CancellationToken cancellationToken) =>
{
    var usuario = await usuarioService.ResolverUsuarioAutenticadoAsync(user, cancellationToken);
    var alertas = await analiseDesempenhoService.DetectarQuedaDeDesempenhoAsync(usuario.Id, cancellationToken);
    return Results.Ok(alertas.Select(AlertaDesempenhoResponse.De));
}).RequireAuthorization();

app.MapPost("/plano/recriar", async (
    Guid carreiraId,
    ClaimsPrincipal user,
    IUsuarioService usuarioService,
    IPlanoEstudoService planoEstudoService,
    CancellationToken cancellationToken) =>
{
    var usuario = await usuarioService.ResolverUsuarioAutenticadoAsync(user, cancellationToken);
    var resultado = await planoEstudoService.RecriarPlanoAsync(
        usuario.Id,
        carreiraId,
        usuario.TempoDisponivelMinDia,
        usuario.Nivel,
        cancellationToken);
    return Results.Ok(RecriarPlanoResponse.De(resultado));
}).RequireAuthorization();

app.Run();
