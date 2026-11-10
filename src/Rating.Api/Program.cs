using Microsoft.EntityFrameworkCore;
using Rating.Api.Data;
using Rating.Api.Messaging;
using Rating.Api.Rating;
using Swashbuckle.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<RatingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Rating")));
builder.Services.AddScoped<RatingService>();
builder.Services.AddHostedService<MatchCompletedListener>();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RatingDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Rating v1"));
}

app.MapHealthChecks("/health");

app.MapGet("/leaderboard", async (RatingService rating) =>
    Results.Ok(await rating.Leaderboard()));

app.MapGet("/players/{playerId:guid}", async (Guid playerId, RatingService rating) =>
{
    var player = await rating.Find(playerId);
    return player is null ? Results.NotFound() : Results.Ok(player);
});

app.Run();