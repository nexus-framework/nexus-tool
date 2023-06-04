using Nexus.Core;

namespace {{ROOT_NAMESPACE}}.Entities;

/// <summary>
///     Represents a Company entity in the system.
/// </summary>
public class Company : AuditableEntityBase
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Company" /> class.
    /// </summary>
    /// <param name="name">The name of the company.</param>
    public Company(string name)
    {
        Name = name;
    }

    /// <summary>
    ///     Gets or sets the name of the company.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    ///     Gets or sets the list of tags associated with the company.
    /// </summary>
    public virtual List<Tag> Tags { get; set; } = new ();
    
    /// <summary>
    ///     Adds a tag to the list of tags associated with the company.
    /// </summary>
    /// <param name="tag">The tag to add.</param>
    public void AddTag(Tag tag)
    {
        if (!Tags.Contains(tag))
        {
            Tags.Add(tag);
        }
    }

    /// <summary>
    ///     Adds a list of tags to the list of tags associated with the company.
    /// </summary>
    /// <param name="tags">The list of tags to add.</param>
    public void AddTags(List<Tag> tags)
    {
        foreach (Tag tag in tags)
        {
            AddTag(tag);
        }
    }

    /// <summary>
    ///     Updates the name of the company.
    /// </summary>
    /// <param name="newName">The new name of the company.</param>
    public void UpdateName(string newName)
    {
        Name = newName;
    }

    /// <summary>
    ///     Removes a tag from the list of tags associated with the company.
    /// </summary>
    /// <param name="tagName">The name of the tag to remove.</param>
    public void RemoveTag(string tagName)
    {
        Tag? tagToRemove = Tags.FirstOrDefault(x => x.Name == tagName);

        if (tagToRemove != null)
        {
            Tags.Remove(tagToRemove);
        }
    }

    /// <summary>
    ///     Removes all tags from the list of tags associated with the company.
    /// </summary>
    public void RemoveTags()
    {
        if (Tags.Count == 0)
        {
            return;
        }

        List<string> tagNames = Tags.Select(t => t.Name).ToList();

        foreach (string tagName in tagNames)
        {
            RemoveTag(tagName);
        }
    }
}