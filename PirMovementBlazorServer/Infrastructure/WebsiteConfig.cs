namespace PirMovementBlazorServer.Infrastructure;

public class WebsiteConfig
{
    public string Url { get; set; } = default!;
    private readonly IConfiguration _configuration;
    public const string SectionName = "Website";
    public WebsiteConfig(IConfiguration configuration)
    {
        _configuration = configuration;
        _configuration.GetSection(SectionName).Bind(this);
    }
}
