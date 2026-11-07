using ApexPost.Courier.Offline;
using ApexPost.Courier.Services;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Timeout;
using System.Net;

namespace ApexPost.Courier;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Offline store (Part A)
		var dbPath = Path.Combine(FileSystem.AppDataDirectory, "apexpost_courier");
		builder.Services.AddSingleton(sp =>
		{
			var db = new CourierDatabase(dbPath, sp.GetService<ILogger<CourierDatabase>>());
			db.CreateIndexes();
			return db;
		});
		builder.Services.AddSingleton<IScanRepository, CouchbaseScanRepository>();
		builder.Services.AddSingleton<IShipmentUploader, FakeShipmentUploader>();
		builder.Services.AddSingleton<OutboxService>();

		// Typed API client via IHttpClientFactory with resilience pipeline
		var apiBaseUrl = builder.Configuration["ApexApi:BaseUrl"] ?? "https://localhost:7xxx";

		builder.Services.AddTransient<AuthHeaderHandler>();
		builder.Services.AddHttpClient<ApexApiClient>(client =>
		{
			client.BaseAddress = new Uri(apiBaseUrl);
			client.Timeout = TimeSpan.FromSeconds(30); // overall request timeout
		})
		.AddHttpMessageHandler<AuthHeaderHandler>()
		.AddPolicyHandler(GetResiliencePipeline());

		builder.Services.AddSingleton<OfflineSyncService>();

		// Pages
		builder.Services.AddTransient<ApexPost.Courier.Pages.LoginPage>();
		builder.Services.AddTransient<ApexPost.Courier.Pages.NewScanPage>();
		builder.Services.AddTransient<ApexPost.Courier.Pages.PendingScansPage>();
		builder.Services.AddTransient<ApexPost.Courier.Pages.SyncStatusPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}

	private static IAsyncPolicy<HttpResponseMessage> GetResiliencePipeline()
	{
		// Per-attempt timeout: each individual try times out after 10s.
		// Overall request timeout is enforced by HttpClient.Timeout (30s) above.
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
					Console.WriteLine($"[Retry] Attempt {attempt} after {delay.TotalSeconds:F1}s: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}"));

		var circuitBreaker = Policy<HttpResponseMessage>
			.Handle<HttpRequestException>()
			.Or<TimeoutRejectedException>()
			.OrResult(msg => (int)msg.StatusCode >= 500)
			.CircuitBreakerAsync(
				handledEventsAllowedBeforeBreaking: 5,
				durationOfBreak: TimeSpan.FromSeconds(30),
				onBreak: (outcome, duration) => Console.WriteLine($"[CircuitBreaker] OPEN for {duration.TotalSeconds}s"),
				onReset: () => Console.WriteLine("[CircuitBreaker] CLOSED"),
				onHalfOpen: () => Console.WriteLine("[CircuitBreaker] HALF-OPEN"));

		// Wrap order: circuit breaker outermost, then retry, then per-attempt timeout innermost.
		return Policy.WrapAsync(circuitBreaker, retry, perAttemptTimeout);
	}
}
