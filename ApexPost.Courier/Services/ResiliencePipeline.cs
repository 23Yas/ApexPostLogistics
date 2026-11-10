using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System.Net;

namespace ApexPost.Courier.Services;

/// <summary>
/// Singleton holder for the HTTP resilience pipeline. Registered once in DI so the
/// circuit breaker's state persists across requests and sync runs; the breaker state
/// is read back from Polly rather than tracked by hand.
/// </summary>
public sealed class ResiliencePipeline
{
	/// <summary>The composed policy (circuit breaker → retry → per-attempt timeout).</summary>
	public required IAsyncPolicy<HttpResponseMessage> Handler { get; init; }

	/// <summary>The underlying circuit breaker, exposed so callers can read its real state.</summary>
	public required AsyncCircuitBreakerPolicy<HttpResponseMessage> CircuitBreaker { get; init; }

	/// <summary>Current Polly circuit state as a display string (Closed/Open/HalfOpen/Isolated).</summary>
	public string CircuitState => CircuitBreaker.CircuitState switch
	{
		global::Polly.CircuitBreaker.CircuitState.Open => "Open",
		global::Polly.CircuitBreaker.CircuitState.HalfOpen => "HalfOpen",
		global::Polly.CircuitBreaker.CircuitState.Isolated => "Isolated",
		_ => "Closed"
	};

	public static ResiliencePipeline Create(ILogger logger)
	{
		// Per-attempt timeout: each individual try times out after 10s.
		// Overall request timeout is enforced by HttpClient.Timeout (30s).
		var perAttemptTimeout = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));

		var retry = Policy<HttpResponseMessage>
			.Handle<HttpRequestException>()
			.Or<TimeoutRejectedException>()
			.OrResult(msg => msg.StatusCode == HttpStatusCode.RequestTimeout
				|| (int)msg.StatusCode >= 500)
			.WaitAndRetryAsync(
				retryCount: 3,
				sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 500)),
				onRetry: (outcome, delay, attempt, ctx) =>
					logger.LogWarning("[Retry] Attempt {Attempt} after {Delay}s: {Reason}", attempt, delay.TotalSeconds, outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()));

		var circuitBreaker = Policy<HttpResponseMessage>
			.Handle<HttpRequestException>()
			.Or<TimeoutRejectedException>()
			.OrResult(msg => (int)msg.StatusCode >= 500)
			.CircuitBreakerAsync(
				handledEventsAllowedBeforeBreaking: 5,
				durationOfBreak: TimeSpan.FromSeconds(30),
				onBreak: (outcome, duration) => logger.LogWarning("[CircuitBreaker] OPEN for {Seconds}s", duration.TotalSeconds),
				onReset: () => logger.LogInformation("[CircuitBreaker] CLOSED"),
				onHalfOpen: () => logger.LogInformation("[CircuitBreaker] HALF-OPEN"));

		// Wrap order: circuit breaker outermost, then retry, then per-attempt timeout innermost.
		return new ResiliencePipeline
		{
			Handler = Polly.Policy.WrapAsync(circuitBreaker, retry, perAttemptTimeout),
			CircuitBreaker = circuitBreaker
		};
	}
}
