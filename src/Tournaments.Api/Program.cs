using System.Text;
using Tournaments.Domain;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore;
using Tournaments.Api.Data;
using Tournaments.Api.Identity;
using Tournaments.Api.Tournaments;

var builder = WebApplication.CreateBuilder(args);

var jwt = builder.Configuration.GetSection("Jwt");
var issuer = jwt["Issuer"] ?? throw new InvalidOperationException("Не задан Jwt:Issuer.");
var audience = jwt["Audience"] ?? throw new InvalidOperationException("Не задан Jwt:Audience.");
var key = jwt["Key"] ?? throw new InvalidOperationException("Не задан Jwt:Key.");

builder.Services.AddDbContext<TournamentsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Tournaments")));
builder.Services.AddScoped<TournamentService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ForwardBearerHandler>();
builder.Services.AddHttpClient<IIdentityPlayers, IdentityPlayers>(client =>
{
    var baseUrl = builder.Configuration["Identity:BaseUrl"]
        ?? throw new InvalidOperationException("Не задан Identity:BaseUrl.");
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
}).AddHttpMessageHandler<ForwardBearerHandler>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateLifetime = true
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddOperationTransformer((operation, context, cancellationToken) =>
    {
        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
        });
        return Task.CompletedTask;
    });
});
builder.Services.AddHealthChecks();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TournamentsDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Tournaments v1"));
}

app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");

app.MapPost("/tournaments", async (CreateTournamentRequest request, TournamentService tournaments) =>
{
    try
    {
        var tournament = await tournaments.Create(request);
        return Results.Created($"/tournaments/{tournament.Id}", tournament);
    }
    catch (InvalidTournamentException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
}).RequireAuthorization();

app.MapPost("/tournaments/{tournamentId:guid}/players", async (
    Guid tournamentId,
    AddPlayerRequest request,
    TournamentService tournaments) =>
{
    try
    {
        await tournaments.AddPlayer(tournamentId, request);
        return Results.NoContent();
    }
    catch (TournamentNotFoundException)
    {
        return Results.NotFound();
    }
    catch (UnknownPlayerException)
    {
        return Results.NotFound();
    }
    catch (DuplicatePlayerException)
    {
        return Results.Conflict(new { error = "Этот игрок уже записан." });
    }
    catch (TournamentClosedException)
    {
        return Results.Conflict(new { error = "Запись уже закрыта." });
    }
    catch (InvalidTournamentException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
}).RequireAuthorization();

app.MapPost("/tournaments/{tournamentId:guid}/bracket", async (Guid tournamentId, TournamentService tournaments) =>
{
    try
    {
        return Results.Ok(await tournaments.BuildBracket(tournamentId));
    }
    catch (TournamentNotFoundException)
    {
        return Results.NotFound();
    }
    catch (TournamentClosedException)
    {
        return Results.Conflict(new { error = "Сетка уже построена." });
    }
    catch (InvalidTournamentException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
}).RequireAuthorization();

app.Run();
sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider schemes) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var all = await schemes.GetAllSchemesAsync();
        if (all.All(scheme => scheme.Name != "Bearer"))
            return;

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        };
    }
}