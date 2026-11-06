using ApexPost.Courier.Offline;
using ApexPost.Courier.Services;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
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
		builder.Services.AddSingleton(_ => new CourierDatabase(dbPath));
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
		.AddPolicyHandler(GetRetryPolicy())
		.AddPolicyHandler(GetCircuitBreakerPolicy());

		builder.Services.AddSingleton<OfflineSyncService>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}

	private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
	{
		return Policy<HttpResponseMessage>
			.Handle<HttpRequestException>()
			.OrResult(msg => msg.StatusCode == HttpStatusCode.RequestTimeout
				|| (int)msg.StatusCode >= 500)
			.WaitAndRetryAsync(
				retryCount: 3,
				sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 500)),
				onRetry: (outcome, delay, attempt, ctx) =>
					Console.WriteLine($"[Retry] Attempt {attempt} after {delay.TotalSeconds:F1}s: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}"));
	}

	private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
	{
		return Policy<HttpResponseMessage>
			.Handle<HttpRequestException>()
			.OrResult(msg => (int)msg.StatusCode >= 500)
			.CircuitBreakerAsync(
				handledEventsAllowedBeforeBreaking: 5,
				breakDuration: TimeSpan.FromSeconds(30),
				onBreak: (outcome, duration) => Console.WriteLine($"[CircuitBreaker] OPEN for {duration.TotalSeconds}s"),
				onReset: () => Console.WriteLine("[CircuitBreaker] CLOSED"),
				onHalfOpen: () => Console.WriteLine("[CircuitBreaker] HALF-OPEN"));
	}
}
