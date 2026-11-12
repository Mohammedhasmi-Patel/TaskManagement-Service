using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.TimeEntries;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.API.Controllers;

[Authorize]
[Route("api/time-entries")]
public class TimeEntriesController(ITimeEntriesService timeEntriesService) : BaseApiController
{
    private readonly ITimeEntriesService _timeEntriesService = timeEntriesService;

    /// <summary>
    /// Gets all time entries belonging to the authenticated user, optionally filtered by task.
    /// </summary>
    /// <param name="taskId">Optional task ID filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user's time entries ordered by start time descending.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TimeEntryResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IEnumerable<TimeEntryResponse>>>> GetAll(
        [FromQuery] Guid? taskId,
        CancellationToken cancellationToken)
    {
        var response = await _timeEntriesService.GetAllAsync(taskId, cancellationToken);
        return OkResponse(response, "Time entries retrieved successfully.");
    }

    /// <summary>
    /// Gets a time entry by ID for the authenticated user.
    /// </summary>
    /// <param name="id">Time entry ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The requested time entry.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TimeEntryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TimeEntryResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await _timeEntriesService.GetByIdAsync(id, cancellationToken);
        return OkResponse(response, "Time entry retrieved successfully.");
    }

    /// <summary>
    /// Creates or starts a new time entry for a task owned by the authenticated user.
    /// </summary>
    /// <param name="request">Time entry creation details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created time entry.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TimeEntryResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TimeEntryResponse>>> Create(
        [FromBody] CreateTimeEntryRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _timeEntriesService.CreateAsync(request, cancellationToken);
        return CreatedResponse(nameof(GetById), new { id = response.Id }, response, "Time entry created successfully.");
    }

    /// <summary>
    /// Updates an existing time entry belonging to the authenticated user.
    /// </summary>
    /// <param name="id">Time entry ID.</param>
    /// <param name="request">Updated time entry details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated time entry.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TimeEntryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TimeEntryResponse>>> Update(
        Guid id,
        [FromBody] UpdateTimeEntryRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _timeEntriesService.UpdateAsync(id, request, cancellationToken);
        return OkResponse(response, "Time entry updated successfully.");
    }

    /// <summary>
    /// Stops a running time entry belonging to the authenticated user.
    /// </summary>
    /// <param name="id">Time entry ID.</param>
    /// <param name="request">Optional explicit stop time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The stopped time entry with calculated duration.</returns>
    [HttpPost("{id:guid}/stop")]
    [ProducesResponseType(typeof(ApiResponse<TimeEntryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TimeEntryResponse>>> Stop(
        Guid id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] StopTimeEntryRequest? request,
        CancellationToken cancellationToken)
    {
        var response = await _timeEntriesService.StopAsync(id, request, cancellationToken);
        return OkResponse(response, "Time entry stopped successfully.");
    }

    /// <summary>
    /// Deletes an existing time entry belonging to the authenticated user.
    /// </summary>
    /// <param name="id">Time entry ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Confirmation response.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _timeEntriesService.DeleteAsync(id, cancellationToken);
        return OkResponse("Time entry deleted successfully.");
    }
}
