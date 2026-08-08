using System.Text;
using IdemPotent.Core;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace IdemPotent.Core.Tests;

public class IdempotencyMiddlewareTests
{
    [Theory]
    [InlineData(-1, 200)]
    [InlineData(5, 0)]
    public void Add_idempotency_rejects_invalid_polling_configuration(int maxWaitSeconds, int pollIntervalMs)
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();

        Assert.Throws<ArgumentOutOfRangeException>(() => services.AddIdempotency(options =>
        {
            options.MaxWaitSeconds = maxWaitSeconds;
            options.PollIntervalMs = pollIntervalMs;
        }));
    }

    [Fact]
    public async Task Request_without_an_idempotency_key_passes_through()
    {
        var executions = 0;
        var middleware = new IdempotencyMiddleware(
            context =>
            {
                executions++;
                return context.Response.WriteAsync("processed");
            },
            new IdempotencyOptions());
        var context = CreateContext("POST", null, "{}");

        await middleware.InvokeAsync(context, new InMemoryIdempotencyStore());

        Assert.Equal(1, executions);
        Assert.Equal("processed", await ReadResponseAsync(context));
    }

    [Fact]
    public async Task Put_with_same_key_and_body_replays_first_response()
    {
        var executions = 0;
        var store = new InMemoryIdempotencyStore();
        var middleware = new IdempotencyMiddleware(
            async context =>
            {
                var orderId = Interlocked.Increment(ref executions);
                context.Response.Headers["X-Request-Id"] = "original-request";
                await context.Response.WriteAsync($"{{\"orderId\":{orderId}}}");
            },
            new IdempotencyOptions());

        var first = CreateContext("PUT", "same-key", "{\"productName\":\"book\"}");
        await middleware.InvokeAsync(first, store);

        var second = CreateContext("PUT", "same-key", "{\"productName\":\"book\"}");
        await middleware.InvokeAsync(second, store);

        Assert.Equal(1, executions);
        Assert.Equal("{\"orderId\":1}", await ReadResponseAsync(first));
        Assert.Equal("{\"orderId\":1}", await ReadResponseAsync(second));
        Assert.Equal("original-request", second.Response.Headers["X-Request-Id"]);
    }

    [Fact]
    public async Task Replay_preserves_content_type_and_custom_headers_without_replaying_content_length()
    {
        var executions = 0;
        var store = new InMemoryIdempotencyStore();
        var middleware = new IdempotencyMiddleware(
            async context =>
            {
                executions++;
                context.Response.ContentType = "text/plain; charset=utf-8";
                context.Response.ContentLength = 4;
                context.Response.Headers["X-Result"] = "original";
                await context.Response.WriteAsync("done");
            },
            new IdempotencyOptions());

        await middleware.InvokeAsync(CreateContext("POST", "same-key", "{}"), store);
        var replay = CreateContext("POST", "same-key", "{}");
        await middleware.InvokeAsync(replay, store);

        Assert.Equal(1, executions);
        Assert.Equal("text/plain; charset=utf-8", replay.Response.ContentType);
        Assert.Equal("original", replay.Response.Headers["X-Result"]);
        Assert.Equal("done", await ReadResponseAsync(replay));
        Assert.Null(replay.Response.ContentLength);
    }

    [Fact]
    public async Task Key_selector_can_scope_identical_client_keys_to_different_tenants()
    {
        var executions = 0;
        var middleware = new IdempotencyMiddleware(
            context =>
            {
                executions++;
                return context.Response.WriteAsync("processed");
            },
            new IdempotencyOptions
            {
                KeySelector = (context, key) => $"{context.Request.Headers["X-Tenant"]}:{key}"
            });
        var store = new InMemoryIdempotencyStore();

        await middleware.InvokeAsync(CreateContext("POST", "same-key", "{}", "tenant-a"), store);
        await middleware.InvokeAsync(CreateContext("POST", "same-key", "{}", "tenant-b"), store);

        Assert.Equal(2, executions);
    }

    [Fact]
    public async Task Same_key_with_a_different_body_returns_unprocessable_entity()
    {
        var executions = 0;
        var store = new InMemoryIdempotencyStore();
        var middleware = new IdempotencyMiddleware(
            context =>
            {
                executions++;
                return context.Response.WriteAsync("processed");
            },
            new IdempotencyOptions());

        await middleware.InvokeAsync(CreateContext("POST", "same-key", "{\"productName\":\"book\"}"), store);
        var conflictingRequest = CreateContext("POST", "same-key", "{\"productName\":\"pen\"}");

        await middleware.InvokeAsync(conflictingRequest, store);

        Assert.Equal(1, executions);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, conflictingRequest.Response.StatusCode);
    }

    [Fact]
    public async Task Concurrent_request_is_rejected_when_configured_to_reject()
    {
        var store = new InMemoryIdempotencyStore();
        var handlerStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowHandlerToFinish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var middleware = new IdempotencyMiddleware(
            async context =>
            {
                handlerStarted.TrySetResult();
                await allowHandlerToFinish.Task;
                await context.Response.WriteAsync("processed");
            },
            new IdempotencyOptions { ConcurrentRequestStrategy = ConcurrentRequestStrategy.Reject409 });
        var first = CreateContext("POST", "same-key", "{}");
        var firstTask = middleware.InvokeAsync(first, store);
        await handlerStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        var second = CreateContext("POST", "same-key", "{}");
        await middleware.InvokeAsync(second, store);
        allowHandlerToFinish.TrySetResult();
        await firstTask;

        Assert.Equal(StatusCodes.Status409Conflict, second.Response.StatusCode);
        Assert.Equal("1", second.Response.Headers.RetryAfter);
    }

    [Fact]
    public async Task Concurrent_request_waits_and_replays_when_configured_to_poll()
    {
        var executions = 0;
        var store = new InMemoryIdempotencyStore();
        var handlerStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowHandlerToFinish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var middleware = new IdempotencyMiddleware(
            async context =>
            {
                var orderId = Interlocked.Increment(ref executions);
                handlerStarted.TrySetResult();
                await allowHandlerToFinish.Task;
                await context.Response.WriteAsync($"{{\"orderId\":{orderId}}}");
            },
            new IdempotencyOptions
            {
                ConcurrentRequestStrategy = ConcurrentRequestStrategy.PollAndWait,
                MaxWaitSeconds = 2,
                PollIntervalMs = 10
            });
        var first = CreateContext("POST", "same-key", "{}");
        var firstTask = middleware.InvokeAsync(first, store);
        await handlerStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        var second = CreateContext("POST", "same-key", "{}");
        var secondTask = middleware.InvokeAsync(second, store);
        await store.InProgressRecordRead.WaitAsync(TimeSpan.FromSeconds(2));
        allowHandlerToFinish.TrySetResult();
        await Task.WhenAll(firstTask, secondTask);

        Assert.Equal(1, executions);
        Assert.Equal("{\"orderId\":1}", await ReadResponseAsync(first));
        Assert.Equal("{\"orderId\":1}", await ReadResponseAsync(second));
    }

    [Fact]
    public async Task Failed_request_releases_key_for_a_retry()
    {
        var executions = 0;
        var middleware = new IdempotencyMiddleware(
            context =>
            {
                executions++;
                return executions == 1
                    ? Task.FromException(new InvalidOperationException("boom"))
                    : context.Response.WriteAsync("recovered");
            },
            new IdempotencyOptions());
        var store = new InMemoryIdempotencyStore();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.InvokeAsync(CreateContext("POST", "same-key", "{}"), store));
        var retry = CreateContext("POST", "same-key", "{}");
        await middleware.InvokeAsync(retry, store);

        Assert.Equal(2, executions);
        Assert.Equal("recovered", await ReadResponseAsync(retry));
    }

    private static DefaultHttpContext CreateContext(string method, string? key, string body, string? tenant = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = "/orders";
        if (key is not null)
        {
            context.Request.Headers["Idempotency-Key"] = key;
        }
        if (tenant is not null)
        {
            context.Request.Headers["X-Tenant"] = tenant;
        }
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadResponseAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }
}
