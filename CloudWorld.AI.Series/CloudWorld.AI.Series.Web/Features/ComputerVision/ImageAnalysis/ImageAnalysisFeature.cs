using Azure.AI.Vision.ImageAnalysis;
using MediatR;
using Microsoft.Extensions.Azure;

namespace CloudWorld.AI.Series.Web.Features.ComputerVision.ImageAnalysis;

public record ImageAnalysisCommand(ImageAnalysisRequest AnalysisRequest) : IRequest<ImageAnalysisResponse>;

public class ImageAnalysisRequest
{
    public string? ImageUrl { get; set; }
}

public record ImageAnalysisResponse(List<string> Tags);

public class ImageAnalysisHandler(IAzureClientFactory<ImageAnalysisClient> imageAnalysisClientFactory)
    : IRequestHandler<ImageAnalysisCommand, ImageAnalysisResponse>
{
    public async Task<ImageAnalysisResponse> Handle(ImageAnalysisCommand request, CancellationToken cancellationToken)
    {
        var client = imageAnalysisClientFactory.CreateClient("AzureComputerVisionClient");

        var imageUri = new Uri(request.AnalysisRequest.ImageUrl);

        const VisualFeatures visualFeatures = VisualFeatures.Caption |
                                              VisualFeatures.DenseCaptions |
                                              VisualFeatures.Objects |
                                              VisualFeatures.Read |
                                              VisualFeatures.Tags |
                                              VisualFeatures.People |
                                              VisualFeatures.SmartCrops;

        var options = new ImageAnalysisOptions
        {
            GenderNeutralCaption = false,
            Language = "en",
            SmartCropsAspectRatios = [0.9F, 1.33F]
        };

        var result = await client.AnalyzeAsync(
            imageUri,
            visualFeatures,
            options, cancellationToken);

        var tags = new List<string>();

        result.Value.Objects.Values.ToList().ForEach(obj => { tags.AddRange(obj.Tags.Select(x => x.Name)); });

        return new ImageAnalysisResponse(tags);
    }
}