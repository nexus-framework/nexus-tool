namespace {{ROOT_NAMESPACE}}.Model;

/// <summary>
///     New company request model.
/// </summary>
[ExcludeFromCodeCoverage]
public class CompanyRequestModel
{
    /// <summary>
    ///     Company Name.
    /// </summary>
    required public string Name { get; set; }

    /// <summary>
    ///     Tags associated with the company.
    /// </summary>
    public List<string> Tags { get; set; } = new ();
}