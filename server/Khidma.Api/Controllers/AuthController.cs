using Khidma.Api.Auth;
using Khidma.Api.Contracts.Auth;
using Khidma.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Khidma.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly UserRegistrationService _registrationService;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthController(
        UserRegistrationService registrationService,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _registrationService = registrationService;
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<CurrentUserDto>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (AppRoles.IsAdminRole(request.Role))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Admin accounts cannot be registered publicly.",
                Detail = "Public registration is limited to Customer and Provider roles."
            });
        }

        if (!AppRoles.TryNormalizePublicRole(request.Role, out var role))
        {
            ModelState.AddModelError(
                nameof(request.Role),
                "Role must be Customer or Provider.");
            return ValidationProblem(ModelState);
        }

        var result = await _registrationService.RegisterAsync(
            request,
            role,
            cancellationToken);

        if (!result.Succeeded)
        {
            if (result.StatusCode == StatusCodes.Status409Conflict)
            {
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = result.Title,
                    Detail = result.Errors.SelectMany(e => e.Value)
                        .FirstOrDefault()
                });
            }

            foreach (var (key, messages) in result.Errors)
            {
                foreach (var message in messages)
                {
                    ModelState.AddModelError(key, message);
                }
            }

            return ValidationProblem(ModelState);
        }

        return Ok(result.User);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<CurrentUserDto>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _signInManager.PasswordSignInAsync(
            request.Email.Trim(),
            request.Password,
            isPersistent: true,
            lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Invalid email or password."
            });
        }

        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Invalid email or password."
            });
        }

        cancellationToken.ThrowIfCancellationRequested();
        var dto = await CurrentUserMapper.ToDtoAsync(_userManager, user);
        return Ok(dto);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserDto>> Me()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized"
            });
        }

        var dto = await CurrentUserMapper.ToDtoAsync(_userManager, user);
        return Ok(dto);
    }
}
