using Microsoft.AspNetCore.Mvc;
using Dapper;
using PirMovementBlazorServer.Connection;
using System.Data;
using MQTTnet;
using MQTTnet.Protocol;
using System.Text;
using PirMovementBlazorServer.Infrastructure;

namespace PirMovementBlazorServer.Controllers
{
    // Value Controller. Get and Update data for Light and Sound in db.

    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private const string sqlGetValue = "SELECT Value FROM pirmodules WHERE Name = @ModuleName";
        private const string sqlUpdateValue = "UPDATE pirmodules SET Value = @Value WHERE Name = @ModuleName";
        private readonly DbConnectionFactory _connectionFactory;
        private readonly IConfiguration _config;
        public ValuesController(DbConnectionFactory connectionFactory, IConfiguration config)
        {
            _connectionFactory = connectionFactory;
            _config = config;
        }

        // GET api/<ValuesController>/5
        [HttpGet("{moduleName}")]
        public async Task<IActionResult> Get(string moduleName)
        {
            var dynamicParameters = new DynamicParameters();
            dynamicParameters.Add("@ModuleName", moduleName, DbType.String);
            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.OpenAsync();
                await using (var transaction = await connection.BeginTransactionAsync())
                {
                    int? valueResult = await connection.QuerySingleOrDefaultAsync<int>(sqlGetValue, new { ModuleName = moduleName });

                    if (valueResult.HasValue)
                    {
                        return Ok(valueResult.Value);
                    }
                    else
                    {
                        return NotFound($"ModuleName '{moduleName}' not found.");
                    }
                }
            }
        }

        // PUT api/<ValuesController>/5
        [HttpPut("{moduleName}")]
        public async Task Put(string moduleName, [FromBody] int value)
        {
            // MQTT
            var mqttConfig = new MQTTConfig(_config);

            string broker = $"{mqttConfig.BrokerHost}";
            int port = int.Parse(mqttConfig.Port);

            string clientId = Guid.NewGuid().ToString();
            string topic = $"/sensor/arduino/pir";

            MqttClientFactory mqttFactory = new MqttClientFactory();

            var mqttClient = mqttFactory.CreateMqttClient();

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(broker, port) // MQTT broker address and port
                .WithClientId(clientId)
                .WithCleanSession()
                .Build();

            // Connect to MQTT broker
            var connectResult = await mqttClient.ConnectAsync(options);

            if (connectResult.ResultCode == MqttClientConnectResultCode.Success)
            {
                // Subscribe to a topic
                await mqttClient.SubscribeAsync(topic);

                // Callback function when a message is received
                mqttClient.ApplicationMessageReceivedAsync += e =>
                {
                    Console.WriteLine($"Received message: {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
                    return Task.CompletedTask;
                };

                // Publish a message

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload($"{moduleName}{value}")
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .WithRetainFlag()
                    .Build();

                await mqttClient.PublishAsync(message);
                

                // Unsubscribe and disconnect
                await mqttClient.UnsubscribeAsync(topic);
                await mqttClient.DisconnectAsync();
            }
            else
            {
                Console.WriteLine($"Failed to connect to MQTT broker: {connectResult.ResultCode}");
            }

            // Update Database

            var dynamicParameters = new DynamicParameters();
            dynamicParameters.Add("@ModuleName", moduleName, DbType.String);
            dynamicParameters.Add("@Value", value, DbType.Int32);

            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.OpenAsync();
                await using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        await connection.ExecuteAsync(sqlUpdateValue, dynamicParameters, transaction);
                        await transaction.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }
    }
}
