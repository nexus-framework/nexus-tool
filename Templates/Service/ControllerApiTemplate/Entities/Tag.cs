using Nexus.Core;

namespace {{ROOT_NAMESPACE}}.Entities;

/// <summary>
///     Represents a tag that can be associated with one or more companies.
/// </summary>
public class Tag : AuditableEntityBase
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Tag" /> class with the specified name.
    /// </summary>
    /// <param name="name">The name of the tag.</param>
    public Tag(string name)
    {
        Name = name;
    }

    /// <summary>
    ///     Gets the name of the tag.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets or sets the list of companies that are associated with this tag.
    /// </summary>
    public virtual List<Company> Companies { get; set; } = new ();
}
