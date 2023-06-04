using System.Net;
using AutoMapper;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Fallback;
using {{ROOT_NAMESPACE}}.Abstractions;
using {{ROOT_NAMESPACE}}.DTO;
using {{ROOT_NAMESPACE}}.Model;

namespace {{ROOT_NAMESPACE}}.Services;

/// <summary>
///     Service for managing projects.
/// </summary>
public class ProjectService : IProjectService
{
    private readonly HttpClient _client;
    private readonly IMapper _mapper;
    private readonly ILogger<ProjectService> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectService" /> class.
    /// </summary>
    /// <param name="client">The HTTP client.</param>
    /// <param name="mapper">The mapper.</param>
    public ProjectService(HttpClient client, IMapper mapper, ILogger<ProjectService> logger)
    {
        _client = client;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    ///     Gets projects by company ID asynchronously.
    /// </summary>
    /// <param name="companyId">The ID of the company to get projects for.</param>
    /// <returns>A list of projects for the specified company.</returns>
    public async Task<List<ProjectSummaryDto>> GetProjectsByCompanyIdAsync(int companyId)
    {
        AsyncCircuitBreakerPolicy<HttpResponseMessage>? circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .AdvancedCircuitBreakerAsync(
                0.5,
                TimeSpan.FromSeconds(10),
                2,
                TimeSpan.FromSeconds(30),
                OnBreak,
                OnReset,
                OnHalfOpen);

        AsyncFallbackPolicy<HttpResponseMessage>? fallbackPolicy = Policy<HttpResponseMessage>
            .Handle<Exception>()
            .FallbackAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<ProjectResponseModel>()),
            });

        HttpResponseMessage? response = await fallbackPolicy.WrapAsync(circuitBreakerPolicy).ExecuteAsync(() =>
            _client.GetAsync($"https://project-api/api/v1/Project?companyId={companyId}"));

        if (!response.IsSuccessStatusCode)
        {
            return new List<ProjectSummaryDto>();
        }

        List<ProjectResponseModel>? projects =
            await response.Content.ReadFromJsonAsync<List<ProjectResponseModel>>();

        return _mapper.Map<List<ProjectSummaryDto>>(projects);
    }

    private void OnHalfOpen()
    {
        _logger.LogInformation("Circuit breaker half-opened");
    }

    private void OnReset()
    {
        _logger.LogInformation("Circuit breaker reset");
    }

    private void OnBreak(DelegateResult<HttpResponseMessage> result, TimeSpan span)
    {
        _logger.LogError("Circuit breaker opened for {Span} due to {ExceptionType}", span,
            result.Exception?.GetType().Name ?? "Exception");
    }
}
