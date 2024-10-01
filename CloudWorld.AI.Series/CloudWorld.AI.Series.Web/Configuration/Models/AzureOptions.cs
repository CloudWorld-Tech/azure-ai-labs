namespace CloudWorld.AI.Series.Web.Configuration.Models;

public record AzureOptions
{
    public required string Endpoint { get; init; }
}

public record ComputerVisionOptions : AzureOptions;