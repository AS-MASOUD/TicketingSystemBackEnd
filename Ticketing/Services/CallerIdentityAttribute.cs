namespace Ticketing.Services;

public enum CallerIdentityKind { UserId, RoleId, Username }

/// <summary>Marks caller identity fields, never resource filters or assignment targets.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class CallerIdentityAttribute(CallerIdentityKind kind) : Attribute
{
    public CallerIdentityKind Kind { get; } = kind;
}

public sealed class CallerIdentityException(int statusCode) : Exception("هویت یا نقش ارسال‌شده معتبر نیست.")
{
    public int StatusCode { get; } = statusCode;
}
