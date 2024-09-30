using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prometheus;
using StackExchange.Redis;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var redisConnectionString = builder.Configuration.GetSection("Redis")["ConnectionString"];

        builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));
        builder.Services.AddSingleton<IRateLimitService, RateLimitService>();
        builder.Services.UseHttpClientMetrics();

        builder.Services.AddControllers();

        var app = builder.Build();


        var counter = Metrics.CreateCounter("http_requests_total", "Number of HTTP requests",
        new CounterConfiguration
        {
            LabelNames = new[] { "method", "status_code",  "url" }
        });

        app.Use(async (context, next) =>
        {
            await next.Invoke();
            if (!context.Request.Path.StartsWithSegments("/metrics"))
            {
                var method = context.Request.Method;
                var statusCode = context.Response.StatusCode.ToString();
                var url = context.Request.Path.Value;

                counter.WithLabels(method, statusCode, url).Inc();
            }
        });
        app.UseMiddleware<RateLimitingMiddleware>();
        app.UseRouting();
        app.UseHttpMetrics();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapMetrics();
        });
        app.MapControllers();

        app.Run();
    }
}