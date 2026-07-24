using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Wakaikami.Core.Security;
using Wakaikami.Database.Login.Extensions;
using Wakaikami.Web.Components;
using Wakaikami.Web.Services;
using Wakaikami.Web.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

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

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
