using AutoMapper;
using {{ROOT_NAMESPACE}}.Abstractions;
using {{ROOT_NAMESPACE}}.Data;
using {{ROOT_NAMESPACE}}.DTO;

namespace {{ROOT_NAMESPACE}}.Services;

/// <summary>
///     Service for managing tags.
/// </summary>
public class TagService : ITagService
{
    private readonly IMapper _mapper;
    private readonly UnitOfWork _unitOfWork;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TagService" /> class.
    /// </summary>
    /// <param name="tagRepository">The tag repository.</param>
    /// <param name="mapper">The mapper.</param>
    /// <param name="companyRepository">The company repository.</param>
    public TagService(IMapper mapper, UnitOfWork unitOfWork)
    {
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    ///     Creates a new tag asynchronously.
    /// </summary>
    /// <param name="name">The name of the tag to create.</param>
    /// <returns>The created tag.</returns>
    public async Task<TagDto> CreateAsync(string name)
    {
        return new TagDto
            { Name = "" };
        // Tag tagToCreate = new (name);
        // Tag createdTag = await _tagRepository.AddAsync(tagToCreate);
        // return _mapper.Map<TagDto>(createdTag);
    }

    /// <summary>
    ///     Deletes a tag asynchronously.
    /// </summary>
    /// <param name="name">The name of the tag to delete.</param>
    /// <returns>True if the tag was deleted, false otherwise.</returns>
    public async Task<bool> DeleteAsync(string name)
    {
        return false;
        // if (await _companyRepository.AnyAsync(new AllCompaniesByTagNameSpec(name)))
        // {
        //     return false;
        // }
        //
        // Tag? tagToDelete = await _tagRepository.FirstOrDefaultAsync(new TagByNameSpec(name));
        //
        // if (tagToDelete != null)
        // {
        //     await _tagRepository.DeleteAsync(tagToDelete);
        // }
        //
        // return true;
    }

    /// <summary>
    ///     Gets all tags asynchronously.
    /// </summary>
    /// <returns>A list of all tags.</returns>
    public async Task<List<TagDto>> GetAllAsync()
    {
        return new List<TagDto>();
        // List<Tag> tags = await _tagRepository.ListAsync();
        // return _mapper.Map<List<TagDto>>(tags);
    }
}