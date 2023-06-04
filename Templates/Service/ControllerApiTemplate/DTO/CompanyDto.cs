namespace {{ROOT_NAMESPACE}}.DTO;

[ExcludeFromCodeCoverage]
public class CompanyDto
{
    public int Id { get; set; }

    required public string Name { get; set; }

    public List<TagDto> Tags { get; set; } = new ();

    public List<ProjectSummaryDto> Projects { get; set; } = new ();
}