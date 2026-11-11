using MediatR;

namespace Ticketing.EndPoints.Group.Query.GetGroup;

public class GetProjectListQuery: IRequest<object>
{
    [Ticketing.Services.CallerIdentity(Ticketing.Services.CallerIdentityKind.RoleId)]
    public int? RoleId { get; set; }
}