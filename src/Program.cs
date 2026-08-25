using Azure.Core.Diagnostics;
using BriefingTool.Builders;
using BriefingTool.Builders.Interfaces;
using BriefingTool.Config;
using BriefingTool.Constants;
using BriefingTool.Factories;
using BriefingTool.FileRetrievers;
using BriefingTool.FileRetrievers.Interfaces;
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
 
builder.Services.AddScoped<IAcademyInformationRetriever, AcademyInformationRetriever>();
builder.Services.AddScoped<IConcernsInformationRetriever, ConcernsInformationRetriever>();
builder.Services.AddScoped<IAzureSearchService, AzureSearchService>(); 
builder.Services.AddScoped<IConcernsInformationRetriever, ConcernsInformationRetriever>();
builder.Services.AddKeyedScoped<IBriefingRunner, McpBriefingRunner>(RunnerServiceType.Mcp);
builder.Services.AddScoped<IDatabricksQueryBriefingRunner, DatabricksQueryBriefingRunner>();
builder.Services.AddKeyedScoped<IBriefingRunner, AgentBriefingRunner>(RunnerServiceType.Agent);
builder.Services.AddKeyedScoped<IBriefingRunner, FoundryHostedAgentBriefingRunner>(RunnerServiceType.FoundryHostedAgent);
builder.Services.AddKeyedScoped<IBriefingRunner, ClaudeFoundryBriefingRunner>(RunnerServiceType.ClaudeFoundry);
builder.Services.AddKeyedScoped<IBriefingRunner, SingleSourceBriefingRunner>(RunnerServiceType.SingleDataSource);
builder.Services.AddScoped<IPromptBuilder, PromptBuilder>();
builder.Services.AddScoped<IAzureOpenAIService, AzureOpenAIService>();
builder.Services.AddScoped<IMcpClientFactory, McpClientFactory>();
builder.Services.AddScoped<IClaudeClientFactory, ClaudeClientFactory>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPromptFileReader, PromptFileReader>(); 
builder.Services.AddScoped<IPromptRetrieverService, PromptRetrieverService>();

// Configurations
builder.Services.AddOptions<AuthenticationConfig>();
var apiKeysConfiguration = builder.Configuration.GetSection("AuthenticationConfig");
builder.Services.Configure<AuthenticationConfig>(apiKeysConfiguration);

var azureFoundry = builder.Configuration.GetSection("AzureFoundry").Get<AzureFoundryConfig>()
    ?? throw new InvalidOperationException("AzureFoundry section is missing!");
builder.Services.AddSingleton(azureFoundry);
var fauAPI = builder.Configuration.GetSection("FauAPI").Get<FauAPIConfig>()
    ?? throw new InvalidOperationException("FauAPI section is missing!");
builder.Services.AddSingleton(fauAPI);
var authConfig = builder.Configuration.GetSection("AuthenticationConfig").Get<AuthenticationConfig>()
    ?? throw new InvalidOperationException("AuthenticationConfig section is missing!");
builder.Services.AddSingleton(authConfig);
var mcpClientConfig = builder.Configuration.GetSection("Mcp:Client").Get<McpClientConfig>()
    ?? throw new InvalidOperationException("Mcp Client section is missing!");
builder.Services.AddSingleton(mcpClientConfig);
var azureAdConfig = builder.Configuration.GetSection("Mcp:AzureAd").Get<AzureAdConfig>()
    ?? throw new InvalidOperationException("Mcp Azure AD section is missing!");
builder.Services.AddSingleton(azureAdConfig);

var foundryHostedAgentConfig = builder.Configuration.GetSection("FoundryHostedAgent").Get<FoundryHostedAgentConfig>()
    ?? throw new InvalidOperationException("Foundry Hosted Agent section is missing!");
builder.Services.AddSingleton(foundryHostedAgentConfig);

var promptFiles = builder.Configuration.GetSection("PromptFiles").Get<PromptConfig>()
            ?? throw new InvalidOperationException("Prompt files section is missing!"); 
builder.Services.AddSingleton(promptFiles);

var claudeFoundryConfig = builder.Configuration.GetSection("ClaudeFoundry").Get<ClaudeFoundryConfig>()
    ?? throw new InvalidOperationException("Claude Foundry section is missing!");
builder.Services.AddSingleton(claudeFoundryConfig);

var azureSearchConfig = builder.Configuration.GetSection("AzureSearch").Get<AzureSearchConfig>()
    ?? throw new InvalidOperationException("Azure Search section is missing!");
builder.Services.AddSingleton(azureSearchConfig);

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
