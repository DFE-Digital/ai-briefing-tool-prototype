# Briefing tool prototype

This project is a prototype for design and user research into Use Case 21 - Briefing tool. This is intended to be a simple project used to call the DfE Azure AI sandbox to produce draft briefings 

This project will not be used in production and will not comply with coding standards other than security

## Project Scope
This prototype supports the investigation of an “AI Briefing Tool” for Use Case 21. Its primary role is to test integration with Azure OpenAI and understand the feasibility, UI/UX, and functional behaviours of an AI‑driven briefing assistant.

## Rules

* Only public or mock data may be used in this repository or sent to Azure AI
* The prototype must not send sensitive data to the Azure AI service.

## Requirements & Setup

### Prerequisites
Before you can build and run the project you need:
- **.NET 10.0 SDK** — suitable for building and running both Frontend and API.
- **Azure OpenAI API key** authorised to access the _Use Case 21 AI model_.
- **Docker & Docker Compose** — for development and containerised execution if required.

### Configuration
Before running the application, you need to set up **user secrets** locally. The prototype expects Azure services and authentication keys to be configured. 
```bash
dotnet user-secrets set "AzureSettings:AzureOpenaiEndpoint" "[your-endpoint-here]"
dotnet user-secrets set "AzureSettings:AzureOpenaiKey" "[your-key-here]"
dotnet user-secrets set "AzureSettings:AzureEmbeddingEndpoint" "[your-endpoint-here]"
dotnet user-secrets set "AzureSettings:AzureEmbeddingKey" "[your-key-here]"
dotnet user-secrets set "AzureSettings:AzureOpenaiDeployment" "[your-deployment-name-here]"
dotnet user-secrets set "AzureSettings:AzureSearchEndpoint" "[your-search-endpoint-here]"
dotnet user-secrets set "AzureSettings:AzureSearchKey" "[your-search-key-here]"
dotnet user-secrets set "AzureSettings:AzureSearchIndex" "[your-search-index-here]"

dotnet user-secrets set "AzureAd:Instance" "https://login.microsoftonline.com/"
dotnet user-secrets set "AzureAd:Domain" "[your-tenant-domain-here]"
dotnet user-secrets set "AzureAd:ClientID" "[your-client-id-here]"
dotnet user-secrets set "AzureAd:TenantID" "[your-tenant-id-here]"
dotnet user-secrets set "AzureAd:CallbackPath" "/signin-oidc"

dotnet user-secrets set "AuthenticationConfig:ApiKeys:0" "api-key"
```

## Running via docker

TODO
