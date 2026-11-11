using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Projects;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.API.Controllers;

[Authorize]
public class ProjectsController(IProjectsService projectsService) : BaseApiController
{
    private readonly IProjectsService _projectsService = projectsService;

    /// <summary>
    /// Gets all projects belonging to the authenticated user, optionally filtered by company.
    /// </summary>
    /// <param name="companyId">Optional company ID filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user's projects ordered alphabetically by name.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProjectResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IEnumerable<ProjectResponse>>>> GetAll(
        [FromQuery] Guid? companyId,
        CancellationToken cancellationToken)
    {
        var response = await _projectsService.GetAllAsync(companyId, cancellationToken);
        return OkResponse(response, "Projects retrieved successfully.");
    }

    /// <summary>
    /// Gets a project by ID for the authenticated user.
    /// </summary>
    /// <param name="id">Project ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The requested project.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProjectResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProjectResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await _projectsService.GetByIdAsync(id, cancellationToken);
        return OkResponse(response, "Project retrieved successfully.");
    }

    /// <summary>
    /// Creates a new project for a company owned by the authenticated user.
    /// </summary>
    /// <param name="request">Project creation details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created project.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProjectResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ProjectResponse>>> Create(
        [FromBody] CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _projectsService.CreateAsync(request, cancellationToken);
        return CreatedResponse(nameof(GetById), new { id = response.Id }, response, "Project created successfully.");
    }

    /// <summary>
    /// Updates an existing project belonging to the authenticated user.
    /// </summary>
    /// <param name="id">Project ID.</param>
    /// <param name="request">Updated project details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated project.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProjectResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ProjectResponse>>> Update(
        Guid id,
        [FromBody] UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _projectsService.UpdateAsync(id, request, cancellationToken);
        return OkResponse(response, "Project updated successfully.");
    }

    /// <summary>
    /// Deletes an existing project belonging to the authenticated user.
    /// </summary>
    /// <param name="id">Project ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Confirmation response.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _projectsService.DeleteAsync(id, cancellationToken);
        return OkResponse("Project deleted successfully.");
    }
}

