using DisputePortal.Api.Exceptions;
using DisputePortal.Api.Middleware;
using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace DisputePortal.Tests.Middleware;

public class ExceptionHandlerMiddlewareTests
{
    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static ExceptionHandlerMiddleware Create(RequestDelegate next) =>
        new(next, NullLogger<ExceptionHandlerMiddleware>.Instance);

    [Fact]
    public async Task InvokeAsync_NoException_DoesNotAlterResponse()
    {
        var middleware = Create(_ => Task.CompletedTask);
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_UnauthorizedAccessException_Returns401()
    {
        var middleware = Create(_ => throw new UnauthorizedAccessException());
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(401, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_KeyNotFoundException_Returns404()
    {
        var middleware = Create(_ => throw new KeyNotFoundException("Dispute not found."));
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(404, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_InvalidOperationException_Returns400()
    {
        var middleware = Create(_ => throw new InvalidOperationException("Bad state."));
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(400, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_UnhandledException_Returns500()
    {
        var middleware = Create(_ => throw new Exception("Unexpected."));
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(500, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_KeyNotFoundException_ResponseBodyContainsMessage()
    {
        var middleware = Create(_ => throw new KeyNotFoundException("Dispute not found."));
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("Dispute not found.", body);
    }

    [Fact]
    public async Task InvokeAsync_InvalidOperationException_ResponseBodyIsGeneric()
    {
        var middleware = Create(_ => throw new InvalidOperationException("Internal details."));
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.DoesNotContain("Internal details.", body);
        Assert.Contains("Bad request", body);
    }

    [Fact]
    public async Task InvokeAsync_UnhandledException_ResponseBodyIsGeneric()
    {
        var middleware = Create(_ => throw new Exception("Secret internal error."));
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.DoesNotContain("Secret internal error.", body);
        Assert.Contains("unexpected", body);
    }

    [Fact]
    public async Task InvokeAsync_BusinessRuleException_Returns400WithMessage()
    {
        var middleware = Create(_ => throw new BusinessRuleException("Reply message cannot be blank."));
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(400, context.Response.StatusCode);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("Reply message cannot be blank.", body);
    }

    [Fact]
    public async Task InvokeAsync_BusinessRuleException_NotCaughtAsGenericInvalidOperation()
    {
        // BusinessRuleException must be caught before the generic InvalidOperationException handler
        var middleware = Create(_ => throw new BusinessRuleException("Specific user message."));
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.DoesNotContain("Bad request", body);
        Assert.Contains("Specific user message.", body);
    }
}
