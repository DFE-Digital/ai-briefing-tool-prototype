using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Identity.Web;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace DfE.FindInformationAcademiesTrusts.Setup;

[ExcludeFromCodeCoverage]
public static class SecurityServicesSetup
{
    public static void AddSecurityServices(WebApplicationBuilder builder)
    {
        AddHsts(builder);
        AddIdentityServices(builder);
        AddAntiForgeryCookies(builder);
        AddDataProtectionServices(builder);
    }

    private static void AddHsts(WebApplicationBuilder builder)
    {
        // Enforce HTTPS in ASP.NET Core
        // @link https://learn.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?
        builder.Services.AddHsts(options =>
        {
            options.Preload = true;
            options.IncludeSubDomains = true;
            options.MaxAge = TimeSpan.FromDays(365);
        });
    }

    private static void AddIdentityServices(WebApplicationBuilder builder)
    {
        // Setup bypass for automation tests
        builder.Services.AddAuthorization(options =>
        {
            var policyBuilder = new AuthorizationPolicyBuilder();
            policyBuilder.RequireAuthenticatedUser();
            options.DefaultPolicy = policyBuilder.Build();
            options.FallbackPolicy = options.DefaultPolicy;

        });

        builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration);

        if (!builder.Environment.IsDevelopment())
        {
            // Override the redirect URI scheme
            builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.Events.OnRedirectToIdentityProvider = context =>
                {
                    if (context.ProtocolMessage.RedirectUri.StartsWith("http://"))
                    {
                        context.ProtocolMessage.RedirectUri =
                            context.ProtocolMessage.RedirectUri.Replace("http://", "https://");
                    }

                    return Task.CompletedTask;
                };
            });
        }

        builder.Services.Configure<CookieAuthenticationOptions>(
            CookieAuthenticationDefaults.AuthenticationScheme,
            options =>
            {
                options.Cookie.Name = "BriefingName";
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });
    }

    private static void AddAntiForgeryCookies(WebApplicationBuilder builder)
    {
        builder.Services.AddAntiforgery(opts => { opts.Cookie.Name = "BriefingAntiForge"; });
    }

    private static void AddDataProtectionServices(WebApplicationBuilder builder)
    {
        // Setup basic Data Protection and persist keys.xml to local file system
        var dp = builder.Services.AddDataProtection();

        // If a Key Vault Key URI is defined, expect to encrypt the keys.xml
        var kvProtectionKeyUri = builder.Configuration.GetValue<string>("DataProtection:KeyVaultKey");
        if (!string.IsNullOrWhiteSpace(kvProtectionKeyUri))
        {
            var kvProtectionPath = builder.Configuration.GetValue<string>("DataProtection:Path");

            if (string.IsNullOrWhiteSpace(kvProtectionPath))
            {
                throw new InvalidOperationException("DataProtection:Path is undefined or empty");
            }

            var kvProtectionPathDir = new DirectoryInfo(kvProtectionPath);
            if (!kvProtectionPathDir.Exists || kvProtectionPathDir.Attributes.HasFlag(FileAttributes.ReadOnly))
            {
                throw new ReadOnlyException($"DataProtection path '{kvProtectionPath}' cannot be written to");
            }

            dp.PersistKeysToFileSystem(kvProtectionPathDir);
        }
    }
}
