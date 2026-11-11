using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.DTOs.Companies;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.API.Controllers;

[Authorize]
public class CompaniesController(ICompaniesService companiesService) : BaseApiController
{
    private readonly ICompaniesService _companiesService = companiesService;

    /// <summary>
    /// Creates a new company for the authenticated user.
    /// </summary>
    /// <param name="request">Company creation details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created company.</returns>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CompanyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CompanyResponse>> Create(
        [FromForm] CreateCompanyRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _companiesService.CreateAsync(request, cancellationToken);
        return Created($"/api/companies/{response.Id}", response);
    }
}

