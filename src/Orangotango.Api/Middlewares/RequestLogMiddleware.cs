﻿using Microsoft.AspNetCore.Http;
using Orangotango.Core.Abstractions;
using Orangotango.Core.Enums;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Orangotango.Api.Middlewares;

internal sealed class RequestLogMiddleware(RequestDelegate _next, ILoggerService _logger)
{
    public async Task Invoke(HttpContext context)
    {
        var traceId = Guid.NewGuid();
        var message = $"{context.Request.Method} {context.Request.Path.Value}";

        await LogRequest(message, traceId, context);

        try
        {
            await LogResponseAndInvokeNext(message, traceId, context);
        }
        catch (Exception exception)
        {
            _logger.Error(nameof(OperationLogs.RequestFailure), message, exception, traceId);
        }
    }

    private async Task LogRequest(string message, Guid traceId, HttpContext context)
    {
        var body = await ReadRequestBody(context);
        context.Response.Headers.Append("TraceId", traceId.ToString());

        _logger.Information(nameof(OperationLogs.ReceivedRequest), message, body, traceId);
    }

    private static async Task<object> ReadRequestBody(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Request.EnableBuffering();
        var streamReader = new StreamReader(context.Request.Body);
        var body = await streamReader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        return DeserializeJson(body);
    }

    private async Task LogResponseAndInvokeNext(string message, Guid traceId, HttpContext context)
    {
        using var buffer = new MemoryStream();
        var stream = context.Response.Body;
        context.Response.Body = buffer;

        await _next.Invoke(context);

        buffer.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(buffer);
        var body = await reader.ReadToEndAsync();

        buffer.Seek(0, SeekOrigin.Begin);

        await buffer.CopyToAsync(stream);
        context.Response.Body = stream;

        _logger.Information(nameof(OperationLogs.ReturnedResponse),
            message,
           DeserializeJson(body),
            context.Response.StatusCode,
            traceId);
    }

    private static object DeserializeJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonSerializer.Deserialize<object>(json);
    }
}
