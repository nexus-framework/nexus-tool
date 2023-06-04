using Microsoft.EntityFrameworkCore;
using {{ROOT_NAMESPACE}}.Entities;
using Nexus.Persistence;

namespace {{ROOT_NAMESPACE}}.Data.Repositories;

public class CompanyRepository : EfCustomRepository<Company>
{
    public CompanyRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<Company>> AllCompaniesWithTagsAsync()
    {
        return await DbSet.Include(x => x.Tags).ToListAsync();
    }

    public async Task<bool> ExistsWithNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(x => x.Name == name, cancellationToken);
    }

    public async Task<Company?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
    }
}