using DugoutIQ.Api.Middleware;
using DugoutIQ.Api.Options;
using DugoutIQ.Application;
using DugoutIQ.Application.Options;
using DugoutIQ.Infrastructure;
using DugoutIQ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, _, loggerConfiguration) =>
        loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console());

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddOptions<BaseballDataOptions>().ValidateOnStart();
    builder.Services.AddOptions<RequestPerformanceOptions>()
        .Bind(builder.Configuration.GetSection(RequestPerformanceOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<DugoutIqExceptionHandler>();
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "DugoutIQ API",
            Version = "v1",
            Description = "Baseball Operations intelligence API. ASP.NET Core owns application behavior."
        });
    });

    builder.Services.AddSingleton(TimeProvider.System);
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<DugoutIQDbContext>("sqlserver");

    var configuredOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>();
    var origins = (configuredOrigins ?? ["http://localhost:3000"])
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Frontend", policy =>
        {
            if (origins.Length == 0)
            {
                return;
            }

            policy.WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    var app = builder.Build();

    // Outermost so ProblemDetails responses still carry Access-Control-Allow-Origin.
    app.UseCors("Frontend");

    if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DugoutIQDbContext>();
        await db.Database.MigrateAsync();
    }

    app.UseExceptionHandler();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<RequestPerformanceMiddleware>();
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0} ms CorrelationId={CorrelationId}";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier);
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "DugoutIQ API v1");
            options.RoutePrefix = "swagger";
        });
    }
    else
    {
        app.UseHttpsRedirection();
    }

    app.MapControllers();
    app.MapHealthChecks("/api/v1/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString()
                })
            });
        }
    });

    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "DugoutIQ API failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
