using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRateLimitService _rateLimitService;

    public RateLimitingMiddleware(RequestDelegate next, IRateLimitService rateLimitService)
    {
        _next = next;
        _rateLimitService = rateLimitService;
    }

    public async Task InvokeAsync(HttpContext context)
    {


        if (!context.Request.Path.StartsWithSegments("/api/notification", StringComparison.InvariantCultureIgnoreCase))
        {
            await _next(context);
            return;
        }
        context.Request.EnableBuffering();

        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        var bodyContent = await reader.ReadToEndAsync();

        context.Request.Body.Position = 0;

        var bodyData = JsonSerializer.Deserialize<Dictionary<string, string>>(bodyContent);

        if (bodyData == null || !bodyData.ContainsKey("recipient") || !bodyData.ContainsKey("type"))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Recipient and notification type are required.");
            return;
        }

        var recipient = bodyData["recipient"];
        var type = bodyData["type"];

        if (string.IsNullOrEmpty(recipient) || string.IsNullOrEmpty(type))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Recipient and notification type are required.");
            return;
        }

        if (!_rateLimitService.IsValidNotificationType(type))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Invalid notification type.");
            return;
        }

        var rateLimitInfo = await _rateLimitService.GetRateLimitInfo(recipient, type);

        if (!rateLimitInfo.Allowed)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["X-RateLimit-Limit"] = rateLimitInfo.Limit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = rateLimitInfo.Remaining.ToString();
            context.Response.Headers["X-RateLimit-Reset"] = rateLimitInfo.Reset.ToString();
            await context.Response.WriteAsync("Rate limit exceeded.");
            return;
        }

        await _next(context);
    }
}
