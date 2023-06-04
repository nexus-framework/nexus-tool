using AutoMapper;
using {{ROOT_NAMESPACE}}.Abstractions;
using {{ROOT_NAMESPACE}}.Data;
using {{ROOT_NAMESPACE}}.DTO;
using {{ROOT_NAMESPACE}}.Entities;

namespace {{ROOT_NAMESPACE}}.Services;

/// <summary>
///     Service for managing companies and their associated tags and projects.
/// </summary>
public class CompanyService : ICompanyService
{
    private readonly IMapper _mapper;
    private readonly IProjectService _projectService;
    private readonly UnitOfWork _unitOfWork;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CompanyService" /> class.
    /// </summary>
    /// <param name="mapper">The mapper.</param>
    /// <param name="projectService">The project service.</param>
    /// <param name="unitOfWork">Unit of work for the project.</param>
    public CompanyService(IMapper mapper,
        IProjectService projectService, UnitOfWork unitOfWork)
    {
        _mapper = mapper;
        _projectService = projectService;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    ///     Gets all companies asynchronously.
    /// </summary>
    /// <returns>A list of all companies.</returns>
    public async Task<List<CompanySummaryDto>> GetAllAsync()
    {
        List<Company> companies = await _unitOfWork.Companies.AllCompaniesWithTagsAsync();
        List<CompanySummaryDto> mappedCompanies = _mapper.Map<List<CompanySummaryDto>>(companies);

        foreach (CompanySummaryDto company in mappedCompanies)
        {
            List<ProjectSummaryDto> projects = await _projectService.GetProjectsByCompanyIdAsync(company.Id);
            company.ProjectCount = projects.Count;
        }
        
        return mappedCompanies;
    }

    /// <summary>
    ///     Creates a new company asynchronously.
    /// </summary>
    /// <param name="companySummary">The company summary.</param>
    /// <returns>The created company.</returns>
    public async Task<CompanySummaryDto> CreateAsync(CompanySummaryDto companySummary)
    {
        try
        {
            _unitOfWork.BeginTransaction();
            Company companyToCreate = new (companySummary.Name);
            List<Tag> tagsToAdd = new ();

            if (companySummary.Tags.Count != 0)
            {
                foreach (string tagName in companySummary.Tags.Select(t => t.Name))
                {
                    Tag? dbTag = await _unitOfWork.Tags.GetByName(tagName);

                    if (dbTag != null)
                    {
                        tagsToAdd.Add(dbTag);
                    }
                    else
                    {
                        Tag tagToAdd = new (tagName);
                        _unitOfWork.Tags.Add(tagToAdd);
                        tagsToAdd.Add(tagToAdd);
                    }
                }
            }

            companyToCreate.AddTags(tagsToAdd);
            _unitOfWork.Companies.Add(companyToCreate);

            _unitOfWork.Commit();

            return _mapper.Map<CompanySummaryDto>(companyToCreate);
        }
        catch (Exception e)
        {
            Console.WriteLine("error hmmm");
            _unitOfWork.Rollback();

            return new CompanySummaryDto { Name = "" };
        }
    }

    /// <summary>
    ///     Gets a company by ID asynchronously.
    /// </summary>
    /// <param name="id">The ID of the company to get.</param>
    /// <returns>The company with the specified ID, or null if not found.</returns>
    public async Task<CompanyDto?> GetByIdAsync(int id)
    {
        return new CompanyDto { Name = "" };
        // Company? company = await _unitOfWork.Companies.FirstOrDefaultAsync(new CompanyByIdWithTagsSpec(id));
        //
        // if (company == null)
        // {
        //     return null;
        // }
        //
        // CompanyDto mappedCompanySummary = _mapper.Map<CompanyDto>(company);
        // List<ProjectSummaryDto> projects = await _projectService.GetProjectsByCompanyIdAsync(id);
        // mappedCompanySummary.Projects = projects;
        //
        // return mappedCompanySummary;
    }

    /// <summary>
    ///     Updates the name of a company asynchronously.
    /// </summary>
    /// <param name="id">The ID of the company to update.</param>
    /// <param name="name">The new name of the company.</param>
    /// <returns>The updated company, or null if not found.</returns>
    public async Task<CompanySummaryDto?> UpdateNameAsync(int id, string name)
    {
        return new CompanySummaryDto
            { Name = "" };
        // Company? companyToUpdate = await _unitOfWork.Companies.GetByIdAsync(id);
        //
        // if (companyToUpdate == null)
        // {
        //     return null;
        // }
        //
        // companyToUpdate.UpdateName(name);
        // await _unitOfWork.Companies.SaveChangesAsync();
        //
        // CompanySummaryDto summaryDto = _mapper.Map<CompanySummaryDto>(companyToUpdate);
        // return summaryDto;
    }

    /// <summary>
    ///     Adds a tag to a company asynchronously.
    /// </summary>
    /// <param name="id">The ID of the company to add the tag to.</param>
    /// <param name="tagName">The name of the tag to add.</param>
    /// <returns>The updated company, or null if not found.</returns>
    public async Task<CompanySummaryDto?> AddTagAsync(int id, string tagName)
    {
        return new CompanySummaryDto { Name = "" };
        // Company? companyToUpdate = await _unitOfWork.Companies.FirstOrDefaultAsync(new CompanyByIdWithTagsSpec(id));
        //
        // if (companyToUpdate == null)
        // {
        //     return null;
        // }
        //
        // Tag? dbTag = await _unitOfWork.Tags.FirstOrDefaultAsync(new TagByNameSpec(tagName));
        //
        // if (dbTag != null)
        // {
        //     companyToUpdate.AddTag(dbTag);
        // }
        // else
        // {
        //     Tag addedTag = await _unitOfWork.Tags.AddAsync(new Tag(tagName));
        //     companyToUpdate.AddTag(addedTag);
        // }
        //
        // await _unitOfWork.Companies.SaveChangesAsync();
        // return _mapper.Map<CompanySummaryDto>(companyToUpdate);
    }

    /// <summary>
    ///     Deletes a tag from a company asynchronously.
    /// </summary>
    /// <param name="id">The ID of the company to delete the tag from.</param>
    /// <param name="tagName">The name of the tag to delete.</param>
    /// <returns>The updated company, or null if not found.</returns>
    public async Task<CompanySummaryDto?> DeleteTagAsync(int id, string tagName)
    {
        return new CompanySummaryDto { Name = "" };
        // Company? companyToUpdate = await _unitOfWork.Companies.FirstOrDefaultAsync(new CompanyByIdWithTagsSpec(id));
        //
        // if (companyToUpdate == null)
        // {
        //     return null;
        // }
        //
        // companyToUpdate.RemoveTag(tagName);
        //
        // await _unitOfWork.Companies.SaveChangesAsync();
        // CompanySummaryDto summaryDto = _mapper.Map<CompanySummaryDto>(companyToUpdate);
        //
        // return summaryDto;
    }

    /// <summary>
    ///     Deletes a company asynchronously.
    /// </summary>
    /// <param name="id">The ID of the company to delete.</param>
    public async Task DeleteAsync(int id)
    {
        // Company? companyToDelete = await _unitOfWork.Companies.FirstOrDefaultAsync(new CompanyByIdWithTagsSpec(id));
        //
        // if (companyToDelete == null)
        // {
        //     return;
        // }
        //
        // companyToDelete.RemoveTags();
        //
        // await _unitOfWork.Companies.SaveChangesAsync();
        // await _unitOfWork.Companies.DeleteAsync(companyToDelete);
    }
}