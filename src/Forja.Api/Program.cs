using System.Security.Claims;
using Forja.Api;
using Forja.Api.Auth;
using Forja.Application.Usuarios;
using Forja.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddNeonAuthJwtBearer(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/me", async (ClaimsPrincipal user, IUsuarioService usuarioService, CancellationToken cancellationToken) =>
{
    try
    {
        var usuario = await usuarioService.ResolverUsuarioAutenticadoAsync(user, cancellationToken);
        return Results.Ok(new UsuarioPerfilResponse(
            usuario.Id,
            usuario.Nome,
            usuario.Email,
            usuario.Nivel.ToString(),
            usuario.TempoDisponivelMinDia,
            usuario.FusoHorario));
    }
    catch (UsuarioNaoAutenticadoException)
    {
        return Results.Unauthorized();
    }
}).RequireAuthorization();

app.Run();
