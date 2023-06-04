namespace {{ROOT_NAMESPACE}}.Model;

[ExcludeFromCodeCoverage]
public class CompanyUpdateRequestModel
{
    required public int Id { get; set; }
    
    required public string Name { get; set; }
}