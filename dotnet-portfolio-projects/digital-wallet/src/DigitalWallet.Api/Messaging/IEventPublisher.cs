namespace DigitalWallet.Api.Messaging;

public interface IEventPublisher
{
    Task PublishAsync<T>(string routingKey, T message, CancellationToken ct = default);
}
