using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace {{ROOT_NAMESPACE}}.Telemetry;

public class CompanyInstrumentation : ICompanyInstrumentation, IDisposable
{
    private readonly Meter _meter;
    internal const string ActivitySourceName = "CompanyApi.Company";
    internal const string MeterName = "CompanyApi.Company";
    public CompanyInstrumentation()
    {
        
        string? version = typeof(CompanyInstrumentation).Assembly.GetName().Version?.ToString();
        ActivitySource = new ActivitySource(ActivitySourceName, version);
        _meter = new Meter(MeterName, version);
        GetAllCompaniesCounter =
            _meter.CreateCounter<long>("company.api.getall", "The number of calls to GetAllCompanies endpoint");
    }
    
    public ActivitySource ActivitySource { get; }
    
    public Counter<long> GetAllCompaniesCounter { get; }

    public void Dispose()
    {
        ActivitySource.Dispose();
        _meter.Dispose();
    }
}

public interface ICompanyInstrumentation
{
    ActivitySource ActivitySource { get; }

    Counter<long> GetAllCompaniesCounter { get; }
}