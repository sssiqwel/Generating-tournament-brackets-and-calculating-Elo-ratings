using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace Tournaments.Api.Messaging;

public sealed record MatchCompleted(Guid MatchId, Guid TournamentId, Guid PlayerAId, Guid PlayerBId, double ScoreA);

public interface IMatchCompletedPublisher
{
    Task Publish(MatchCompleted message);
}

public sealed class RabbitMatchPublisher(IConfiguration configuration) : IMatchCompletedPublisher
{
    private readonly object _gate = new();
    private IConnection? _connection;
    private IModel? _channel;

    public Task Publish(MatchCompleted message)
    {
        var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var body = Encoding.UTF8.GetBytes(json);

        lock (_gate)
        {
            if (_channel is null)
            {
                var factory = new ConnectionFactory
                {
                    HostName = configuration["RabbitMQ:Host"] ?? "localhost",
                    UserName = configuration["RabbitMQ:User"] ?? "tournament",
                    Password = configuration["RabbitMQ:Password"] ?? "tournament"
                };
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                _channel.ExchangeDeclare("tournaments", ExchangeType.Topic, durable: true);
                _channel.QueueDeclare("match-completed", durable: true, exclusive: false, autoDelete: false);
                _channel.QueueBind("match-completed", "tournaments", "match.completed");
            }

            _channel.BasicPublish("tournaments", "match.completed", null, body);
        }

        return Task.CompletedTask;
    }
}