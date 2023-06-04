using System.Reflection;
using Microsoft.EntityFrameworkCore;
using {{ROOT_NAMESPACE}}.Entities;
using Nexus.Persistence;
using Nexus.Persistence.Auditing;

namespace {{ROOT_NAMESPACE}}.Data;

/// <summary>
///     Application database context.
/// </summary>
[ExcludeFromCodeCoverage]
public class ApplicationDbContext : AuditableDbContext
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ApplicationDbContext" /> class.
    /// </summary>
    /// <param name="options">The options for this context.</param>
    /// <param name="auditableEntitySaveChangesInterceptor">Service to handle audit information.</param>
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        AuditableEntitySaveChangesInterceptor auditableEntitySaveChangesInterceptor)
        : base(options, auditableEntitySaveChangesInterceptor)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
    
    /// <summary>
    ///     Gets the companies.
    /// </summary>
    public DbSet<Company> Companies => Set<Company>();

    /// <summary>
    ///     Gets the tags.
    /// </summary>
    public DbSet<Tag> Tags => Set<Tag>();
}