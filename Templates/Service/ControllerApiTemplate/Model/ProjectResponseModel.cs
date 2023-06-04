namespace {{ROOT_NAMESPACE}}.Model;

[ExcludeFromCodeCoverage]
public class ProjectResponseModel
{
    public int Id { get; set; }

    public int? CompanyId { get; set; }

    public string Name { get; set; } = string.Empty;

    public List<TodoItemResponseModel> TodoItems { get; set; } = new ();
}