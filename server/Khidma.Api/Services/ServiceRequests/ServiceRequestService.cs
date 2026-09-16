using Khidma.Api.Auth;
using Khidma.Api.Contracts.Common;
using Khidma.Api.Contracts.Offers;
using Khidma.Api.Contracts.ServiceRequests;
using Khidma.Api.Data;
using Khidma.Api.Domain;
using Khidma.Api.Domain.Enums;
using Khidma.Api.Services.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Khidma.Api.Services.ServiceRequests;

public sealed class ServiceRequestService : IServiceRequestService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ServiceRequestService> _logger;
    private readonly IAuditService _audit;

    public ServiceRequestService(
        AppDbContext db,
        ILogger<ServiceRequestService> logger,
        IAuditService audit)
    {
        _db = db;
        _logger = logger;
        _audit = audit;
    }

    public IQueryable<ServiceRequest> EligibleOpenRequestsForProvider(
        ProviderProfile provider)
    {
        if (!provider.CanReceiveWork)
        {
            return _db.ServiceRequests.Where(_ => false);
        }

        var profileId = provider.Id;
        var city = provider.City;

        return _db.ServiceRequests.Where(request =>
            request.Status == ServiceRequestStatus.Open &&
            request.City.ToLower() == city.ToLower() &&
            _db.ProviderServices.Any(ps =>
                ps.ProviderProfileId == profileId &&
                ps.ServiceId == request.ServiceId));
    }

    public Task<ProviderProfile?> FindProviderProfileAsync(
        string userId,
        CancellationToken cancellationToken) =>
        _db.ProviderProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    public async Task<ServiceResult<RequestDetailForCustomerDto>> CreateAsync(
        string customerId,
        CreateServiceRequestRequest request,
        CancellationToken cancellationToken)
    {
        var validation = RequestValidation.ValidateScheduleAndBudget<RequestDetailForCustomerDto>(
            request.PreferredDate,
            request.BudgetMin,
            request.BudgetMax);
        if (validation is not null)
        {
            return validation;
        }

        var serviceExists = await _db.Services
            .AnyAsync(s => s.Id == request.ServiceId, cancellationToken);
        if (!serviceExists)
        {
            return ServiceResult<RequestDetailForCustomerDto>.Validation(
                "serviceId",
                "The selected service does not exist.");
        }

        var entity = new ServiceRequest
        {
            CustomerId = customerId,
            ServiceId = request.ServiceId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            City = request.City.Trim(),
            PreferredDate = request.PreferredDate,
            BudgetMin = request.BudgetMin,
            BudgetMax = request.BudgetMax,
            Status = ServiceRequestStatus.Open,
            CreatedAt = DateTimeOffset.UtcNow,
            RowVersion = [0]
        };

        _db.ServiceRequests.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Customer {UserId} created service request {ServiceRequestId}",
            customerId,
            entity.Id);

        await _audit.RecordAsync(new AuditEntry
        {
            Category = AuditCategories.Request,
            Action = AuditActions.RequestCreated,
            Outcome = AuditOutcomes.Success,
            EntityType = nameof(ServiceRequest),
            EntityId = entity.Id.ToString(),
            Message = "Customer created a service request.",
            Details = new Dictionary<string, object?>
            {
                ["serviceId"] = entity.ServiceId,
                ["city"] = entity.City
            }
        }, cancellationToken);

        return await GetCustomerDetailAsync(entity.Id, customerId, cancellationToken);
    }

    public async Task<ServiceResult<PagedResult<ServiceRequestSummaryDto>>> GetMineAsync(
        string customerId,
        PageQuery paging,
        CancellationToken cancellationToken)
    {
        var (page, pageSize) = paging.Normalize();
        var query = _db.ServiceRequests
            .AsNoTracking()
            .Where(r => r.CustomerId == customerId);

        if (!string.IsNullOrWhiteSpace(paging.Status))
        {
            if (!RequestValidation.TryParseStatus(paging.Status, out var status))
            {
                return ServiceResult<PagedResult<ServiceRequestSummaryDto>>.Validation(
                    "status",
                    "Status is not valid.");
            }

            query = query.Where(r => r.Status == status);
        }

        var pageResult = await query
            .OrderByDescending(r => r.Id)
            .Select(r => new ServiceRequestSummaryDto
            {
                Id = r.Id,
                Title = r.Title,
                ServiceId = r.ServiceId,
                ServiceName = r.Service.Name,
                CategoryName = r.Service.Category.Name,
                City = r.City,
                PreferredDate = r.PreferredDate,
                BudgetMin = r.BudgetMin,
                BudgetMax = r.BudgetMax,
                Status = r.Status.ToString(),
                OfferCount = r.Offers.Count(o => o.Status != OfferStatus.Withdrawn),
                CreatedAt = r.CreatedAt
            })
            .ToPagedResultAsync(page, pageSize, cancellationToken);

        return ServiceResult<PagedResult<ServiceRequestSummaryDto>>.Success(pageResult);
    }

    public async Task<ServiceResult<PagedResult<RequestSummaryForProviderDto>>> GetAvailableAsync(
        string providerUserId,
        PageQuery paging,
        CancellationToken cancellationToken)
    {
        var profile = await FindProviderProfileAsync(providerUserId, cancellationToken);
        if (profile is null)
        {
            return ServiceResult<PagedResult<RequestSummaryForProviderDto>>.Forbidden(
                "Provider profile was not found.");
        }

        var (page, pageSize) = paging.Normalize();
        var pageResult = await EligibleOpenRequestsForProvider(profile)
            .AsNoTracking()
            .OrderByDescending(r => r.Id)
            .Select(r => new RequestSummaryForProviderDto
            {
                Id = r.Id,
                Title = r.Title,
                Description = r.Description,
                ServiceId = r.ServiceId,
                ServiceName = r.Service.Name,
                CategoryName = r.Service.Category.Name,
                City = r.City,
                PreferredDate = r.PreferredDate,
                BudgetMin = r.BudgetMin,
                BudgetMax = r.BudgetMax,
                Status = r.Status.ToString(),
                CreatedAt = r.CreatedAt
            })
            .ToPagedResultAsync(page, pageSize, cancellationToken);

        return ServiceResult<PagedResult<RequestSummaryForProviderDto>>.Success(pageResult);
    }

    public async Task<ServiceResult<object>> GetByIdAsync(
        int id,
        string userId,
        string role,
        CancellationToken cancellationToken)
    {
        if (role == AppRoles.Admin)
        {
            var admin = await GetAdminDetailAsync(id, cancellationToken);
            return admin.Succeeded
                ? ServiceResult<object>.Success(admin.Value!)
                : MapFailure<object, RequestDetailForAdminDto>(admin);
        }

        if (role == AppRoles.Provider)
        {
            var provider = await GetProviderDetailAsync(id, userId, cancellationToken);
            return provider.Succeeded
                ? ServiceResult<object>.Success(provider.Value!)
                : MapFailure<object, RequestDetailForProviderDto>(provider);
        }

        var customer = await GetCustomerDetailAsync(id, userId, cancellationToken);
        return customer.Succeeded
            ? ServiceResult<object>.Success(customer.Value!)
            : MapFailure<object, RequestDetailForCustomerDto>(customer);
    }

    public async Task<ServiceResult<RequestDetailForCustomerDto>> UpdateAsync(
        int id,
        string customerId,
        UpdateServiceRequestRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await _db.ServiceRequests
            .Include(r => r.Offers)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (entity is null)
        {
            return ServiceResult<RequestDetailForCustomerDto>.NotFound(
                "Service request not found.");
        }

        if (entity.CustomerId != customerId)
        {
            return ServiceResult<RequestDetailForCustomerDto>.Forbidden(
                "You do not own this service request.");
        }

        var hasActiveOffers = entity.Offers.Any(o => o.Status != OfferStatus.Withdrawn);
        if (entity.Status != ServiceRequestStatus.Open || hasActiveOffers)
        {
            return ServiceResult<RequestDetailForCustomerDto>.Conflict(
                "This request can only be edited while it is open and has no active offers.");
        }

        var validation = RequestValidation.ValidateScheduleAndBudget<RequestDetailForCustomerDto>(
            request.PreferredDate,
            request.BudgetMin,
            request.BudgetMax);
        if (validation is not null)
        {
            return validation;
        }

        entity.Title = request.Title.Trim();
        entity.Description = request.Description.Trim();
        entity.PreferredDate = request.PreferredDate;
        entity.BudgetMin = request.BudgetMin;
        entity.BudgetMax = request.BudgetMax;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Customer {UserId} updated service request {ServiceRequestId}",
            customerId,
            entity.Id);

        await _audit.RecordAsync(new AuditEntry
        {
            Category = AuditCategories.Request,
            Action = AuditActions.RequestUpdated,
            Outcome = AuditOutcomes.Success,
            EntityType = nameof(ServiceRequest),
            EntityId = entity.Id.ToString(),
            Message = "Customer updated a service request."
        }, cancellationToken);

        return await GetCustomerDetailAsync(entity.Id, customerId, cancellationToken);
    }

    public async Task<ServiceResult<RequestDetailForCustomerDto>> CancelAsync(
        int id,
        string customerId,
        CancellationToken cancellationToken)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            IDbContextTransaction? transaction = null;
            if (_db.Database.IsRelational())
            {
                transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            }

            try
            {
                var entity = await _db.ServiceRequests
                    .Include(r => r.Offers)
                    .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

                if (entity is null)
                {
                    return await Abort(
                        transaction,
                        ServiceResult<RequestDetailForCustomerDto>.NotFound(
                            "Service request not found."),
                        cancellationToken);
                }

                if (entity.CustomerId != customerId)
                {
                    return await Abort(
                        transaction,
                        ServiceResult<RequestDetailForCustomerDto>.Forbidden(
                            "You do not own this service request."),
                        cancellationToken);
                }

                if (entity.Status != ServiceRequestStatus.Open)
                {
                    return await Abort(
                        transaction,
                        ServiceResult<RequestDetailForCustomerDto>.Conflict(
                            "Only open requests can be cancelled."),
                        cancellationToken);
                }

                entity.Status = ServiceRequestStatus.Cancelled;
                foreach (var offer in entity.Offers.Where(o => o.Status == OfferStatus.Pending))
                {
                    offer.Status = OfferStatus.Rejected;
                }

                await _db.SaveChangesAsync(cancellationToken);
                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                _logger.LogInformation(
                    "Customer {UserId} cancelled service request {ServiceRequestId}",
                    customerId,
                    entity.Id);

                await _audit.RecordAsync(new AuditEntry
                {
                    Category = AuditCategories.Request,
                    Action = AuditActions.RequestCancelled,
                    Outcome = AuditOutcomes.Success,
                    EntityType = nameof(ServiceRequest),
                    EntityId = entity.Id.ToString(),
                    Message = "Customer cancelled a service request."
                }, cancellationToken);

                return await GetCustomerDetailAsync(entity.Id, customerId, cancellationToken);
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
    }

    private async Task<ServiceResult<RequestDetailForCustomerDto>> GetCustomerDetailAsync(
        int id,
        string customerId,
        CancellationToken cancellationToken)
    {
        var entity = await _db.ServiceRequests
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new
            {
                r.Id,
                r.CustomerId,
                r.Title,
                r.Description,
                r.ServiceId,
                ServiceName = r.Service.Name,
                CategoryName = r.Service.Category.Name,
                r.City,
                r.PreferredDate,
                r.BudgetMin,
                r.BudgetMax,
                r.Status,
                r.CreatedAt,
                OfferCount = r.Offers.Count(o => o.Status != OfferStatus.Withdrawn),
                ActiveOfferCount = r.Offers.Count(o => o.Status != OfferStatus.Withdrawn),
                BookingId = _db.Bookings
                    .Where(b => b.ServiceRequestId == r.Id)
                    .Select(b => (int?)b.Id)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
        {
            return ServiceResult<RequestDetailForCustomerDto>.NotFound(
                "Service request not found.");
        }

        if (entity.CustomerId != customerId)
        {
            return ServiceResult<RequestDetailForCustomerDto>.Forbidden(
                "You do not own this service request.");
        }

        var offers = await OfferProjections
            .ForCustomer(
                _db.Offers.AsNoTracking().Where(o => o.ServiceRequestId == id),
                entity.Status == ServiceRequestStatus.Open)
            .OrderByDescending(o => o.Id)
            .ToListAsync(cancellationToken);

        return ServiceResult<RequestDetailForCustomerDto>.Success(new RequestDetailForCustomerDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            ServiceId = entity.ServiceId,
            ServiceName = entity.ServiceName,
            CategoryName = entity.CategoryName,
            City = entity.City,
            PreferredDate = entity.PreferredDate,
            BudgetMin = entity.BudgetMin,
            BudgetMax = entity.BudgetMax,
            Status = entity.Status.ToString(),
            OfferCount = entity.OfferCount,
            CreatedAt = entity.CreatedAt,
            CanEdit = entity.Status == ServiceRequestStatus.Open && entity.ActiveOfferCount == 0,
            CanCancel = entity.Status == ServiceRequestStatus.Open,
            BookingId = entity.BookingId,
            Offers = offers
        });
    }

    private async Task<ServiceResult<RequestDetailForProviderDto>> GetProviderDetailAsync(
        int id,
        string providerUserId,
        CancellationToken cancellationToken)
    {
        var profile = await FindProviderProfileAsync(providerUserId, cancellationToken);
        if (profile is null)
        {
            return ServiceResult<RequestDetailForProviderDto>.NotFound(
                "Service request not found.");
        }

        var visible = await EligibleOpenRequestsForProvider(profile)
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new RequestDetailForProviderDto
            {
                Id = r.Id,
                Title = r.Title,
                Description = r.Description,
                ServiceId = r.ServiceId,
                ServiceName = r.Service.Name,
                CategoryName = r.Service.Category.Name,
                City = r.City,
                PreferredDate = r.PreferredDate,
                BudgetMin = r.BudgetMin,
                BudgetMax = r.BudgetMax,
                Status = r.Status.ToString(),
                CreatedAt = r.CreatedAt,
                CanOffer = false,
                MyOffer = null
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (visible is null)
        {
            return ServiceResult<RequestDetailForProviderDto>.NotFound(
                "Service request not found.");
        }

        var myOffer = await _db.Offers
            .AsNoTracking()
            .Where(o => o.ServiceRequestId == id && o.ProviderId == providerUserId)
            .OrderByDescending(o => o.Id)
            .Select(o => new OfferSnapshotDto
            {
                Id = o.Id,
                Price = o.Price,
                Message = o.Message,
                EstimatedDate = o.EstimatedDate,
                Status = o.Status.ToString(),
                CreatedAt = o.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        var hasActiveOffer = await _db.Offers.AnyAsync(
            o => o.ServiceRequestId == id &&
                 o.ProviderId == providerUserId &&
                 o.Status != OfferStatus.Withdrawn,
            cancellationToken);

        return ServiceResult<RequestDetailForProviderDto>.Success(new RequestDetailForProviderDto
        {
            Id = visible.Id,
            Title = visible.Title,
            Description = visible.Description,
            ServiceId = visible.ServiceId,
            ServiceName = visible.ServiceName,
            CategoryName = visible.CategoryName,
            City = visible.City,
            PreferredDate = visible.PreferredDate,
            BudgetMin = visible.BudgetMin,
            BudgetMax = visible.BudgetMax,
            Status = visible.Status,
            CreatedAt = visible.CreatedAt,
            CanOffer = !hasActiveOffer,
            MyOffer = myOffer
        });
    }

    private async Task<ServiceResult<RequestDetailForAdminDto>> GetAdminDetailAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var entity = await _db.ServiceRequests
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new RequestDetailForAdminDto
            {
                Id = r.Id,
                Title = r.Title,
                Description = r.Description,
                ServiceId = r.ServiceId,
                ServiceName = r.Service.Name,
                CategoryName = r.Service.Category.Name,
                City = r.City,
                PreferredDate = r.PreferredDate,
                BudgetMin = r.BudgetMin,
                BudgetMax = r.BudgetMax,
                Status = r.Status.ToString(),
                CreatedAt = r.CreatedAt,
                CustomerId = r.CustomerId,
                CustomerName = r.Customer.FullName,
                CustomerEmail = r.Customer.Email ?? string.Empty,
                OfferCount = r.Offers.Count(o => o.Status != OfferStatus.Withdrawn),
                BookingId = _db.Bookings
                    .Where(b => b.ServiceRequestId == r.Id)
                    .Select(b => (int?)b.Id)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
        {
            return ServiceResult<RequestDetailForAdminDto>.NotFound(
                "Service request not found.");
        }

        return ServiceResult<RequestDetailForAdminDto>.Success(entity);
    }

    private static ServiceResult<TOut> MapFailure<TOut, TIn>(ServiceResult<TIn> source) =>
        source.Errors.Count > 0
            ? ServiceResult<TOut>.Validation(source.Errors, source.Title)
            : source.StatusCode switch
            {
                StatusCodes.Status403Forbidden => ServiceResult<TOut>.Forbidden(source.Title, source.Detail),
                StatusCodes.Status409Conflict => ServiceResult<TOut>.Conflict(source.Title, source.Detail),
                StatusCodes.Status401Unauthorized => ServiceResult<TOut>.Unauthorized(source.Title),
                _ => ServiceResult<TOut>.NotFound(source.Title, source.Detail)
            };

    private static async Task<ServiceResult<T>> Abort<T>(
        IDbContextTransaction? transaction,
        ServiceResult<T> result,
        CancellationToken cancellationToken)
    {
        if (transaction is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
        }

        return result;
    }
}
