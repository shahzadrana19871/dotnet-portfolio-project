using SapPosEtl.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<EtlOptions>(builder.Configuration.GetSection("Etl"));
builder.Services.Configure<PosOptions>(builder.Configuration.GetSection("Pos"));
builder.Services.Configure<SapOptions>(builder.Configuration.GetSection("Sap"));

builder.Services.AddSingleton<IPosSalesSource, SqlServerPosSource>();
builder.Services.AddSingleton<ISyncStateStore, SqliteSyncStateStore>();

builder.Services
    .AddHttpClient<ISapClient, SapServiceLayerClient>((sp, client) =>
    {
        var sap = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SapOptions>>().Value;
        client.BaseAddress = new Uri(sap.BaseUrl);
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = true });

builder.Services.AddTransient<EtlRunner>();
builder.Services.AddHostedService<EtlWorker>();

builder.Build().Run();
