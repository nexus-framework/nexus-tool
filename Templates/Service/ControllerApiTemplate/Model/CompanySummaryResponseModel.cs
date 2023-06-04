namespace {{ROOT_NAMESPACE}}.Model;

[ExcludeFromCodeCoverage]
public class CompanySummaryResponseModel
{
    public int Id { get; set; }

    required public string Name { get; set; }

    public int ProjectCount { get; set; }
    
    public List<string> Tags { get; set; } = new ();
}