using Azure.AI.Vision.ImageAnalysis;
using MediatR;
using Microsoft.Extensions.Azure;

namespace CloudWorld.AI.Series.Web.Features.ComputerVision.ImageAnalysis;

public record ImageAnalysisCommand(ImageAnalysisRequest AnalysisRequest) : IRequest<ImageAnalysisResponse>;

public class ImageAnalysisRequest
{
    public string? ImageUrl { get; set; }
}

public record ImageAnalysisResponse(List<ImageAnalysisResponseTag> Tags, List<ImageAnalysisResponseCaption> Captions);

public record ImageAnalysisResponseTag(string Name, float Confidence);

public record ImageAnalysisResponseCaption(string Text, float Confidence);

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

        var response = await client.AnalyzeAsync(
            imageUri,
            visualFeatures,
            options, cancellationToken);

        var result = response.Value;
        var tags = new List<ImageAnalysisResponseTag>();

        result.Tags.Values.ToList()
            .ForEach(obj => { tags.Add(new ImageAnalysisResponseTag(obj.Name, obj.Confidence)); });

        var captions = new List<ImageAnalysisResponseCaption>
        {
            new(result.Caption.Text, result.Caption.Confidence)
        };

        result.DenseCaptions.Values.ToList()
            .ForEach(obj => { captions.Add(new ImageAnalysisResponseCaption(obj.Text, obj.Confidence)); });
        
        return new ImageAnalysisResponse(tags, captions);
    }
}