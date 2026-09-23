using Khidma.Api.Auth;
using Khidma.Api.Contracts.Account;
using Khidma.Api.Data;
using Khidma.Api.Domain;
using Khidma.Api.Services.Audit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Khidma.Api.Services.Account;

public interface IAccountService
{
    Task<ServiceResult<AccountProfileDto>> GetProfileAsync(
        string userId,
        CancellationToken cancellationToken);

    Task<ServiceResult<AccountProfileDto>> UpdateProfileAsync(
        string userId,
        UpdateAccountProfileRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<bool>> ChangePasswordAsync(
        string userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken);
}

public sealed class AccountService : IAccountService
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _audit;

    public AccountService(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        IAuditService audit)
    {
        _db = db;
        _userManager = userManager;
        _audit = audit;
    }

    public async Task<ServiceResult<AccountProfileDto>> GetProfileAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return ServiceResult<AccountProfileDto>.Unauthorized();
        }

        cancellationToken.ThrowIfCancellationRequested();
        var dto = await ToDtoAsync(user, cancellationToken);
        return ServiceResult<AccountProfileDto>.Success(dto);
    }

    public async Task<ServiceResult<AccountProfileDto>> UpdateProfileAsync(
        string userId,
        UpdateAccountProfileRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return ServiceResult<AccountProfileDto>.Unauthorized();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? string.Empty;
        var isCustomer = role == AppRoles.Customer;

        if (isCustomer && string.IsNullOrWhiteSpace(request.City))
        {
            return ServiceResult<AccountProfileDto>.Validation(
                "city",
                "Enter your city.");
        }

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = PhoneRules.Normalize(request.PhoneNumber);

        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            return ServiceResult<AccountProfileDto>.Validation(
                update.Errors.ToDictionary(
                    error => error.Code,
                    error => new[] { error.Description }));
        }

        if (isCustomer)
        {
            var profile = await _db.CustomerProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);
            if (profile is null)
            {
                return ServiceResult<AccountProfileDto>.NotFound("Customer profile not found.");
            }

            profile.City = request.City!.Trim();
            await _db.SaveChangesAsync(cancellationToken);
        }

        await _audit.RecordAsync(new AuditEntry
        {
            Category = AuditCategories.Auth,
            Action = AuditActions.AccountProfileUpdated,
            Outcome = AuditOutcomes.Success,
            EntityType = nameof(ApplicationUser),
            EntityId = user.Id,
            Message = "User updated their profile."
        }, cancellationToken);

        return ServiceResult<AccountProfileDto>.Success(
            await ToDtoAsync(user, cancellationToken));
    }

    public async Task<ServiceResult<bool>> ChangePasswordAsync(
        string userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return ServiceResult<bool>.Unauthorized();
        }

        cancellationToken.ThrowIfCancellationRequested();
        var result = await _userManager.ChangePasswordAsync(
            user,
            request.CurrentPassword,
            request.NewPassword);

        if (!result.Succeeded)
        {
            var message = result.Errors.Any(error => error.Code == "PasswordMismatch")
                ? "Current password is incorrect."
                : PasswordRules.RequirementMessage;
            return ServiceResult<bool>.Validation("currentPassword", message);
        }

        await _audit.RecordAsync(new AuditEntry
        {
            Category = AuditCategories.Auth,
            Action = AuditActions.AccountPasswordChanged,
            Outcome = AuditOutcomes.Success,
            EntityType = nameof(ApplicationUser),
            EntityId = user.Id,
            Message = "User changed their password."
        }, cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    private async Task<AccountProfileDto> ToDtoAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? string.Empty;
        string? city = null;
        if (role == AppRoles.Customer)
        {
            city = await _db.CustomerProfiles
                .AsNoTracking()
                .Where(p => p.UserId == user.Id)
                .Select(p => p.City)
                .FirstOrDefaultAsync(cancellationToken);
        }
        else if (role == AppRoles.Provider)
        {
            city = await _db.ProviderProfiles
                .AsNoTracking()
                .Where(p => p.UserId == user.Id)
                .Select(p => p.City)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new AccountProfileDto
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            Role = role,
            City = city
        };
    }
}
