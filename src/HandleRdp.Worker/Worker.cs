using System.Messaging;
using System.Text.Json;
using HandleRdp.Worker.Messaging;
using HandleRdp.Worker.Models;
using Microsoft.Extensions.Options;

namespace HandleRdp.Worker;

// Client B: dequeues requests from WebService, processes them, and replies.
public class Worker : BackgroundService
{
    private readonly MsmqOptions _options;
    private readonly ILogger<Worker> _logger;

    public Worker(IOptions<MsmqOptions> options, ILogger<Worker> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!MessageQueue.Exists(_options.RequestQueuePath))
        {
            MessageQueue.Create(_options.RequestQueuePath);
        }
        if (!MessageQueue.Exists(_options.ResponseQueuePath))
        {
            MessageQueue.Create(_options.ResponseQueuePath);
        }

        using var requestQueue = new MessageQueue(_options.RequestQueuePath)
        {
            Formatter = new XmlMessageFormatter(new[] { typeof(string) })
        };
        using var responseQueue = new MessageQueue(_options.ResponseQueuePath)
        {
            Formatter = new XmlMessageFormatter(new[] { typeof(string) })
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            Message message;
            try
            {
                // Short poll timeout so we keep checking stoppingToken instead of
                // blocking forever on Receive().
                message = await Task.Run(() => requestQueue.Receive(TimeSpan.FromSeconds(1)), stoppingToken);
            }
            catch (MessageQueueException ex) when (ex.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
            {
                continue;
            }
            catch (OperationCanceledException)
            {
                break;
            }

            var response = HandleMessage(message);

            var reply = new Message(JsonSerializer.Serialize(response))
            {
                CorrelationId = message.Id
            };
            responseQueue.Send(reply, MessageQueueTransactionType.Single);
        }
    }

    private ProcessResponse HandleMessage(Message message)
    {
        try
        {
            var body = (string)message.Body;
            var request = JsonSerializer.Deserialize<ProcessRequest>(body)
                ?? throw new InvalidOperationException("Empty request body.");

            var result = Process(request.Payload);
            return new ProcessResponse(true, result, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process MSMQ request message {MessageId}", message.Id);
            return new ProcessResponse(false, null, ex.Message);
        }
    }

    // TODO: replace with the actual RDP handling logic. This is a placeholder
    // so the request/reply plumbing can be exercised end to end.
    private string Process(string payload)
    {
        _logger.LogInformation("Processing payload: {Payload}", payload);
        return $"processed:{payload}";
    }
}
