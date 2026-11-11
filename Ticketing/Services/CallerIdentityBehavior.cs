using System.Reflection;
using MediatR;

namespace Ticketing.Services;

/// <summary>Validate caller identity once, before any handler can read or write data.</summary>
public sealed class CallerIdentityBehavior<TRequest, TResponse>(ICurrentUserService currentUser)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private static readonly (PropertyInfo Property, CallerIdentityKind Kind)[] Fields =
        typeof(TRequest).GetProperties()
            .Select(p => (Property: p, Attribute: p.GetCustomAttribute<CallerIdentityAttribute>()))
            .Where(p => p.Attribute != null)
            .Select(p => (p.Property, p.Attribute!.Kind)).ToArray();

    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not > 0 || !currentUser.RoleIds.Any(r => r > 0))
            throw new CallerIdentityException(StatusCodes.Status401Unauthorized);

        foreach (var (property, kind) in Fields)
        {
            var value = property.GetValue(request);
            switch (kind)
            {
                case CallerIdentityKind.UserId:
                    if (value is int id && id != 0 && id != currentUser.UserId)
                        throw new CallerIdentityException(StatusCodes.Status403Forbidden);
                    property.SetValue(request, currentUser.UserId.Value);
                    break;
                case CallerIdentityKind.RoleId:
                    if (value is int role && role != 0 && !currentUser.HasRole(role))
                        throw new CallerIdentityException(StatusCodes.Status403Forbidden);
                    var assignedRole = value is int supplied && supplied != 0 ? supplied
                        : currentUser.DefaultRoleId is > 0 && currentUser.HasRole(currentUser.DefaultRoleId.Value)
                            ? currentUser.DefaultRoleId.Value : currentUser.RoleIds.First(r => r > 0);
                    property.SetValue(request, assignedRole);
                    break;
                case CallerIdentityKind.Username:
                    if (value is string name && !string.IsNullOrWhiteSpace(name) &&
                        !string.Equals(name, currentUser.Username, StringComparison.OrdinalIgnoreCase))
                        throw new CallerIdentityException(StatusCodes.Status403Forbidden);
                    property.SetValue(request, currentUser.Username ?? "");
                    break;
            }
        }
        return next();
    }
}
