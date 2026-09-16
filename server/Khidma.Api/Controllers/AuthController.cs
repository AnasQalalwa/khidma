using Khidma.Api.Auth;
using Khidma.Api.Contracts.Auth;
using Khidma.Api.Domain;
using Khidma.Api.Services.Audit;
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
    private readonly IAuditService _audit;

    public AuthController(
        UserRegistrationService registrationService,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditService audit)
    {
        _registrationService = registrationService;
        _signInManager = signInManager;
        _userManager = userManager;
        _audit = audit;
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

        await _audit.RecordAsync(new AuditEntry
        {
            Category = AuditCategories.Auth,
            Action = AuditActions.RegisterSucceeded,
            Outcome = AuditOutcomes.Success,
            EntityType = nameof(ApplicationUser),
            EntityId = result.User!.Id,
            ActorUserId = result.User.Id,
            ActorEmail = result.User.Email,
            ActorRole = result.User.Role,
            Message = "User registered.",
            Details = new Dictionary<string, object?>
            {
                ["role"] = result.User.Role
            }
        }, cancellationToken);

        return Ok(result.User);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<CurrentUserDto>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var result = await _signInManager.PasswordSignInAsync(
            email,
            request.Password,
            isPersistent: true,
            lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            await _audit.RecordAsync(new AuditEntry
            {
                Category = AuditCategories.Auth,
                Action = AuditActions.LoginFailed,
                Outcome = AuditOutcomes.Failed,
                ActorEmail = email,
                Message = "Login failed.",
                Details = new Dictionary<string, object?>
                {
                    ["attemptedEmail"] = email.ToUpperInvariant()
                }
            }, cancellationToken);

            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Invalid email or password."
            });
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Invalid email or password."
            });
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await _userManager.UpdateAsync(user);

        cancellationToken.ThrowIfCancellationRequested();
        var dto = await CurrentUserMapper.ToDtoAsync(_userManager, user);

        await _audit.RecordAsync(new AuditEntry
        {
            Category = AuditCategories.Auth,
            Action = AuditActions.LoginSucceeded,
            Outcome = AuditOutcomes.Success,
            EntityType = nameof(ApplicationUser),
            EntityId = user.Id,
            ActorUserId = user.Id,
            ActorEmail = dto.Email,
            ActorRole = dto.Role,
            Message = "User signed in."
        }, cancellationToken);

        return Ok(dto);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await _audit.RecordAsync(new AuditEntry
        {
            Category = AuditCategories.Auth,
            Action = AuditActions.Logout,
            Outcome = AuditOutcomes.Success,
            Message = "User signed out."
        }, cancellationToken);

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
