using Khidma.Api.Auth;
using Khidma.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Khidma.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected string RequireUserId()
    {
        return User.GetUserId()
            ?? throw new InvalidOperationException("Authenticated user id is missing.");
    }

    protected string CallerRole() => User.ResolveWorkspaceRole();

    protected bool CallerIsAdmin() => User.IsInRole(AppRoles.Admin);

    protected IActionResult FromResult<T>(ServiceResult<T> result)
    {
        if (result.Succeeded)
        {
            return Ok(result.Value);
        }

        if (result.StatusCode == StatusCodes.Status400BadRequest &&
            result.Errors.Count > 0)
        {
            foreach (var (key, messages) in result.Errors)
            {
                foreach (var message in messages)
                {
                    ModelState.AddModelError(key, message);
                }
            }

            return ValidationProblem(ModelState);
        }

        return StatusCode(result.StatusCode, new ProblemDetails
        {
            Status = result.StatusCode,
            Title = result.Title,
            Detail = result.Detail
        });
    }
}
