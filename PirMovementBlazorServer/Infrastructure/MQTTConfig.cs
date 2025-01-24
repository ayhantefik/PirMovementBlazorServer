namespace PirMovementBlazorServer.Infrastructure;

public class MQTTConfig
{
    public string BrokerHost { get; set; } = default!;
    public string Port { get; set; } = default!;
    private readonly IConfiguration _configuration;
    public const string SectionName = "MQTT";
    public MQTTConfig(IConfiguration configuration)
    {
        _configuration = configuration;
        _configuration.GetSection(SectionName).Bind(this);
    }
}
