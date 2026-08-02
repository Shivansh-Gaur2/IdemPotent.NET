using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdemPotent.Core
{
    public class IdempotencyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IdempotencyOptions _options;


        public IdempotencyMiddleware(RequestDelegate next, IdempotencyOptions options)
        {
            _next = next;
            _options = options;
        }

        public async Task InvokeAsync(HttpContext context, IIdempotencyStore store)
        {
            if (!IsIdempotentCandidate(context.Request.Method))
            {
                await _next(context);
                return;
            }

            if(!context.Request.Headers.TryGetValue(_options.HeaderName, out var idempotencyKey))
            {
                await _next(context);
                return;
            }

            string key = idempotencyKey.ToString();

            // First let me read the body and based upon that compute the fingerprint
            context.Request.EnableBuffering(); //Since its a stream i want that it could be read multiple times

            using var requestBodyStream = new MemoryStream();
            await context.Request.Body.CopyToAsync(requestBodyStream);
            byte[] bodyBytes = requestBodyStream.ToArray();
            context.Request.Body.Position = 0;

            string fingerprint = FingerprintCalculator.Compute(context.Request.Method, context.Request.Path, bodyBytes);

            // Now check that i have seen this key before 
            var existingRecord = await store.GetAsync(key, context.RequestAborted);

            if(existingRecord is not null)
            {
                if(existingRecord.RequestFingerprint != fingerprint)
                {
                    context.Response.StatusCode = 422;
                    await context.Response.WriteAsJsonAsync(new { error = "Idempotency key conflict: request fingerprint does not match previous request." });
                    return;
                }

                if(existingRecord.Status == IdempotencyStatus.Completed)
                {
                    await ReplayResponseAsync(context, existingRecord);
                    return;
                }

                if(existingRecord.Status == IdempotencyStatus.InProgress)
                {
                    await HandleConcurrentRequestAsync(context, store, key);
                    return;
                }
            }

            // Try to claim this key
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

            // Now we have claimed the key, we can proceed to process the request
            var orignalBodyStream = context.Response.Body;

            using var bufferStream = new MemoryStream();
            context.Response.Body = bufferStream;

            try
            {
                await _next(context);
                bufferStream.Seek(0, SeekOrigin.Begin);

                var responseBodyText = await new StreamReader(bufferStream).ReadToEndAsync();

                var headersJson = System.Text.Json.JsonSerializer.Serialize(
                    context.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()));

                await store.UpdateAsCompletedAsync(key, context.Response.StatusCode, responseBodyText, headersJson, context.RequestAborted);

                bufferStream.Seek(0, SeekOrigin.Begin);
                await bufferStream.CopyToAsync(orignalBodyStream);
            }
            catch
            {
                await store.DeleteAsync(key, context.RequestAborted);
                throw;
            }
            finally
            {
                context.Response.Body = orignalBodyStream;
            }
        }

        private static bool IsIdempotentCandidate(string method) => method is "POST" or "PUT" or "PATCH";

        private static async Task ReplayResponseAsync(HttpContext context, IdempotencyRecord record)
        {
            context.Response.StatusCode = record.ResponseStatusCode ?? 500;
            context.Response.ContentType = "application/json";

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

            // Write the response body back to the main response stream
            await context.Response.WriteAsync(record.ResponseBody ?? string.Empty);
        }

        private async Task HandleConcurrentRequestAsync(HttpContext context, IIdempotencyStore store, string key)
        {
            if(_options.ConcurrentRequestStrategy == ConcurrentRequestStrategy.Reject409)
            {
                context.Response.StatusCode = 409;
                context.Response.Headers.RetryAfter = "1";
                await context.Response.WriteAsJsonAsync(new { error = "Request already in progress. Retry shortly" });
                return;
            }

            var deadline = DateTimeOffset.UtcNow.AddSeconds(_options.MaxWaitSeconds);

            while(DateTimeOffset.UtcNow < deadline)
            {
                await Task.Delay(_options.PollIntervalMs, context.RequestAborted);

                var record = await store.GetAsync(key, context.RequestAborted);

                if(record?.Status == IdempotencyStatus.Completed)
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
