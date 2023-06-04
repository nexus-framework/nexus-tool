namespace {{ROOT_NAMESPACE}}.Model;

[ExcludeFromCodeCoverage]
public class ProjectSummaryResponseModel
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    public string Name { get; set; }

    public int TaskCount { get; set; }
}