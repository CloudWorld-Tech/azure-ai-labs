using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Azure;

namespace CloudWorld.AI.Series.Search.Features.Search.SkillSet;

public record CreateSkillSetRequest(string SkillSetName, string Description);
public record CreateSkillSetResponse(string SkillSetName, string Description);

public record CreateSkillSetCommand(CreateSkillSetRequest Command) : IRequest<CreateSkillSetResponse>;

public class CreateSkillSetHandler(IAzureClientFactory<SearchIndexerClient> azureClientFactory) : IRequestHandler<CreateSkillSetCommand, CreateSkillSetResponse>
{
    public async Task<CreateSkillSetResponse> Handle(CreateSkillSetCommand request, CancellationToken cancellationToken)
    {
        List<InputFieldMappingEntry> inputMappingsOcr =
        [
            new("image")
            {
                Source = "/document/normalized_images/*"
            }
        ];

        List<OutputFieldMappingEntry> outputMappingsOcr =
        [
            new("text")
            {
                TargetName = "text"
            }
        ];

        var ocrSkill = new OcrSkill(inputMappingsOcr, outputMappingsOcr)
        {
            Description = "Extract Text from images on the data source",
            Context = "/document/normalized_images/*",
            DefaultLanguageCode = OcrSkillLanguage.En,
            ShouldDetectOrientation = true
        };
        
        var inputMappingsMerge = new List<InputFieldMappingEntry>
        {
            new("text")
            {
                Source = "/document/content"
            },
            new("itemsToInsert")
            {
                Source = "/document/normalized_images/*/text"
            },
            new("offsets")
            {
                Source = "/document/normalized_images/*/contentOffset"
            }
        };
        
        List<OutputFieldMappingEntry> outputMappingsMerge =
        [
            new("mergedText")
            {
                TargetName = "merged_text"
            }
        ];
        
        var mergeSkill = new MergeSkill(inputMappingsMerge, outputMappingsMerge)
        {
            Description =
                "Create merged_text with the OCR Input",
            Context = "/document",
            InsertPreTag = " ",
            InsertPostTag = " "
        };
        
        var inputMappingsLanguage = new List<InputFieldMappingEntry>
        {
            new("text")
            {
                Source = "/document/merged_text"
            }
        };

        List<OutputFieldMappingEntry> outputMappingsLanguage =
        [
            new("languageCode")
            {
                TargetName = "languageCode"
            }
        ];
        
        var languageDetectionSkill = new LanguageDetectionSkill(inputMappingsLanguage, outputMappingsLanguage)
        {
            Description = "Detect the language used in the document",
            Context = "/document"
        };
        
        List<InputFieldMappingEntry> inputMappingsSplit =
        [
            new("text")
            {
                Source = "/document/merged_text"
            },

            new("languageCode")
            {
                Source = "/document/languageCode"
            }
        ];

        var outputMappingsSplit = new List<OutputFieldMappingEntry>
        {
            new("textItems")
            {
                TargetName = "pages"
            }
        };
        
        var splitSkill = new SplitSkill(inputMappingsSplit, outputMappingsSplit)
        {
            Description = "Split content into pages",
            Context = "/document",
            TextSplitMode = TextSplitMode.Pages,
            MaximumPageLength = 4000,
            DefaultLanguageCode = SplitSkillLanguage.En
        };
        
        List<InputFieldMappingEntry> inputMappingsEntity =
        [
            new("text")
            {
                Source = "/document/pages/*"
            }
        ];

        List<OutputFieldMappingEntry> outputMappingsEntity =
        [
            new("organizations")
            {
                TargetName = "organizations"
            }
        ];

        var entityRecognitionSkill =
            new EntityRecognitionSkill(inputMappingsEntity, outputMappingsEntity,
                EntityRecognitionSkill.SkillVersion.V3)
            {
                Description = "Recognize organizations",
                Context = "/document/pages/*",
                DefaultLanguageCode = EntityRecognitionSkillLanguage.En
            };
        
        var inputMappingsKeyPhrase = new List<InputFieldMappingEntry>
        {
            new("text") { Source = "/document/pages/*" }
        };

        var outputMappingsKeyPhrase = new List<OutputFieldMappingEntry>
        {
            new("keyPhrases") { TargetName = "keyPhrases" }
        };
        
        var keyPhraseSkill =new KeyPhraseExtractionSkill(inputMappingsKeyPhrase, outputMappingsKeyPhrase)
        {
            Description = "Extract key phrases",
            Context = "/document/pages/*",
            DefaultLanguageCode = KeyPhraseExtractionSkillLanguage.En
        };
        
        var searchIndexerSkillSet =
            new SearchIndexerSkillset(request.Command.SkillSetName,
                new List<SearchIndexerSkill>
                    { ocrSkill, mergeSkill, languageDetectionSkill, splitSkill, entityRecognitionSkill, keyPhraseSkill })
            {
                Description = request.Command.Description
            };

        
        var searchIndexerClient = azureClientFactory.CreateClient("SearchServiceIndexer");

        var response = await searchIndexerClient.CreateOrUpdateSkillsetAsync(searchIndexerSkillSet,
            cancellationToken: cancellationToken);
        
        return new CreateSkillSetResponse(response.Value.Name, response.Value.Description);
    }
}

public static class CreateSkillSetEndpoint
{
    public static IEndpointRouteBuilder AddCreateSkillSetEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/skillset", async (
                [FromServices] IMediator mediator,
                [FromBody] CreateSkillSetRequest request) =>
            {
                var response = await mediator.Send(new CreateSkillSetCommand(request));
                return Results.Ok(response);
            })
            .WithName("CreateSkillSet")
            .Produces<CreateSkillSetResponse>()
            .Produces(StatusCodes.Status400BadRequest)
            .WithOpenApi();

        return endpoints;
    }
}