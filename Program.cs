using BriefingTool.Services;
using GovUk.Frontend.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddGovUkFrontend(options => options.Rebrand = true);

builder.Services.AddScoped<IBasePromptRetriever, BasePromptRetriever>();
builder.Services.AddScoped<IConcernsPromptRetriever, ConcernsPromptRetriever>();

builder.Services.AddScoped<IAcademyInformationRetriever, AcademyInformationRetriever>();
builder.Services.AddScoped<IConcernsInformationRetriever, ConcernsInformationRetriever>();


var app = builder.Build();

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

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();



app.Run();
