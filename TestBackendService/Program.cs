var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

builder.Services.AddScoped<TestBackendService.Services.IMockDataService, TestBackendService.Services.MockDataService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/openapi/v1.json", "TestBackendService v1");
        c.RoutePrefix = "swagger"; // UI at /swagger
    });
}
else
{
    app.UseExceptionHandler();
    app.UseHsts();
}

var disableHttpsRedirect = Environment.GetEnvironmentVariable("DISABLE_HTTPS_REDIRECT");
var skipHttpsRedirect = string.Equals(disableHttpsRedirect, "true", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(disableHttpsRedirect, "1", StringComparison.OrdinalIgnoreCase);

if (!skipHttpsRedirect)
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

// Expose liveness/readiness endpoints for orchestrators
app.MapHealthChecks("/healthz");

app.Run();
