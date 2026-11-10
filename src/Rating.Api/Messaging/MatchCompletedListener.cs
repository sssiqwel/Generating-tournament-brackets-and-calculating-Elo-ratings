using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Rating.Api.Rating;

namespace Rating.Api.Messaging;

public sealed class MatchCompletedListener(IServiceScopeFactory scopes, IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Listen(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task Listen(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"] ?? "localhost",
            UserName = configuration["RabbitMQ:User"] ?? "tournament",
            Password = configuration["RabbitMQ:Password"] ?? "tournament"
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        channel.ExchangeDeclare("tournaments", ExchangeType.Topic, durable: true);
        channel.QueueDeclare("match-completed", durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind("match-completed", "tournaments", "match.completed");
        channel.BasicQos(0, 1, false);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (_, args) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(args.Body.ToArray());
                var message = JsonSerializer.Deserialize<MatchCompleted>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (message is not null)
                {
                    using var scope = scopes.CreateScope();
                    var rating = scope.ServiceProvider.GetRequiredService<RatingService>();
                    rating.Apply(message).GetAwaiter().GetResult();
                }

                channel.BasicAck(args.DeliveryTag, false);
            }
            catch
            {
                channel.BasicNack(args.DeliveryTag, false, false);
            }
        };

        channel.BasicConsume("match-completed", autoAck: false, consumer);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}