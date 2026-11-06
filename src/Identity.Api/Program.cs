using System.Text;
using Identity.Api.Auth;
using Identity.Api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var jwt = builder.Configuration.GetSection("Jwt");
var jwtOptions = new JwtOptions(
    jwt["Issuer"] ?? throw new InvalidOperationException("Не задан Jwt:Issuer."),
    jwt["Audience"] ?? throw new InvalidOperationException("Не задан Jwt:Audience."),
    jwt["Key"] ?? throw new InvalidOperationException("Не задан Jwt:Key."));

builder.Services.AddSingleton(jwtOptions);
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Identity")));
builder.Services.AddScoped<AuthService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Identity v1"));
}

app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");

app.MapPost("/auth/register", async (RegisterRequest request, AuthService auth) =>
{
    if (request.Password.Length < 8)
        return Results.BadRequest(new { error = "Пароль должен быть не короче 8 символов." });

    try
    {
        var user = await auth.Register(request);
        return Results.Created($"/users/{user.Id}", user);
    }
    catch (DuplicateEmailException)
    {
        return Results.Conflict(new { error = "Такая почта уже занята." });
    }
});

app.MapPost("/auth/login", async (LoginRequest request, AuthService auth) =>
{
    try
    {
        return Results.Ok(await auth.Login(request));
    }
    catch (InvalidCredentialsException)
    {
        return Results.Unauthorized();
    }
});

app.MapGet("/users/{userId:guid}", async (Guid userId, AuthService auth) =>
{
    var user = await auth.Find(userId);
    return user is null ? Results.NotFound() : Results.Ok(user);
}).RequireAuthorization();

app.Run();