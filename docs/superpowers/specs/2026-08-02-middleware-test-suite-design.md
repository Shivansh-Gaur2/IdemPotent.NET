# Middleware Test Suite Design

## Goal

Provide deterministic, production-relevant automated coverage for `IdempotencyMiddleware`, including the `PollAndWait` concurrency path that has only been manually planned so far.

## Scope

Create a `net8.0` xUnit test project that references `IdemPotent.Core`. Tests invoke the middleware through `DefaultHttpContext` and a thread-safe in-memory implementation of `IIdempotencyStore` held in the test project. The test store is a test double only; it will not be included in any production package.

## Coverage

- Non-candidate methods and requests without an idempotency key pass through unchanged.
- A first keyed request executes the handler and saves its response.
- A completed matching request replays the saved status and body without executing the handler.
- Reusing a key with a different fingerprint returns 422.
- A concurrent request returns 409 under `Reject409`.
- Under `PollAndWait`, a second matching request waits for a deliberately blocked first request, replays the completed response, and the handler executes exactly once.
- A failing handler removes its in-progress record so a retry can claim the key.

## Test Design

The concurrency test coordinates two middleware invocations with `TaskCompletionSource` instances instead of `Task.Delay`. The first handler signals when it owns the key, then waits until the test permits completion. The second invocation begins only after that signal, making the expected polling branch deterministic. Assertions verify both response payloads, the execution count, and successful completion before the configured deadline.

## Boundaries

No sample endpoint will be slowed permanently and no Docker service is required for this suite. Docker-backed smoke checks remain useful supplementary checks, but unit tests own middleware state-machine coverage.

## Validation

Run the new test project during development and `dotnet test IdemPotent.sln` after it is added. The subsequent cleanup and packaging slices will be validated separately to keep failures attributable.
