using Microsoft.EntityFrameworkCore;

namespace Khidma.Api.Infrastructure;

public static class DbExceptionClassifier
{
    public static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        for (var current = exception.InnerException;
             current is not null;
             current = current.InnerException)
        {
            var number = current.GetType().GetProperty("Number")?.GetValue(current) as int?;
            if (number is 2601 or 2627)
            {
                return true;
            }

            var message = current.Message;
            if (message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("unique index", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
