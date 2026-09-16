using Astriology.API.Extensions;
using Astriology.API.Middleware;
using Astriology.Infrastructure;
using Astriology.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApi(builder.Configuration);

var app = builder.Build();

// Migrate and seed before the first request is served.
await DbInitializer.InitializeAsync(app.Services);

// Order below is execution order, not preference.

// Outermost, so it catches everything underneath.
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    // Development only: an exposed schema in production is an information leak.
    app.MapOpenApi();

    // Serves the UI at /swagger, reading the document produced by MapOpenApi above.
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Astriology API v1"));
}

app.UseHttpsRedirection();

app.UseRouting();

// After routing, before authentication, so CORS headers are present on error
// responses too.
app.UseCors(ServiceCollectionExtensions.CorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

app.Run();

/// <summary>
/// Top-level statements compile into an internal Program class. Declaring it public
/// lets WebApplicationFactory&lt;Program&gt; reference it from the test project.
/// </summary>
public partial class Program;
