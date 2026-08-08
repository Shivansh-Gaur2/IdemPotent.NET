using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdemShield.AspNetCore
{
    /// <summary>
    /// Coordinates idempotency records around supported ASP.NET Core requests.
    /// </summary>
    public class IdempotencyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IdempotencyOptions _options;
        /// <summary>Creates the middleware.</summary>
        /// <param name="next">The next request delegate in the pipeline.</param>
        /// <param name="options">The validated idempotency options.</param>
        public IdempotencyMiddleware(RequestDelegate next, IdempotencyOptions options)
        {
            _next = next;
            _options = options;
        }

        /// <summary>Processes one request using the configured idempotency store.</summary>
        /// <param name="context">The current HTTP context.</param>
        /// <param name="store">The application-selected idempotency store.</param>
        public async Task InvokeAsync(HttpContext context, IIdempotencyStore store)
        {
            if (!IsIdempotentCandidate(context.Request.Method))
            {
                await _next(context);
                return;
            }

            if (!context.Request.Headers.TryGetValue(_options.HeaderName, out var idempotencyKey))
            {
                await _next(context);
                return;
            }

            var clientKey = idempotencyKey.ToString();
            var key = _options.KeySelector?.Invoke(context, clientKey) ?? clientKey;
            if (string.IsNullOrWhiteSpace(key) || key.Length > 255)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { error = "The idempotency key must contain between 1 and 255 characters." });
                return;
            }

            context.Request.EnableBuffering();

            using var requestBodyStream = new MemoryStream();
            await context.Request.Body.CopyToAsync(requestBodyStream);
            byte[] bodyBytes = requestBodyStream.ToArray();
            context.Request.Body.Position = 0;

            var requestTarget = $"{context.Request.Path}{context.Request.QueryString}";
            string fingerprint = FingerprintCalculator.Compute(context.Request.Method, requestTarget, bodyBytes);

            var existingRecord = await store.GetAsync(key, context.RequestAborted);

            if (existingRecord is not null)
            {
                if (existingRecord.RequestFingerprint != fingerprint)
                {
                    context.Response.StatusCode = 422;
                    await context.Response.WriteAsJsonAsync(new { error = "Idempotency key conflict: request fingerprint does not match previous request." });
                    return;
                }

                if (existingRecord.Status == IdempotencyStatus.Completed)
                {
                    await ReplayResponseAsync(context, existingRecord);
                    return;
                }

                if (existingRecord.Status == IdempotencyStatus.InProgress)
                {
                    await HandleConcurrentRequestAsync(context, store, key);
                    return;
                }
            }

            var newRecord = new IdempotencyRecord
            {
                IdempotencyKey = key,
                RequestFingerprint = fingerprint,
                Status = IdempotencyStatus.InProgress,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.Add(_options.DefaultTtl)
            };

            bool wonTheRace = await store.TryInsertInProgressAsync(newRecord, context.RequestAborted);

            if (!wonTheRace)
            {
                await HandleConcurrentRequestAsync(context, store, key);
                return;
            }

            var originalBodyStream = context.Response.Body;

            using var bufferStream = new MemoryStream();
            context.Response.Body = bufferStream;

            try
            {
                await _next(context);
                bufferStream.Seek(0, SeekOrigin.Begin);

                var responseBodyText = await new StreamReader(bufferStream).ReadToEndAsync();

                var headersJson = System.Text.Json.JsonSerializer.Serialize(context.Response.Headers
                    .Where(header => !IsDerivedOrHopByHopHeader(header.Key))
                    .ToDictionary(header => header.Key, header => header.Value.ToString()));

                await store.UpdateAsCompletedAsync(key, context.Response.StatusCode, responseBodyText, headersJson, context.RequestAborted);

                bufferStream.Seek(0, SeekOrigin.Begin);
                await bufferStream.CopyToAsync(originalBodyStream, context.RequestAborted);
            }
            catch
            {
                await store.DeleteAsync(key, context.RequestAborted);
                throw;
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }

        private static bool IsIdempotentCandidate(string method) => method is "POST" or "PUT" or "PATCH";

        private static async Task ReplayResponseAsync(HttpContext context, IdempotencyRecord record)
        {
            context.Response.StatusCode = record.ResponseStatusCode ?? 500;

            if (!string.IsNullOrWhiteSpace(record.ResponseHeaders))
            {
                var headers = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(record.ResponseHeaders);
                if (headers is not null)
                {
                    foreach (var header in headers)
                    {
                        context.Response.Headers[header.Key] = header.Value;
                    }
                }
            }

            await context.Response.WriteAsync(record.ResponseBody ?? string.Empty);
        }

        private static bool IsDerivedOrHopByHopHeader(string headerName) =>
            headerName.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) ||
            headerName.Equals("Connection", StringComparison.OrdinalIgnoreCase) ||
            headerName.Equals("Keep-Alive", StringComparison.OrdinalIgnoreCase) ||
            headerName.Equals("Proxy-Authenticate", StringComparison.OrdinalIgnoreCase) ||
            headerName.Equals("Proxy-Authorization", StringComparison.OrdinalIgnoreCase) ||
            headerName.Equals("TE", StringComparison.OrdinalIgnoreCase) ||
            headerName.Equals("Trailer", StringComparison.OrdinalIgnoreCase) ||
            headerName.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) ||
            headerName.Equals("Upgrade", StringComparison.OrdinalIgnoreCase);

        private async Task HandleConcurrentRequestAsync(HttpContext context, IIdempotencyStore store, string key)
        {
            if (_options.ConcurrentRequestStrategy == ConcurrentRequestStrategy.Reject409)
            {
                context.Response.StatusCode = 409;
                context.Response.Headers.RetryAfter = "1";
                await context.Response.WriteAsJsonAsync(new { error = "Request already in progress. Retry shortly" });
                return;
            }

            var deadline = DateTimeOffset.UtcNow.AddSeconds(_options.MaxWaitSeconds);

            while (DateTimeOffset.UtcNow < deadline)
            {
                await Task.Delay(_options.PollIntervalMs, context.RequestAborted);

                var record = await store.GetAsync(key, context.RequestAborted);

                if (record?.Status == IdempotencyStatus.Completed)
                {
                    await ReplayResponseAsync(context, record);
                    return;
                }
            }

            context.Response.StatusCode = 409;
            context.Response.Headers.RetryAfter = "1";
            await context.Response.WriteAsJsonAsync(new { error = "Request already in progress. Retry shortly" });
        }
    }
}
