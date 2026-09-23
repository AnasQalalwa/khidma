using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Khidma.Api.Auth;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class PhoneNumberAttribute : ValidationAttribute
{
    public PhoneNumberAttribute()
        : base(PhoneRules.RequirementMessage)
    {
    }

    public override bool IsValid(object? value) =>
        value is string phone && PhoneRules.IsValid(phone);
}

public static partial class PhoneRules
{
    public const int MinLength = 9;

    public const int MaxLength = 20;

    public const string RequirementMessage =
        "Choose a country code and enter the local number, such as +970 0598969367.";

    public static string Normalize(string phone) => phone.Trim();

    public static bool IsValid(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return false;
        }

        var trimmed = phone.Trim();
        if (trimmed.Length < MinLength || trimmed.Length > MaxLength)
        {
            return false;
        }

        if (trimmed.StartsWith("+970 ", StringComparison.Ordinal))
        {
            return PalestinePattern().IsMatch(trimmed);
        }

        return InternationalPattern().IsMatch(trimmed);
    }

    [GeneratedRegex(@"^\+970 0[0-9]{8,9}$")]
    private static partial Regex PalestinePattern();

    [GeneratedRegex(@"^\+[1-9][0-9]{0,3} [0-9]{6,14}$")]
    private static partial Regex InternationalPattern();
}
