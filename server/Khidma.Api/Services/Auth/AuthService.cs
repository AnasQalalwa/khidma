using Khidma.Api.Auth;
using Khidma.Api.Contracts.Auth;
using Khidma.Api.Domain;
using Khidma.Api.Services;
using Khidma.Api.Services.Audit;
using Microsoft.AspNetCore.Identity;

namespace Khidma.Api.Services.Auth;

public interface IAuthService
{
    Task<ServiceResult<CurrentUserDto>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<CurrentUserDto>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken);

    Task LogoutAsync(CancellationToken cancellationToken);

    Task<ServiceResult<CurrentUserDto>> GetMeAsync(
        string userId,
        CancellationToken cancellationToken);
}

public sealed class AuthService : IAuthService
{
    private readonly UserRegistrationService _registration;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _audit;

    public AuthService(
        UserRegistrationService registration,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditService audit)
    {
        _registration = registration;
        _signInManager = signInManager;
        _userManager = userManager;
        _audit = audit;
    }

    public async Task<ServiceResult<CurrentUserDto>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (AppRoles.IsAdminRole(request.Role))
        {
            return ServiceResult<CurrentUserDto>.BadRequest(
                "Admin accounts cannot be registered publicly.",
                "Public registration is limited to Customer and Provider roles.");
        }

        if (!AppRoles.TryNormalizePublicRole(request.Role, out var role))
        {
            return ServiceResult<CurrentUserDto>.Validation(
                nameof(request.Role),
                "Role must be Customer or Provider.");
        }

        var result = await _registration.RegisterAsync(request, role, cancellationToken);
        if (!result.Succeeded)
        {
            if (result.StatusCode == StatusCodes.Status409Conflict)
            {
                return ServiceResult<CurrentUserDto>.Conflict(
                    result.Title,
                    result.Errors.SelectMany(e => e.Value).FirstOrDefault());
            }

            return ServiceResult<CurrentUserDto>.Validation(result.Errors, result.Title);
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

        return ServiceResult<CurrentUserDto>.Success(result.User!);
    }

    public async Task<ServiceResult<CurrentUserDto>> LoginAsync(
        LoginRequest request,
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

            return ServiceResult<CurrentUserDto>.Unauthorized("Invalid email or password.");
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return ServiceResult<CurrentUserDto>.Unauthorized("Invalid email or password.");
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

        return ServiceResult<CurrentUserDto>.Success(dto);
    }

    public async Task LogoutAsync(CancellationToken cancellationToken)
    {
        await _audit.RecordAsync(new AuditEntry
        {
            Category = AuditCategories.Auth,
            Action = AuditActions.Logout,
            Outcome = AuditOutcomes.Success,
            Message = "User signed out."
        }, cancellationToken);

        await _signInManager.SignOutAsync();
    }

    public async Task<ServiceResult<CurrentUserDto>> GetMeAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return ServiceResult<CurrentUserDto>.Unauthorized();
        }

        cancellationToken.ThrowIfCancellationRequested();
        var dto = await CurrentUserMapper.ToDtoAsync(_userManager, user);
        return ServiceResult<CurrentUserDto>.Success(dto);
    }
}
