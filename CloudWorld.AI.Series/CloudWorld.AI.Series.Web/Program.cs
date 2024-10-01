using Azure.Identity;
using CloudWorld.AI.Series.Web.Components;
using CloudWorld.AI.Series.Web.Configuration.Models;
using Microsoft.Extensions.Azure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();


builder.Services.AddOptions<ComputerVisionOptions>()
    .Bind(builder.Configuration.GetSection("AzureVision"));

builder.Services.AddMediatR(configuration =>
{
    configuration.RegisterServicesFromAssemblies(typeof(Program).Assembly);
});

builder.Services.AddAzureClients(factoryBuilder =>
{
    var azureVision = builder.Configuration.GetSection("AzureVision").Get<ComputerVisionOptions>();
    factoryBuilder.AddImageAnalysisClient(new Uri(azureVision.Endpoint))
        .WithName("AzureComputerVisionClient")
        .WithCredential(new DefaultAzureCredential());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();