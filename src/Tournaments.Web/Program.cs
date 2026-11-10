using Tournaments.Web;
using Tournaments.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
    builder.Services.AddScoped<PlayerSession>();
builder.Services.AddHttpClient("gateway", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Gateway"] ?? "http://localhost:8080");
});
builder.Services.AddScoped(services =>
{
    var http = services.GetRequiredService<IHttpClientFactory>().CreateClient("gateway");
    return new GatewayClient(http, services.GetRequiredService<PlayerSession>());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
