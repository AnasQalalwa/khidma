using Khidma.Api.Contracts.Auth;
using Khidma.Api.Data;
using Khidma.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Khidma.Api.Auth;

public sealed class UserRegistrationService
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public UserRegistrationService(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _db = db;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<RegisterResult> RegisterAsync(
        RegisterRequest request,
        string role,
        CancellationToken cancellationToken)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        var result = await strategy.ExecuteAsync(async () =>
        {
            IDbContextTransaction? transaction = null;
            if (_db.Database.IsRelational())
            {
                transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            }

            try
            {
                var user = new ApplicationUser
                {
                    UserName = request.Email.Trim(),
                    Email = request.Email.Trim(),
                    EmailConfirmed = true,
                    FullName = request.FullName.Trim(),
                    CreatedAt = DateTimeOffset.UtcNow
                };

                var createResult = await _userManager.CreateAsync(
                    user,
                    request.Password);

                if (!createResult.Succeeded)
                {
                    if (transaction is not null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }

                    return new RegistrationWork(
                        RegisterResult.FromIdentityErrors(createResult.Errors),
                        null);
                }

                var roleResult = await _userManager.AddToRoleAsync(user, role);
                if (!roleResult.Succeeded)
                {
                    if (transaction is not null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }

                    return new RegistrationWork(
                        RegisterResult.FromIdentityErrors(roleResult.Errors),
                        null);
                }

                if (role == AppRoles.Customer)
                {
                    _db.CustomerProfiles.Add(new CustomerProfile
                    {
                        UserId = user.Id,
                        City = request.City.Trim()
                    });
                }
                else
                {
                    _db.ProviderProfiles.Add(new ProviderProfile
                    {
                        UserId = user.Id,
                        City = request.City.Trim(),
                        YearsOfExperience = request.YearsOfExperience ?? 0,
                        Bio = string.IsNullOrWhiteSpace(request.Bio)
                            ? null
                            : request.Bio.Trim(),
                        IsApproved = true,
                        AverageRating = 0,
                        ReviewCount = 0
                    });
                }

                await _db.SaveChangesAsync(cancellationToken);

                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                var dto = await CurrentUserMapper.ToDtoAsync(_userManager, user);
                return new RegistrationWork(RegisterResult.Ok(dto), user);
            }
            catch
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                throw;
            }
            finally
            {
                if (transaction is not null)
                {
                    await transaction.DisposeAsync();
                }
            }
        });

        if (result.User is not null)
        {
            await _signInManager.SignInAsync(result.User, isPersistent: true);
        }

        return result.Outcome;
    }

    private sealed record RegistrationWork(
        RegisterResult Outcome,
        ApplicationUser? User);
}
