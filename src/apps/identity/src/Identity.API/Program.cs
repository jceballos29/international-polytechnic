var builder = WebApplication.CreateBuilder(args);

// ── Servicios ──────────────────────────────────────────────
// Aquí registraremos todos los servicios en las fases siguientes.
// Por ahora solo lo mínimo para que el servidor arranque.

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// CORS — permite peticiones desde los frontends en desarrollo
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:3001",
                "http://localhost:3002",
                "http://localhost:3003")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ── Pipeline HTTP ───────────────────────────────────────────
var app = builder.Build();

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Identity API v1"));
}
else
{
    app.UseHttpsRedirection();
}

app.MapControllers();

// Endpoint de salud — Docker y Kubernetes lo usan para
// saber si el servicio está vivo antes de enviarle tráfico
app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).WithTags("Health");

app.Run();
