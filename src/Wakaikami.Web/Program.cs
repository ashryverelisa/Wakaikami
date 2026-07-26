using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using OpenTelemetry.Metrics;
using Wakaikami.Core.Security;
using Wakaikami.Database.Login;
using Wakaikami.Database.Login.Extensions;
using Wakaikami.Web.Components;
using Wakaikami.Web.Services;
using Wakaikami.Web.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Prometheus scrape endpoint on the regular HTTP port; meter list mirrors
// MetricsHostingExtensions.AddPrometheusMetrics (Web has no Networking.Grpc reference).
builder
    .Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
        metrics
            .AddMeter(
                "Wakaikami.*",
                "System.Runtime",
                "Microsoft.AspNetCore.Hosting",
                "Microsoft.AspNetCore.Server.Kestrel",
                "Microsoft.EntityFrameworkCore"
            )
            .AddPrometheusExporter()
    );

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddMudServices();

// Account registration: shared password hashing (pepper MUST match the login service) + EF Core write path.
builder.Services.AddHttpContextAccessor();
builder.Services.AddPasswordHashing();

var loginConnectionString =
    builder.Configuration.GetConnectionString("Login") ?? throw new InvalidOperationException("Connection string 'Login' is not configured.");
builder.Services.AddLoginRegistration(options => options.UseSqlServer(loginConnectionString));

builder.Services.AddScoped<IAccountRegistrationService, AccountRegistrationService>();

builder.Services.AddHealthChecks().AddDbContextCheck<LoginDbContext>("database", tags: ["ready"]);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapPrometheusScrapingEndpoint();
app.MapHealthChecks("/healthz", new HealthCheckOptions { Predicate = static _ => false });
app.MapHealthChecks("/ready", new HealthCheckOptions { Predicate = static r => r.Tags.Contains("ready") });
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
