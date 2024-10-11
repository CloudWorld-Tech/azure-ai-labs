using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Azure;

namespace CloudWorld.AI.Series.Search.Features.Search.DataSource.CreateDataSource;

public record CreateDataSourceRequest(
    string DataStorageName,
    string DataSourceType,
    string StorageId,
    string ContainerName);

public record CreateDataSourceResponse(string DataStorageName, string DataSourceType);

public record CreateDataSourceCommand(CreateDataSourceRequest Command)
    : IRequest<CreateDataSourceResponse>;

public class CreateDataSourceHandler(IAzureClientFactory<SearchIndexerClient> azureClientFactory)
    : IRequestHandler<CreateDataSourceCommand, CreateDataSourceResponse>
{
    public async Task<CreateDataSourceResponse> Handle(CreateDataSourceCommand request,
        CancellationToken cancellationToken)
    {
        var searchIndexerClient = azureClientFactory.CreateClient("SearchServiceIndexer");
        var dataSourceConnection = new SearchIndexerDataSourceConnection(request.Command.DataStorageName,
            request.Command.DataSourceType,
            request.Command.StorageId,
            new SearchIndexerDataContainer(request.Command.ContainerName));

        var response =
            await searchIndexerClient.CreateOrUpdateDataSourceConnectionAsync(dataSourceConnection,
                cancellationToken: cancellationToken);
        
        return new CreateDataSourceResponse(response.Value.Name, response.Value.Type.ToString());
    }
}

public static class CreateDataSourceEndpoint
{
    public static IEndpointRouteBuilder AddCreateDataSourceEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/datasource", async (
                [FromServices] IMediator mediator,
                [FromBody] CreateDataSourceRequest request) =>
            {
                var response = await mediator.Send(new CreateDataSourceCommand(request));
                return Results.Ok(response);
            })
            .WithName("CreateDataSource")
            .Produces<CreateDataSourceResponse>()
            .Produces(StatusCodes.Status400BadRequest)
            .WithOpenApi();

        return endpoints;
    }
}