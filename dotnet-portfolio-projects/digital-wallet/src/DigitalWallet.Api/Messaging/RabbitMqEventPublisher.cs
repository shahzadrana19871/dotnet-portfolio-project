using System.Text.Json;
using RabbitMQ.Client;

namespace DigitalWallet.Api.Messaging;

/// <summary>
/// Publishes domain events to a durable topic exchange. The connection is created lazily
/// and reused for the lifetime of the application.
/// </summary>
public sealed class RabbitMqEventPublisher(IConfiguration configuration) : IEventPublisher, IAsyncDisposable
{
    private const string Exchange = "wallet.events";
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public async Task PublishAsync<T>(string routingKey, T message, CancellationToken ct = default)
    {
        var channel = await GetChannelAsync(ct);
        var body = JsonSerializer.SerializeToUtf8Bytes(message);
        var props = new BasicProperties { Persistent = true, ContentType = "application/json" };

        await _gate.WaitAsync(ct); // IChannel is not safe for concurrent publishes
        try
        {
            await channel.BasicPublishAsync(Exchange, routingKey, mandatory: false, basicProperties: props, body: body, cancellationToken: ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken ct)
    {
        if (_channel is { IsOpen: true }) return _channel;

        await _gate.WaitAsync(ct);
        try
        {
            if (_channel is { IsOpen: true }) return _channel;

            var factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMq:Host"] ?? "localhost",
                UserName = configuration["RabbitMq:User"] ?? "guest",
                Password = configuration["RabbitMq:Password"] ?? "guest"
            };
            _connection = await factory.CreateConnectionAsync(ct);
            _channel = await _connection.CreateChannelAsync(cancellationToken: ct);
            await _channel.ExchangeDeclareAsync(Exchange, ExchangeType.Topic, durable: true, cancellationToken: ct);
            return _channel;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null) await _channel.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
        _gate.Dispose();
    }
}
