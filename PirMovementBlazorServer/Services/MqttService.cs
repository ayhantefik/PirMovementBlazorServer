namespace PirMovementBlazorServer.Services;

using MQTTnet;
using PirMovementBlazorServer.Infrastructure;
using PirMovementBlazorServer.Models;
using System.Text;

// Background task for MQTT Broker Connection

public class MqttService : IHostedService, IDisposable
{
    private readonly IConfiguration _config;
    private Timer? _timer = null;

    public MqttService(IConfiguration config)
    {
        _config = config;
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("MQTT Service running..");

        _timer = new Timer(async state => await DoWork(state), null, TimeSpan.Zero,
            TimeSpan.FromHours(1));

        await Task.CompletedTask;
    }

    private async Task DoWork(object? state)
    {
        var mqttConfig = new MQTTConfig(_config);
        Console.WriteLine("MQTT Background Service active");

        string broker = $"{mqttConfig.BrokerHost}";
        int port = int.Parse(mqttConfig.Port);

        string clientId = Guid.NewGuid().ToString();

        MqttClientFactory mqttFactory = new MqttClientFactory();

        var mqttClient = mqttFactory.CreateMqttClient();
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(broker, port)
            .WithClientId(clientId)
            .WithCleanSession()
        .Build();

        var connectResult = await mqttClient.ConnectAsync(options);

        if (connectResult.ResultCode == MqttClientConnectResultCode.Success)
        {
            Console.WriteLine("MQTT connected");
            // Subscribe to a topic
            await mqttClient.SubscribeAsync("/pir/movement");

            // Callback function when a message is received
            mqttClient.ApplicationMessageReceivedAsync += async e =>
            {
                try
                {
                    HttpClient httpClient = new HttpClient();
                    var websiteConfig = new WebsiteConfig(_config);
                    MovementValue newMovement = new MovementValue { Value = "1" };
                    var response = await httpClient.PostAsJsonAsync($"{websiteConfig.Url}api/movements", newMovement);
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Pir movement sent to Database");
                    }
                    else
                    {
                        Console.WriteLine("Error in submission");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex.Message}");
                }
                Console.WriteLine($"Received message: {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");

            };
        }

    }
    public Task StopAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("MQTT disconnected");
        _timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }
    public void Dispose()
    {
        _timer?.Dispose();
    }
}
