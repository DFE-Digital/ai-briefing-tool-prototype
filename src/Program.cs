using BriefingTool.Config;
using BriefingTool.Services;
using DfE.FindInformationAcademiesTrusts.Setup;
using GovUk.Frontend.AspNetCore;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddGovUkFrontend(options => options.Rebrand = true);

builder.Services.AddScoped<IBasePromptRetriever, BasePromptRetriever>();
builder.Services.AddScoped<IConcernsPromptRetriever, ConcernsPromptRetriever>();

builder.Services.AddScoped<IAcademyInformationRetriever, AcademyInformationRetriever>();
builder.Services.AddScoped<IOfstedPromptRetriever, OfstedPromptRetriever>();
builder.Services.AddScoped<IOfstedSummaryPromptRetriever, OfstedSummaryPromptRetriever>();
builder.Services.AddScoped<IConcernsInformationRetriever, ConcernsInformationRetriever>();
builder.Services.AddScoped<IOfstedIndexer, OfstedIndexer>();
builder.Services.AddScoped<IBriefingRunner, BriefingRunner>();

SecurityServicesSetup.AddSecurityServices(builder);

builder.Services.Configure<AzureSettings>(
    builder.Configuration.GetSection("AzureSettings"));

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

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
