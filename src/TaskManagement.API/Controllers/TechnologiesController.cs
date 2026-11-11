using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Technologies;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.API.Controllers;

[Authorize]
public class TechnologiesController(ITechnologiesService technologiesService) : BaseApiController
{
    private readonly ITechnologiesService _technologiesService = technologiesService;

    /// <summary>
    /// Gets all technologies belonging to the authenticated user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user's technologies ordered alphabetically by name.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TechnologyResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IEnumerable<TechnologyResponse>>>> GetAll(CancellationToken cancellationToken)
    {
        var response = await _technologiesService.GetAllAsync(cancellationToken);
        return OkResponse(response, "Technologies retrieved successfully.");
    }

    /// <summary>
    /// Gets a technology by ID for the authenticated user.
    /// </summary>
    /// <param name="id">Technology ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The requested technology.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TechnologyResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TechnologyResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await _technologiesService.GetByIdAsync(id, cancellationToken);
        return OkResponse(response, "Technology retrieved successfully.");
    }

    /// <summary>
    /// Creates a new technology for the authenticated user.
    /// </summary>
    /// <param name="request">Technology creation details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created technology.</returns>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<TechnologyResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<TechnologyResponse>>> Create(
        [FromForm] CreateTechnologyRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _technologiesService.CreateAsync(request, cancellationToken);
        return CreatedResponse(nameof(GetById), new { id = response.Id }, response, "Technology created successfully.");
    }

    /// <summary>
    /// Updates an existing technology belonging to the authenticated user.
    /// </summary>
    /// <param name="id">Technology ID.</param>
    /// <param name="request">Updated technology details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated technology.</returns>
    [HttpPut("{id:guid}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<TechnologyResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<TechnologyResponse>>> Update(
        Guid id,
        [FromForm] UpdateTechnologyRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _technologiesService.UpdateAsync(id, request, cancellationToken);
        return OkResponse(response, "Technology updated successfully.");
    }

    /// <summary>
    /// Deletes an existing technology belonging to the authenticated user.
    /// </summary>
    /// <param name="id">Technology ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Confirmation response.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _technologiesService.DeleteAsync(id, cancellationToken);
        return OkResponse("Technology deleted successfully.");
    }
}
