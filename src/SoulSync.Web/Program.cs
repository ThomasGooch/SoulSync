using SoulSync.Web.Components;
using SoulSync.Core.Interfaces;
using SoulSync.Core.Authentication;
using SoulSync.Data.Repositories;
using SoulSync.Services.Authentication;
using SoulSync.Services.AI;
using SoulSync.Agents.Registration;
using SoulSync.Agents.Analysis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Register services
builder.Services.AddScoped<IAuthenticationService, JwtAuthenticationService>();
builder.Services.AddScoped<IAIService, MockAIService>();

// Register agents
builder.Services.AddScoped<UserRegistrationAgent>();
builder.Services.AddScoped<ProfileAnalysisAgent>();

// Add JWT Authentication
var jwtSigningKey = builder.Configuration["JWT:SigningKey"] ?? "super-secret-key-that-is-at-least-32-characters-long-for-security";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JWT:Issuer"] ?? "SoulSync",
            ValidAudience = builder.Configuration["JWT:Audience"] ?? "SoulSyncUsers",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey))
        };
    });

builder.Services.AddAuthorization();

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map health check endpoint
app.MapHealthChecks("/health");

app.Run();
