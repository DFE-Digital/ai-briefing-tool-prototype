using Azure.Core.Diagnostics;
using BriefingTool.Builders;
using BriefingTool.Builders.Interfaces;
using BriefingTool.Config;
using BriefingTool.Constants;
using BriefingTool.Indexers;
using BriefingTool.Indexers.Interfaces;
using BriefingTool.Mcp.Factories;
using BriefingTool.Retrievers;
using BriefingTool.Retrievers.Interfaces;
using BriefingTool.Runners;
using BriefingTool.Runners.Interfaces;
using BriefingTool.Services;
using BriefingTool.Services.Interfaces;
using GovUk.Frontend.AspNetCore;
using Microsoft.AspNetCore.HttpOverrides; 

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddGovUkFrontend();

builder.Services.AddScoped<IBasePromptRetriever, BasePromptRetriever>();
builder.Services.AddScoped<IConcernsPromptRetriever, ConcernsPromptRetriever>();

builder.Services.AddScoped<IAcademyInformationRetriever, AcademyInformationRetriever>();
builder.Services.AddScoped<IOfstedPromptRetriever, OfstedPromptRetriever>();
builder.Services.AddScoped<IOfstedSummaryPromptRetriever, OfstedSummaryPromptRetriever>();
builder.Services.AddScoped<IConcernsInformationRetriever, ConcernsInformationRetriever>();
builder.Services.AddScoped<IAzureSearchService, AzureSearchService>(); 
builder.Services.AddScoped<IConcernsInformationRetriever, ConcernsInformationRetriever>();
builder.Services.AddKeyedScoped<IBriefingRunner, BriefingRunner>(RunnerServiceType.Mcp);
builder.Services.AddKeyedScoped<IBriefingRunner, AgentBriefingRunner>(RunnerServiceType.Agent);
builder.Services.AddKeyedScoped<IBriefingRunner, AgentAssistantBriefingRunner>(RunnerServiceType.AgentAssistant);
builder.Services.AddKeyedScoped<IBriefingRunner, SingleSourceBriefingRunner>(RunnerServiceType.SingleDataSource);
builder.Services.AddScoped<IPromptBuilder, PromptBuilder>();
builder.Services.AddScoped<IAzureOpenAIService, AzureOpenAIService>();
builder.Services.AddScoped<IMcpClientFactory, McpClientFactory>();
builder.Services.AddScoped<ITokenService, TokenService>();


// Configurations
builder.Services.AddOptions<AuthenticationConfig>();
var apiKeysConfiguration = builder.Configuration.GetSection("AuthenticationConfig");
builder.Services.Configure<AuthenticationConfig>(apiKeysConfiguration);

var azureSettings = builder.Configuration.GetSection("AzureSettings").Get<AzureSettings>()
    ?? throw new InvalidOperationException("AzureSettings section is missing!");
builder.Services.AddSingleton(azureSettings);
var authConfig = builder.Configuration.GetSection("AuthenticationConfig").Get<AuthenticationConfig>()
    ?? throw new InvalidOperationException("AuthenticationConfig section is missing!");
builder.Services.AddSingleton(authConfig);
var mcpClientConfig = builder.Configuration.GetSection("Mcp:Client").Get<McpClientConfig>()
    ?? throw new InvalidOperationException("Mcp Client section is missing!");
builder.Services.AddSingleton(mcpClientConfig);
var azureAdConfig = builder.Configuration.GetSection("Mcp:AzureAd").Get<AzureAdConfig>()
    ?? throw new InvalidOperationException("Mcp Azure AD section is missing!");
builder.Services.AddSingleton(azureAdConfig);

 

builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Trace);

AzureEventSourceListener.CreateTraceLogger();

SecurityServicesSetup.AddSecurityServices(builder);

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseGovUkFrontend();

app.UseHttpsRedirection();

app.MapControllers();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

await app.RunAsync();
