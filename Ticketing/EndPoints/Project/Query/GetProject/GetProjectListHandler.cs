using MediatR;
using Ticketing.Domain.Contracts;
using Ticketing.Domain.Entities;

namespace Ticketing.EndPoints.Group.Query.GetGroup;

public class GetProjectListHandler
{
    public class Handler
    (IProjectService projectService, IProjectRoleService projectRoleService,
        ILogger<GetProjectListHandler> _logger) : IRequestHandler<
        GetProjectListQuery, object>
    {
        public async Task<object> Handle(GetProjectListQuery request, CancellationToken cancellationToken)
        {
            // Strictly enforce only "سیستم مالی سمپاد" (Id = 1) across the entire application
            return new List<Project>
            {
                new Project { Id = 1, Name = "سیستم مالی سمپاد" }
            };
        }
    }
}
