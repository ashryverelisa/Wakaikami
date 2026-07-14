using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder();

var app = builder.Build();

await app.RunAsync();
