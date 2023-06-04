namespace {{ROOT_NAMESPACE}}.Model;

[ExcludeFromCodeCoverage]
public class CompanyResponseModel
{
    public int Id { get; set; }

    required public string Name { get; set; }

    public List<TagResponseModel> Tags { get; set; } = new ();

    public List<ProjectSummaryResponseModel> Projects { get; set; } = new ();
}