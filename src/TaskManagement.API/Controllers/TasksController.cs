using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Enums;

namespace TaskManagement.API.Controllers;

[Authorize]
public class TasksController(ITasksService tasksService) : BaseApiController
{
    private readonly ITasksService _tasksService = tasksService;

    /// <summary>
    /// Gets all tasks belonging to the authenticated user, optionally filtered by project, status, or priority.
    /// </summary>
    /// <param name="projectId">Optional project ID filter.</param>
    /// <param name="status">Optional task status filter.</param>
    /// <param name="priority">Optional task priority filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user's tasks ordered alphabetically by title.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TaskResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IEnumerable<TaskResponse>>>> GetAll(
        [FromQuery] Guid? projectId,
        [FromQuery] TaskStatusEnum? status,
        [FromQuery] TaskPriority? priority,
        CancellationToken cancellationToken)
    {
        var response = await _tasksService.GetAllAsync(projectId, status, priority, cancellationToken);
        return OkResponse(response, "Tasks retrieved successfully.");
    }

    /// <summary>
    /// Gets a task by ID for the authenticated user.
    /// </summary>
    /// <param name="id">Task ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The requested task.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TaskResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await _tasksService.GetByIdAsync(id, cancellationToken);
        return OkResponse(response, "Task retrieved successfully.");
    }

    /// <summary>
    /// Creates a new task inside a project owned by the authenticated user.
    /// </summary>
    /// <param name="request">Task creation details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created task.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TaskResponse>>> Create(
        [FromBody] CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _tasksService.CreateAsync(request, cancellationToken);
        return CreatedResponse(nameof(GetById), new { id = response.Id }, response, "Task created successfully.");
    }

    /// <summary>
    /// Updates an existing task belonging to the authenticated user.
    /// </summary>
    /// <param name="id">Task ID.</param>
    /// <param name="request">Updated task details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated task.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TaskResponse>>> Update(
        Guid id,
        [FromBody] UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _tasksService.UpdateAsync(id, request, cancellationToken);
        return OkResponse(response, "Task updated successfully.");
    }

    /// <summary>
    /// Deletes an existing task belonging to the authenticated user.
    /// </summary>
    /// <param name="id">Task ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Confirmation response.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _tasksService.DeleteAsync(id, cancellationToken);
        return OkResponse("Task deleted successfully.");
    }
}

