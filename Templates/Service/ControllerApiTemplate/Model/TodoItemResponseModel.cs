namespace {{ROOT_NAMESPACE}}.Model;

[ExcludeFromCodeCoverage]
public class TodoItemResponseModel
{
    public int Id { get; set; }

    required public string Title { get; set; }

    public string Description { get; set; } = string.Empty;

    public string? AssignedToId { get; set; }

    public bool IsCompleted { get; set; }
}