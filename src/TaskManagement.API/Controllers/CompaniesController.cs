using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Companies;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.API.Controllers;

[Authorize]
public class CompaniesController(ICompaniesService companiesService) : BaseApiController
{
    private readonly ICompaniesService _companiesService = companiesService;

    /// <summary>
    /// Gets all companies belonging to the authenticated user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user's companies ordered with the default company first.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CompanyResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IEnumerable<CompanyResponse>>>> GetAll(CancellationToken cancellationToken)
    {
        var response = await _companiesService.GetAllAsync(cancellationToken);
        return OkResponse(response, "Companies retrieved successfully.");
    }

    /// <summary>
    /// Gets a company by ID for the authenticated user.
    /// </summary>
    /// <param name="id">Company ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The requested company.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CompanyResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CompanyResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await _companiesService.GetByIdAsync(id, cancellationToken);
        return OkResponse(response, "Company retrieved successfully.");
    }

    /// <summary>
    /// Creates a new company for the authenticated user.
    /// </summary>
    /// <param name="request">Company creation details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created company.</returns>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<CompanyResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<CompanyResponse>>> Create(
        [FromForm] CreateCompanyRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _companiesService.CreateAsync(request, cancellationToken);
        return CreatedResponse(nameof(GetById), new { id = response.Id }, response, "Company created successfully.");
    }

    /// <summary>
    /// Updates an existing company belonging to the authenticated user.
    /// </summary>
    /// <param name="id">Company ID.</param>
    /// <param name="request">Updated company details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated company.</returns>
    [HttpPut("{id:guid}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<CompanyResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<CompanyResponse>>> Update(
        Guid id,
        [FromForm] UpdateCompanyRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _companiesService.UpdateAsync(id, request, cancellationToken);
        return OkResponse(response, "Company updated successfully.");
    }

    /// <summary>
    /// Deletes an existing company belonging to the authenticated user.
    /// </summary>
    /// <param name="id">Company ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Confirmation response.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _companiesService.DeleteAsync(id, cancellationToken);
        return OkResponse("Company deleted successfully.");
    }
}

