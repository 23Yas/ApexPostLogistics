using ApexPost.Courier.Offline;
using ApexPost.Courier.Services;
using Microsoft.Extensions.Logging;
using Polly;

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
		builder.Services.AddSingleton(sp =>
		{
			var db = new CourierDatabase("apexpost_courier", sp.GetService<ILogger<CourierDatabase>>());
			db.CreateIndexes();
			return db;
		});
		builder.Services.AddSingleton<IScanRepository, CouchbaseScanRepository>();
		builder.Services.AddSingleton<OutboxService>();

		// Typed API client via IHttpClientFactory with resilience pipeline.
		// Base URL: configuration value wins; platform defaults cover emulator/desktop dev.
		var apiBaseUrl = builder.Configuration["ApexApi:BaseUrl"];
#if ANDROID
		apiBaseUrl ??= "https://10.0.2.2:7145";   // Android emulator -> host loopback (HTTPS; dev cert bypassed in DEBUG)
#elif WINDOWS
		apiBaseUrl ??= "https://localhost:7145";  // Windows desktop -> local API (HTTPS)
#else
		apiBaseUrl ??= "https://localhost:7145";
#endif

		builder.Services.AddTransient<AuthHeaderHandler>();

		// Singleton so the circuit breaker's state persists across requests; built with
		// the app's ILogger so retry/breaker events reach the configured logging providers.
		builder.Services.AddSingleton(sp =>
			ResiliencePipeline.Create(sp.GetRequiredService<ILogger<ApexApiClient>>()));

		builder.Services.AddHttpClient<ApexApiClient>(client =>
		{
			client.BaseAddress = new Uri(apiBaseUrl);
			client.Timeout = TimeSpan.FromSeconds(30); // overall request timeout
		})
		.AddHttpMessageHandler<AuthHeaderHandler>()
		.AddPolicyHandler((sp, request) =>
			// Retry only idempotent calls: safe methods plus the scan upload,
			// which is idempotent by design (server de-duplicates by scan ID).
			IsRetryable(request)
				? sp.GetRequiredService<ResiliencePipeline>().Handler
				: Policy.NoOpAsync<HttpResponseMessage>());

#if DEBUG
		// Development-only: trust the local ASP.NET dev certificate so the
		// Android emulator / Windows desktop can call https://localhost.
		// Never compiled into Release builds.
		builder.Services.AddHttpClient<ApexApiClient>()
			.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
			{
				ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
			});
#endif

		builder.Services.AddTransient<IShipmentUploader, ApiShipmentUploader>();
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

	private static bool IsRetryable(HttpRequestMessage request)
	{
		if (request.Method == HttpMethod.Get || request.Method == HttpMethod.Put
			|| request.Method == HttpMethod.Patch || request.Method == HttpMethod.Delete)
		{
			return true;
		}
		// POST /api/v2/shipments/{tracking}/scans is idempotent (scan IDs de-duplicate).
		return request.Method == HttpMethod.Post
			&& request.RequestUri?.AbsolutePath.EndsWith("/scans", StringComparison.OrdinalIgnoreCase) == true;
	}

}
