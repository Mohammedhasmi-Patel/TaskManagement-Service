using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Common.Models;

namespace TaskManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    protected ActionResult<ApiResponse<T>> OkResponse<T>(T data, string message = "Request completed successfully.")
    {
        return Ok(ApiResponse<T>.SuccessResponse(data, message, StatusCodes.Status200OK));
    }

    protected ActionResult<ApiResponse<object?>> OkResponse(string message = "Request completed successfully.")
    {
        return Ok(ApiResponse<object?>.SuccessResponse(null, message, StatusCodes.Status200OK));
    }

    protected ActionResult<ApiResponse<T>> CreatedResponse<T>(
        string actionName,
        object? routeValues,
        T data,
        string message = "Resource created successfully.")
    {
        return CreatedAtAction(
            actionName,
            routeValues,
            ApiResponse<T>.SuccessResponse(data, message, StatusCodes.Status201Created));
    }

    protected ActionResult<ApiResponse<T>> CreatedResponse<T>(
        string uri,
        T data,
        string message = "Resource created successfully.")
    {
        return Created(
            uri,
            ApiResponse<T>.SuccessResponse(data, message, StatusCodes.Status201Created));
    }
}
