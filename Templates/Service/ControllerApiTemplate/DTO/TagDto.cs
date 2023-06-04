namespace {{ROOT_NAMESPACE}}.DTO;

[ExcludeFromCodeCoverage]
public class TagDto
{
    public int Id { get; set; }

    required public string Name { get; set; }
}