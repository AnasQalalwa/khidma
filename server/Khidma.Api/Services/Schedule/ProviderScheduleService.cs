using Khidma.Api.Contracts.Schedule;
using Khidma.Api.Data;
using Khidma.Api.Domain;
using Khidma.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Khidma.Api.Services.Schedule;

public sealed class ProviderScheduleService : IProviderScheduleService
{
    public const int MaxRangeDays = 62;

    private readonly AppDbContext _db;

    public ProviderScheduleService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult<WorkingHoursDto>> GetWorkingHoursAsync(
        string providerUserId,
        CancellationToken cancellationToken)
    {
        var profileId = await ProfileIdAsync(providerUserId, cancellationToken);
        if (profileId is null)
        {
            return ServiceResult<WorkingHoursDto>.NotFound("Provider profile not found.");
        }

        return ServiceResult<WorkingHoursDto>.Success(
            new WorkingHoursDto { Hours = await LoadHoursAsync(profileId.Value, cancellationToken) });
    }

    public async Task<ServiceResult<WorkingHoursDto>> ReplaceWorkingHoursAsync(
        string providerUserId,
        UpdateWorkingHoursRequest request,
        CancellationToken cancellationToken)
    {
        var profileId = await ProfileIdAsync(providerUserId, cancellationToken);
        if (profileId is null)
        {
            return ServiceResult<WorkingHoursDto>.NotFound("Provider profile not found.");
        }

        var distinct = new List<WorkingHourDto>();
        var seen = new HashSet<(int Day, int Hour)>();
        foreach (var hour in request.Hours)
        {
            if (hour.DayOfWeek is < 0 or > 6)
            {
                return ServiceResult<WorkingHoursDto>.Validation(
                    "hours",
                    "Day of week must be between 0 (Sunday) and 6 (Saturday).");
            }

            if (hour.Hour is < 0 or > 23)
            {
                return ServiceResult<WorkingHoursDto>.Validation(
                    "hours",
                    "Hour must be between 0 and 23.");
            }

            if (seen.Add((hour.DayOfWeek, hour.Hour)))
            {
                distinct.Add(hour);
            }
        }

        var existing = await _db.ProviderWorkingHours
            .Where(h => h.ProviderProfileId == profileId.Value)
            .ToListAsync(cancellationToken);
        _db.ProviderWorkingHours.RemoveRange(existing);
        foreach (var hour in distinct)
        {
            _db.ProviderWorkingHours.Add(new ProviderWorkingHour
            {
                ProviderProfileId = profileId.Value,
                DayOfWeek = hour.DayOfWeek,
                Hour = hour.Hour
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult<WorkingHoursDto>.Success(new WorkingHoursDto
        {
            Hours = distinct
                .OrderBy(h => h.DayOfWeek)
                .ThenBy(h => h.Hour)
                .ToList()
        });
    }

    public async Task<ServiceResult<ProviderScheduleDto>> GetMyScheduleAsync(
        string providerUserId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var rangeError = ValidateRange(from, to);
        if (rangeError is not null)
        {
            return ServiceResult<ProviderScheduleDto>.Validation("from", rangeError);
        }

        var profile = await _db.ProviderProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == providerUserId, cancellationToken);
        if (profile is null)
        {
            return ServiceResult<ProviderScheduleDto>.NotFound("Provider profile not found.");
        }

        var (rangeStart, rangeEnd) = RangeBounds(from, to);
        var bookings = await _db.Bookings
            .AsNoTracking()
            .Where(b => b.ProviderId == providerUserId)
            .Where(b =>
                ((b.Status == BookingStatus.Scheduled || b.Status == BookingStatus.InProgress) &&
                 b.ScheduledStart != null &&
                 b.DurationHours != null) ||
                (b.Status == BookingStatus.Pending &&
                 b.RequestedDate >= from &&
                 b.RequestedDate <= to))
            .Select(b => new
            {
                b.Id,
                ServiceName = b.Service.Name,
                CustomerName = b.Customer.FullName,
                b.Status,
                b.ScheduledStart,
                b.DurationHours,
                b.RequestedDate
            })
            .ToListAsync(cancellationToken);

        bookings = bookings
            .Where(b =>
                b.Status == BookingStatus.Pending ||
                (b.ScheduledStart < rangeEnd &&
                 b.ScheduledStart!.Value.AddHours(b.DurationHours!.Value) > rangeStart))
            .ToList();

        var items = bookings
            .OrderBy(b => b.ScheduledStart ?? b.RequestedDate.ToDateTime(TimeOnly.MinValue))
            .Select(b => new ScheduleEntryDto
            {
                BookingId = b.Id,
                ServiceName = b.ServiceName,
                CustomerName = b.CustomerName,
                Status = b.Status.ToString(),
                Start = b.ScheduledStart,
                End = b.ScheduledStart is null || b.DurationHours is null
                    ? null
                    : b.ScheduledStart.Value.AddHours(b.DurationHours.Value),
                RequestedDate = b.Status == BookingStatus.Pending ? b.RequestedDate : null
            })
            .ToList();

        return ServiceResult<ProviderScheduleDto>.Success(new ProviderScheduleDto
        {
            WorkingHours = await LoadHoursAsync(profile.Id, cancellationToken),
            Items = items
        });
    }

    public async Task<ServiceResult<ProviderAvailabilityDto>> GetAvailabilityAsync(
        int providerProfileId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var rangeError = ValidateRange(from, to);
        if (rangeError is not null)
        {
            return ServiceResult<ProviderAvailabilityDto>.Validation("from", rangeError);
        }

        var profile = await _db.ProviderProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == providerProfileId, cancellationToken);
        if (profile is null || !profile.CanReceiveWork)
        {
            return ServiceResult<ProviderAvailabilityDto>.NotFound("Provider not found.");
        }

        var (rangeStart, rangeEnd) = RangeBounds(from, to);
        var slots = await _db.Bookings
            .AsNoTracking()
            .Where(b =>
                b.ProviderId == profile.UserId &&
                (b.Status == BookingStatus.Scheduled || b.Status == BookingStatus.InProgress) &&
                b.ScheduledStart != null &&
                b.DurationHours != null)
            .Select(b => new
            {
                Start = b.ScheduledStart!.Value,
                b.DurationHours
            })
            .ToListAsync(cancellationToken);
        var busy = slots
            .Select(b => new BusyIntervalDto
            {
                Start = b.Start,
                End = b.Start.AddHours(b.DurationHours!.Value)
            })
            .Where(b => b.Start < rangeEnd && b.End > rangeStart)
            .ToList();

        return ServiceResult<ProviderAvailabilityDto>.Success(new ProviderAvailabilityDto
        {
            WorkingHours = await LoadHoursAsync(profile.Id, cancellationToken),
            Busy = busy.OrderBy(b => b.Start).ToList()
        });
    }

    internal static string? ValidateRange(DateOnly from, DateOnly to)
    {
        if (to < from)
        {
            return "The end date must be on or after the start date.";
        }

        if (to.DayNumber - from.DayNumber > MaxRangeDays)
        {
            return $"Choose a range of {MaxRangeDays} days or fewer.";
        }

        return null;
    }

    internal static (DateTimeOffset Start, DateTimeOffset End) RangeBounds(DateOnly from, DateOnly to)
    {
        var start = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var end = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        return (start, end);
    }

    private async Task<int?> ProfileIdAsync(string userId, CancellationToken cancellationToken) =>
        await _db.ProviderProfiles
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<IReadOnlyList<WorkingHourDto>> LoadHoursAsync(
        int profileId,
        CancellationToken cancellationToken) =>
        await _db.ProviderWorkingHours
            .AsNoTracking()
            .Where(h => h.ProviderProfileId == profileId)
            .OrderBy(h => h.DayOfWeek)
            .ThenBy(h => h.Hour)
            .Select(h => new WorkingHourDto
            {
                DayOfWeek = h.DayOfWeek,
                Hour = h.Hour
            })
            .ToListAsync(cancellationToken);
}
