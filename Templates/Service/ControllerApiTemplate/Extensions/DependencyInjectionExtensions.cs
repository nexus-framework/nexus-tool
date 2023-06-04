using FluentValidation;
using {{ROOT_NAMESPACE}}.Abstractions;
using {{ROOT_NAMESPACE}}.Data;
using {{ROOT_NAMESPACE}}.Data.Repositories;
using {{ROOT_NAMESPACE}}.Mapping;
using {{ROOT_NAMESPACE}}.Services;
using {{ROOT_NAMESPACE}}.Telemetry;
using OpenTelemetry.Resources;
using Steeltoe.Common.Http.Discovery;

namespace {{ROOT_NAMESPACE}}.Extensions;

[ExcludeFromCodeCoverage]
public static class DependencyInjectionExtensions
{
    public static void RegisterDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        // Internal Services
        services.AddSingleton<ICompanyInstrumentation, CompanyInstrumentation>();
        
        // Custom Meter for Metrics
        services.AddOpenTelemetry()
            .ConfigureResource(c =>
            {
                c.AddService("company-api");
            })
            .WithMetrics(builder =>
            {
                builder.AddMeter(CompanyInstrumentation.MeterName);
            });
        
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<ITagService, TagService>();
        services
            .AddHttpClient("projects")
            .AddServiceDiscovery()
            .ConfigureHttpClient((serviceProvider, options) =>
            {
                IHttpContextAccessor httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();

                if (httpContextAccessor.HttpContext == null)
                {
                    return;
                }

                string? bearerToken = httpContextAccessor.HttpContext.Request.Headers["Authorization"]
                    .FirstOrDefault(h =>
                        !string.IsNullOrEmpty(h) &&
                        h.StartsWith("bearer ", StringComparison.InvariantCultureIgnoreCase));

                if (!string.IsNullOrEmpty(bearerToken))
                {
                    options.DefaultRequestHeaders.Add("Authorization", bearerToken);
                }
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                return new HttpClientHandler
                {
                    ClientCertificateOptions = ClientCertificateOption.Manual,
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
                };
            })
            .AddTypedClient<IProjectService, ProjectService>();

        // Libraries
        services.AddAutoMapper(typeof(CompanyProfile));
        services.AddValidatorsFromAssemblyContaining(typeof(Program));

        // Persistence
        services.AddCorePersistence<ApplicationDbContext>(configuration);
        services.AddScoped<CompanyRepository>();
        services.AddScoped<TagRepository>();
        services.AddScoped<UnitOfWork>();
    }
}