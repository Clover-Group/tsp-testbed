using HighlightBlazor;
using Microsoft.EntityFrameworkCore;
using Radzen;
using TspTestbed.Components;
using TspTestbed.Data;
using TspTestbed.Services;
using TspTestbed.Utils;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRadzenComponents();

builder.Services.AddScoped<BrowserTimeProvider>();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("TestDatabase"))
        .UseSnakeCaseNamingConvention();
});

builder.Services.AddHttpClient();

builder.Services.AddHighlight();

builder.Services.AddSingleton<RunStatusService>();
builder.Services.AddHostedService(p => p.GetRequiredService<RunStatusService>());

var app = builder.Build();

// Check migrations and apply them.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var modelContext = services.GetRequiredService<AppDbContext>();
    if (modelContext.Database.GetPendingMigrations().Any())
    {
        modelContext.Database.Migrate();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
