using System.Collections.ObjectModel;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Azure;

namespace CloudWorld.AI.Series.Search.Features.Search.Index.CreateIndex;

public record CreateIndexFields(
    string FieldName,
    string FieldType,
    bool IsKey = false,
    bool IsSearchable = false,
    bool IsFilterable = false,
    bool IsSortable = false);

public record CreateIndexRequest(string IndexName, List<CreateIndexFields> Fields);
public record CreateIndexResponse(string IndexName);
public record CreateIndexCommand(CreateIndexRequest Command) : IRequest<CreateIndexResponse>;

public class CreateIndexHandler(IAzureClientFactory<SearchIndexClient> azureClientFactory) : IRequestHandler<CreateIndexCommand, CreateIndexResponse>
{
    public async Task<CreateIndexResponse> Handle(CreateIndexCommand request, CancellationToken cancellationToken)
    {
        var searchIndexClient = azureClientFactory.CreateClient("SearchServiceIndex");

        var searchFields = new Collection<SearchField>();
        
        request.Command.Fields.ForEach(field =>
        {
            var searchField = new SearchField(field.FieldName, field.FieldType)
            {
                IsKey = field.IsKey,
                IsSortable = field.IsSortable
            };
            
            searchFields.Add(searchField);
        });

        var indexDef = new SearchIndex(request.Command.IndexName)
        {
            Fields = searchFields
        };

        var response = await searchIndexClient.CreateOrUpdateIndexAsync(indexDef, cancellationToken: cancellationToken);
        
        return new CreateIndexResponse(response.Value.Name);
    }
}

public static class CreateIndexEndpoint
{
    public static IEndpointRouteBuilder AddCreateIndexEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/index", async (
                [FromServices] IMediator mediator,
                [FromBody] CreateIndexRequest request) =>
            {
                var response = await mediator.Send(new CreateIndexCommand(request));
                return Results.Ok(response);
            })
            .WithName("CreateIndex")
            .Produces<CreateIndexResponse>()
            .Produces(StatusCodes.Status400BadRequest)
            .WithOpenApi();

        return endpoints;
    }
}