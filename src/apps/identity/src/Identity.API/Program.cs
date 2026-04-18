using System.Security.Cryptography;
using Identity.API.Middleware;
using Identity.Application;
using Identity.Infrastructure;
using Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── Application + Infrastructure ───────────────────────────
var mediatRLicense = builder.Configuration["MediatR:LicenseKey"];
builder.Services.AddApplication(mediatRLicense);
builder.Services.AddInfrastructure(builder.Configuration);

// ── Autenticación JWT Bearer ───────────────────────────────
var publicKeyPath = builder.Configuration["Jwt:PublicKeyPath"] ?? "keys/public.pem";
var publicRsa = RSA.Create();
publicRsa.ImportFromPem(File.ReadAllText(publicKeyPath));

builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(publicRsa),
        };
    });

builder.Services.AddAuthorization();

// ── CORS ───────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "IdentityUi",
        policy =>
            policy
                .WithOrigins("http://localhost:3000", "http://localhost:3002")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
    );
});

// ════════════════════════════════════════════════════════════
var app = builder.Build();

// ── Seed inicial ───────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync();
}

// ── Middleware pipeline ────────────────────────────────────
// El orden importa — cada middleware envuelve al siguiente
app.UseMiddleware<ExceptionMiddleware>(); // 1. captura excepciones
app.UseCors("IdentityUi"); // 2. CORS headers
app.UseAuthentication(); // 3. valida JWT
app.UseAuthorization(); // 4. verifica permisos

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).WithTags("Health");

app.Run();
