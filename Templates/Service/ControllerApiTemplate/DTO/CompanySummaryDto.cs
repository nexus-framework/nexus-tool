namespace {{ROOT_NAMESPACE}}.DTO;

[ExcludeFromCodeCoverage]
public class CompanySummaryDto
{
    public int Id { get; set; }

    required public string Name { get; set; }

    public int ProjectCount { get; set; }

    public List<TagDto> Tags { get; set; } = new ();
}