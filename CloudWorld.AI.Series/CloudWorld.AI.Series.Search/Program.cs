using Azure.Identity;
using CloudWorld.AI.Series.Search.Features.Search.DataSource.CreateDataSource;
using CloudWorld.AI.Series.Search.Features.Search.Index.CreateIndex;
using CloudWorld.AI.Series.Search.Features.Search.Indexer.CreateIndexer;
using CloudWorld.AI.Series.Search.Features.Search.SkillSet;
using Microsoft.Extensions.Azure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMediatR(configuration =>
{
    configuration.RegisterServicesFromAssemblies(typeof(CreateDataSourceHandler).Assembly);
});

builder.Services.AddAzureClients(azureClientFactoryBuilder =>
{
    azureClientFactoryBuilder.AddSearchIndexerClient(builder.Configuration.GetRequiredSection("SearchService"))
        .WithCredential(new DefaultAzureCredential())
        .WithName("SearchServiceIndexer");
    
    azureClientFactoryBuilder.AddSearchIndexClient(builder.Configuration.GetRequiredSection("SearchService"))
        .WithCredential(new DefaultAzureCredential())
        .WithName("SearchServiceIndex");
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapGroup("api/search").AddCreateDataSourceEndpoint();
app.MapGroup("api/skillset").AddCreateSkillSetEndpoint();
app.MapGroup("api/index").AddCreateIndexEndpoint();
app.MapGroup("api/indexer").AddCreateIndexerEndpoint();
app.Run();