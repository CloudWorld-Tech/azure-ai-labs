using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Azure;

namespace CloudWorld.AI.Series.Search.Features.Search.Indexer.CreateIndexer;

public record CreateIndexerRequest(
    string IndexerName,
    string Description,
    string DataSourceName,
    string IndexName,
    string SkillSetName,
    int MaxFailedItems,
    int MaxFailedItemsPerBatch);

public record CreateIndexerResponse(string IndexerName);

public record CreateIndexerCommand(CreateIndexerRequest Command) : IRequest<CreateIndexerResponse>;

public class CreateIndexerHandler(IAzureClientFactory<SearchIndexerClient> azureClientFactory)
    : IRequestHandler<CreateIndexerCommand, CreateIndexerResponse>
{
    public async Task<CreateIndexerResponse> Handle(CreateIndexerCommand request, CancellationToken cancellationToken)
    {
        var searchIndexerClient = azureClientFactory.CreateClient("SearchServiceIndexer");

        var indexingParameters = new IndexingParameters
        {
            MaxFailedItems = request.Command.MaxFailedItems,
            MaxFailedItemsPerBatch = request.Command.MaxFailedItemsPerBatch
        };
        
        indexingParameters.Configuration.Add("dataToExtract", "contentAndMetadata");
        indexingParameters.Configuration.Add("imageAction", "generateNormalizedImages");

        var indexer = new SearchIndexer(request.Command.IndexerName, request.Command.DataSourceName,
            request.Command.IndexName)
        {
            Description = request.Command.Description,
            SkillsetName = request.Command.SkillSetName,
            IsDisabled = false,
            Parameters = indexingParameters
        };
        
        var mappingFunction = new FieldMappingFunction("base64Encode");
        
        indexer.FieldMappings.Add(new FieldMapping("metadata_storage_path")
        {
            TargetFieldName = "id",
            MappingFunction = mappingFunction
        });
        
        indexer.FieldMappings.Add(new FieldMapping("content")
        {
            TargetFieldName = "content"
        });
        
        indexer.OutputFieldMappings.Add(new FieldMapping("/document/pages/*/organizations/*")
        {
            TargetFieldName = "organizations"
        });

        indexer.OutputFieldMappings.Add(new FieldMapping("/document/pages/*/keyPhrases/*")
        {
            TargetFieldName = "keyPhrases"
        });

        indexer.OutputFieldMappings.Add(new FieldMapping("/document/languageCode")
        {
            TargetFieldName = "languageCode"
        });

        var response =
            await searchIndexerClient.CreateOrUpdateIndexerAsync(indexer, cancellationToken: cancellationToken);
        
        return new CreateIndexerResponse(response.Value.Name);
    }
}

public static class CreateIndexerEndpoint
{
    public static IEndpointRouteBuilder AddCreateIndexerEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/indexer", async (
                [FromServices] IMediator mediator,
                [FromBody] CreateIndexerRequest request) =>
            {
                var response = await mediator.Send(new CreateIndexerCommand(request));
                return Results.Ok(response);
            })
            .WithName("CreateIndexer")
            .Produces<CreateIndexerResponse>()
            .Produces(StatusCodes.Status400BadRequest)
            .WithOpenApi();

        return endpoints;
    }
}