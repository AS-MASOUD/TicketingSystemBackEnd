using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ticketing.Domain.Models;
using Ticketing.Application.Security;
using Ticketing.Domain.Contracts;
using Ticketing.Domain.Entities;
using Ticketing.Infrastructure.Database;

namespace Ticketing.Application.Service.Admin;

public class AdminManagementService : IAdminManagementService
{
    private readonly TicketingDbContext _dbContext;
    private readonly Microsoft.Extensions.Logging.ILogger<AdminManagementService>? _logger;

    public AdminManagementService(TicketingDbContext dbContext, Microsoft.Extensions.Logging.ILogger<AdminManagementService>? logger = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger;
    }

    public async Task<PagedResult<AdminUserDto>> GetUsersAsync(int pageNumber, int pageSize, string? search = null, int? roleId = null, int? projectId = null, bool? isActive = null)
    {
        var query = _dbContext.Users
            .Include(u => u.UserRoles)
            .Include(u => u.UserProjects)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(u => u.Username.Contains(search) || 
                                     (u.Name != null && u.Name.Contains(search)) || 
                                     (u.Family != null && u.Family.Contains(search)) ||
                                     (u.Email != null && u.Email.Contains(search)) ||
                                     (u.Mobile != null && u.Mobile.Contains(search)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        if (roleId.HasValue && roleId.Value > 0)
        {
            query = query.Where(u => u.UserRoles.Any(ur => ur.RoleId == roleId.Value) || u.DefaultRoleId == roleId.Value);
        }

        if (projectId.HasValue && projectId.Value > 0)
        {
            query = query.Where(u => u.UserProjects.Any(up => up.ProjectId == projectId.Value));
        }

        int totalCount = await query.CountAsync();

        var users = await query
            .OrderByDescending(u => u.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        List<TicketingRole> allRoles;
        try
        {
            allRoles = await _dbContext.Roles.AsNoTracking().ToListAsync();
        }
        catch
        {
            allRoles = new List<TicketingRole>
            {
                new TicketingRole { Id = 1, Name = "User", DisplayName = "کارفرما / کارشناس" },
                new TicketingRole { Id = 5, Name = "AdminVira", DisplayName = "مدیر ارشد سامانه" },
                new TicketingRole { Id = 4, Name = "TechnicalManager", DisplayName = "مدیر فنی پروژه" },
                new TicketingRole { Id = 2, Name = "Developer", DisplayName = "برنامه‌نویس و کارشناس" },
                new TicketingRole { Id = 3, Name = "Supervisor", DisplayName = "ناظر پروژه" }
            };
        }
        var allProjects = await _dbContext.Projects.AsNoTracking().ToListAsync();

        var items = users.Select(u =>
        {
            var defRole = allRoles.FirstOrDefault(r => r.Id == u.DefaultRoleId);
            var userRoleIds = u.UserRoles.Select(ur => ur.RoleId).ToList();
            if (!userRoleIds.Contains(u.DefaultRoleId))
                userRoleIds.Add(u.DefaultRoleId);

            var userRoleNames = allRoles
                .Where(r => userRoleIds.Contains(r.Id))
                .Select(r => r.DisplayName)
                .ToList();

            var userProjIds = u.UserProjects.Select(up => up.ProjectId).ToList();
            var userProjNames = userProjIds.Any()
                ? new List<string> { "سیستم مالی سمپاد" }
                : new List<string>();

            return new AdminUserDto
            {
                Id = u.Id,
                Username = u.Username,
                Name = u.Name,
                Family = u.Family,
                Email = u.Email,
                Mobile = u.Mobile,
                IsActive = u.IsActive,
                DefaultRoleId = u.DefaultRoleId,
                DefaultRoleName = defRole?.DisplayName ?? $"نقش {u.DefaultRoleId}",
                RoleIds = userRoleIds,
                RoleNames = userRoleNames,
                ProjectIds = userProjIds,
                ProjectNames = userProjNames,
                CreatedAt = u.CreatedAt
            };
        }).ToList();

        return new PagedResult<AdminUserDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<AdminUserDto?> GetUserDetailAsync(int userId)
    {
        var u = await _dbContext.Users
            .Include(x => x.UserRoles)
            .Include(x => x.UserProjects)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (u == null)
            return null;

        List<TicketingRole> allRoles;
        try
        {
            allRoles = await _dbContext.Roles.AsNoTracking().ToListAsync();
        }
        catch
        {
            allRoles = new List<TicketingRole>
            {
                new TicketingRole { Id = 1, Name = "User", DisplayName = "کارفرما / کارشناس" },
                new TicketingRole { Id = 5, Name = "AdminVira", DisplayName = "مدیر ارشد سامانه" },
                new TicketingRole { Id = 4, Name = "TechnicalManager", DisplayName = "مدیر فنی پروژه" },
                new TicketingRole { Id = 2, Name = "Developer", DisplayName = "برنامه‌نویس و کارشناس" },
                new TicketingRole { Id = 3, Name = "Supervisor", DisplayName = "ناظر پروژه" }
            };
        }
        var allProjects = await _dbContext.Projects.AsNoTracking().ToListAsync();

        var defRole = allRoles.FirstOrDefault(r => r.Id == u.DefaultRoleId);
        var userRoleIds = u.UserRoles.Select(ur => ur.RoleId).ToList();
        if (!userRoleIds.Contains(u.DefaultRoleId))
            userRoleIds.Add(u.DefaultRoleId);

        var userRoleNames = allRoles
            .Where(r => userRoleIds.Contains(r.Id))
            .Select(r => r.DisplayName)
            .ToList();

        var userProjIds = u.UserProjects.Select(up => up.ProjectId).ToList();
        var userProjNames = userProjIds.Any()
            ? new List<string> { "سیستم مالی سمپاد" }
            : new List<string>();

        return new AdminUserDto
        {
            Id = u.Id,
            Username = u.Username,
            Name = u.Name,
            Family = u.Family,
            Email = u.Email,
            Mobile = u.Mobile,
            IsActive = u.IsActive,
            DefaultRoleId = u.DefaultRoleId,
            DefaultRoleName = defRole?.DisplayName ?? $"نقش {u.DefaultRoleId}",
            RoleIds = userRoleIds,
            RoleNames = userRoleNames,
            ProjectIds = userProjIds,
            ProjectNames = userProjNames,
            CreatedAt = u.CreatedAt
        };
    }

    public async Task<TicketingUser> CreateUserAsync(CreateUserRequest request)
    {
        await using var operation = _dbContext.Database.CurrentTransaction == null
            ? await _dbContext.Database.BeginTransactionAsync() : null;
        await ValidateAssignmentsAsync(request.DefaultRoleId > 0 ? request.DefaultRoleId : 2, request.RoleIds, request.ProjectIds);
        if (await _dbContext.Users.AnyAsync(u => u.Username.ToLower() == request.Username.ToLower()))
            throw new InvalidOperationException($"نام کاربری '{request.Username}' از قبل در سیستم وجود دارد.");

        var user = new TicketingUser
        {
            Username = request.Username.Trim().ToLowerInvariant(),
            PasswordHash = SecurePasswordHasher.HashPassword(request.Password),
            Name = request.Name?.Trim(),
            Family = request.Family?.Trim(),
            Email = request.Email?.Trim(),
            Mobile = request.Mobile?.Trim(),
            DefaultRoleId = request.DefaultRoleId > 0 ? request.DefaultRoleId : 2,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Assign Roles
        var roleIds = request.RoleIds ?? new List<int>();
        if (!roleIds.Contains(user.DefaultRoleId))
            roleIds.Add(user.DefaultRoleId);

        var allRoles = await _dbContext.Roles.AsNoTracking().ToListAsync();
        foreach (var rId in roleIds.Distinct())
        {
            var role = allRoles.FirstOrDefault(r => r.Id == rId);
            _dbContext.UserRoles.Add(new TicketingUserRole
            {
                UserId = user.Id,
                RoleId = rId,
                RoleName = role?.Name ?? $"Role_{rId}"
            });
        }

        // Assign Projects
        if (request.ProjectIds != null)
        {
            foreach (var pId in request.ProjectIds.Distinct())
            {
                _dbContext.UserProjects.Add(new UserProject
                {
                    UserId = user.Id,
                    ProjectId = pId
                });
            }
        }

        await _dbContext.SaveChangesAsync();
        if (operation != null) await operation.CommitAsync();
        return user;
    }

    public async Task<bool> UpdateUserAsync(int userId, UpdateUserRequest request)
    {
        await using var operation = _dbContext.Database.CurrentTransaction == null
            ? await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable) : null;
        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .Include(u => u.UserProjects)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return false;

        await ValidateAssignmentsAsync(request.DefaultRoleId > 0 ? request.DefaultRoleId : user.DefaultRoleId, request.RoleIds, request.ProjectIds);
        await EnsureAdministratorRemainsAsync(userId, request.IsActive &&
            ((request.RoleIds?.Contains(5) ?? false) || (request.DefaultRoleId > 0 ? request.DefaultRoleId : user.DefaultRoleId) == 5));
        user.Name = request.Name?.Trim();
        user.Family = request.Family?.Trim();
        user.Email = request.Email?.Trim();
        user.Mobile = request.Mobile?.Trim();
        user.DefaultRoleId = request.DefaultRoleId > 0 ? request.DefaultRoleId : user.DefaultRoleId;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        // Roles update
        _dbContext.UserRoles.RemoveRange(user.UserRoles);
        var roleIds = request.RoleIds ?? new List<int>();
        if (!roleIds.Contains(user.DefaultRoleId))
            roleIds.Add(user.DefaultRoleId);

        var allRoles = await _dbContext.Roles.AsNoTracking().ToListAsync();
        foreach (var rId in roleIds.Distinct())
        {
            var role = allRoles.FirstOrDefault(r => r.Id == rId);
            _dbContext.UserRoles.Add(new TicketingUserRole
            {
                UserId = user.Id,
                RoleId = rId,
                RoleName = role?.Name ?? $"Role_{rId}"
            });
        }

        // Projects update
        _dbContext.UserProjects.RemoveRange(user.UserProjects);
        if (request.ProjectIds != null)
        {
            foreach (var pId in request.ProjectIds.Distinct())
            {
                _dbContext.UserProjects.Add(new UserProject
                {
                    UserId = user.Id,
                    ProjectId = pId
                });
            }
        }

        await _dbContext.SaveChangesAsync();
        if (operation != null) await operation.CommitAsync();
        return true;
    }

    public async Task<bool> ToggleUserStatusAsync(int userId, bool isActive)
    {
        await using var operation = _dbContext.Database.CurrentTransaction == null
            ? await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable) : null;
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
            return false;

        if (!isActive) await EnsureAdministratorRemainsAsync(userId, false);
        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        if (operation != null) await operation.CommitAsync();
        return true;
    }

    public async Task<bool> ResetUserPasswordAsync(int userId, string newPassword)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
            return false;

        user.PasswordHash = SecurePasswordHasher.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<TeamDto>> GetTeamsAsync()
    {
        var teams = await _dbContext.Teams
            .Include(t => t.Members)
            .AsNoTracking()
            .ToListAsync();

        var users = await _dbContext.Users.AsNoTracking().ToListAsync();

        return teams.Select(t =>
        {
            var leader = users.FirstOrDefault(u => u.Id == t.LeaderUserId);
            return new TeamDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                LeaderUserId = t.LeaderUserId,
                LeaderName = leader != null ? $"{leader.Name} {leader.Family}".Trim() : null,
                IsActive = t.IsActive,
                MemberCount = t.Members.Count,
                MemberUserIds = t.Members.Select(m => m.UserId).ToList(),
                CreatedAt = t.CreatedAt
            };
        }).ToList();
    }

    public async Task<TicketingTeam> CreateTeamAsync(string name, string? description, int? leaderUserId, IEnumerable<int> memberUserIds)
    {
        var team = new TicketingTeam
        {
            Name = name,
            Description = description,
            LeaderUserId = leaderUserId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.Teams.AddAsync(team);
        await _dbContext.SaveChangesAsync();

        if (memberUserIds != null)
        {
            foreach (var uId in memberUserIds.Distinct())
            {
                _dbContext.TeamMembers.Add(new TicketingTeamMember
                {
                    TeamId = team.Id,
                    UserId = uId
                });
            }
            await _dbContext.SaveChangesAsync();
        }

        return team;
    }

    public async Task<bool> UpdateTeamAsync(int teamId, string name, string? description, int? leaderUserId, bool isActive, IEnumerable<int> memberUserIds)
    {
        var team = await _dbContext.Teams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == teamId);

        if (team == null)
            return false;

        team.Name = name;
        team.Description = description;
        team.LeaderUserId = leaderUserId;
        team.IsActive = isActive;

        _dbContext.TeamMembers.RemoveRange(team.Members);
        if (memberUserIds != null)
        {
            foreach (var uId in memberUserIds.Distinct())
            {
                _dbContext.TeamMembers.Add(new TicketingTeamMember
                {
                    TeamId = team.Id,
                    UserId = uId
                });
            }
        }

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteTeamAsync(int teamId)
    {
        var team = await _dbContext.Teams.FindAsync(teamId);
        if (team == null)
            return false;

        _dbContext.Teams.Remove(team);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<Ticketing.Domain.Entities.Project>> GetAllProjectsAsync()
    {
        return new List<Ticketing.Domain.Entities.Project>
        {
            new Ticketing.Domain.Entities.Project { Id = 1, Name = "سیستم مالی سمپاد" }
        };
    }

    public async Task<Ticketing.Domain.Entities.Project> CreateProjectAsync(string name)
    {
        var p = new Ticketing.Domain.Entities.Project { Name = name.Trim() };
        await _dbContext.Projects.AddAsync(p);
        await _dbContext.SaveChangesAsync();
        return p;
    }

    public async Task<bool> UpdateProjectAsync(int projectId, string name)
    {
        var p = await _dbContext.Projects.FindAsync(projectId);
        if (p == null)
            return false;

        p.Name = name.Trim();
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<SlaPolicyDto>> GetSlaPoliciesAsync()
    {
        var policies = await _dbContext.SlaPolicies
            .Include(s => s.Project)
            .AsNoTracking()
            .ToListAsync();

        return policies.Select(s => new SlaPolicyDto
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            Priority = s.Priority,
            PriorityName = s.Priority switch
            {
                1 => "فوری و بالا",
                2 => "متوسط",
                3 => "پایین",
                _ => "همه اولویت‌ها"
            },
            ResponseTimeHours = s.ResponseTimeHours,
            ResolutionTimeHours = s.ResolutionTimeHours,
            ProjectId = s.ProjectId,
            ProjectName = s.Project?.Name,
            IsActive = s.IsActive
        }).ToList();
    }

    public async Task<SlaPolicy> CreateSlaPolicyAsync(SlaPolicyDto dto)
    {
        var policy = new SlaPolicy
        {
            Name = dto.Name,
            Description = dto.Description,
            Priority = dto.Priority,
            ResponseTimeHours = dto.ResponseTimeHours > 0 ? dto.ResponseTimeHours : 4,
            ResolutionTimeHours = dto.ResolutionTimeHours > 0 ? dto.ResolutionTimeHours : 24,
            ProjectId = dto.ProjectId,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.SlaPolicies.AddAsync(policy);
        await _dbContext.SaveChangesAsync();
        return policy;
    }

    public async Task<bool> UpdateSlaPolicyAsync(int id, SlaPolicyDto dto)
    {
        var policy = await _dbContext.SlaPolicies.FindAsync(id);
        if (policy == null)
            return false;

        policy.Name = dto.Name;
        policy.Description = dto.Description;
        policy.Priority = dto.Priority;
        policy.ResponseTimeHours = dto.ResponseTimeHours;
        policy.ResolutionTimeHours = dto.ResolutionTimeHours;
        policy.ProjectId = dto.ProjectId;
        policy.IsActive = dto.IsActive;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteSlaPolicyAsync(int id)
    {
        var policy = await _dbContext.SlaPolicies.FindAsync(id);
        if (policy == null)
            return false;

        _dbContext.SlaPolicies.Remove(policy);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<AppSettingDto>> GetSettingsAsync(string? category = null)
    {
        var query = _dbContext.AppSettings.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(s => s.Category == category);

        var list = await query.ToListAsync();
        return list.Select(s => new AppSettingDto
        {
            Id = s.Id,
            Key = s.Key,
            Value = s.Value,
            Category = s.Category,
            Description = s.Description,
            UpdatedAt = s.UpdatedAt
        }).ToList();
    }

    public async Task<bool> UpdateSettingsAsync(IEnumerable<AppSettingDto> settings)
    {
        foreach (var s in settings)
        {
            var existing = await _dbContext.AppSettings.FirstOrDefaultAsync(x => x.Key == s.Key);
            if (existing != null)
            {
                existing.Value = s.Value;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _dbContext.AppSettings.Add(new AppSetting
                {
                    Key = s.Key,
                    Value = s.Value,
                    Category = s.Category ?? "General",
                    Description = s.Description,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<UserStatsDto> GetUserStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var total = await _dbContext.Users.CountAsync(cancellationToken);
            var active = await _dbContext.Users.CountAsync(u => u.IsActive, cancellationToken);
            return new UserStatsDto
            {
                TotalUsers = total,
                ActiveUsers = active,
                InactiveUsers = Math.Max(0, total - active)
            };
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to count user stats; returning defaults");
            return new UserStatsDto { TotalUsers = 1, ActiveUsers = 1, InactiveUsers = 0 };
        }
    }

    public async Task<IReadOnlyList<UserRoleItemDto>> GetUserRolesAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null) return Array.Empty<UserRoleItemDto>();

            var userRoles = await _dbContext.UserRoles.AsNoTracking().Where(ur => ur.UserId == userId).ToListAsync(cancellationToken);
            List<TicketingRole> allRoles;
            try
            {
                allRoles = await _dbContext.Roles.AsNoTracking().ToListAsync(cancellationToken);
            }
            catch
            {
                allRoles = new List<TicketingRole>();
            }

            return userRoles.Select(ur => {
                var role = allRoles.FirstOrDefault(r => r.Id == ur.RoleId);
                return new UserRoleItemDto
                {
                    RoleId = ur.RoleId,
                    RoleName = ur.RoleName,
                    RoleDisplayName = role?.DisplayName ?? ur.RoleName,
                    IsDefault = ur.RoleId == user.DefaultRoleId
                };
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to get user roles for user {UserId}", userId);
            return Array.Empty<UserRoleItemDto>();
        }
    }

    public async Task<bool> AddUserRoleAsync(int userId, int roleId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);
        var role = await _dbContext.Roles.FindAsync(new object[] { roleId }, cancellationToken);
        if (user == null || role == null) return false;

        bool exists = await _dbContext.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);
        if (exists) return true;

        _dbContext.UserRoles.Add(new TicketingUserRole
        {
            UserId = userId,
            RoleId = roleId,
            RoleName = role.Name
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RemoveUserRoleAsync(int userId, int roleId, CancellationToken cancellationToken = default)
    {
        await using var operation = _dbContext.Database.CurrentTransaction == null
            ? await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable) : null;
        var user = await _dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null) return false;
        if (roleId == 5) await EnsureAdministratorRemainsAsync(userId, false);
        if (user.DefaultRoleId == roleId) return false;

        var ur = await _dbContext.UserRoles.FirstOrDefaultAsync(x => x.UserId == userId && x.RoleId == roleId, cancellationToken);
        if (ur == null) return false;

        _dbContext.UserRoles.Remove(ur);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (operation != null) await operation.CommitAsync();
        return true;
    }

    private async Task ValidateAssignmentsAsync(int defaultRole, IEnumerable<int>? roleIds, IEnumerable<int>? projectIds)
    {
        var roles = (roleIds ?? Array.Empty<int>()).Append(defaultRole).Distinct().ToArray();
        var projects = (projectIds ?? Array.Empty<int>()).Distinct().ToArray();
        if (roles.Length > 100 || projects.Length > 100 || roles.Any(r => r <= 0) || projects.Any(id => id <= 0))
            throw new AdminValidationException("نقش‌ها یا سامانه‌های انتخاب‌شده معتبر نیستند.");
        if (await _dbContext.Roles.CountAsync(r => roles.Contains(r.Id) && r.IsActive) != roles.Length)
            throw new AdminValidationException("نقش انتخاب‌شده وجود ندارد یا غیرفعال است.");
        if (await _dbContext.Projects.CountAsync(p => projects.Contains(p.Id)) != projects.Length)
            throw new AdminValidationException("سامانه انتخاب‌شده وجود ندارد.");
    }

    private async Task EnsureAdministratorRemainsAsync(int userId, bool remainsAdministrator)
    {
        if (remainsAdministrator) return;
        bool administrator = await _dbContext.Users.AnyAsync(u => u.Id == userId && u.IsActive && u.UserRoles.Any(r => r.RoleId == 5));
        if (administrator && !await _dbContext.Users.AnyAsync(u => u.Id != userId && u.IsActive && u.UserRoles.Any(r => r.RoleId == 5)))
            throw new AdminValidationException("آخرین مدیر فعال سامانه را نمی‌توان غیرفعال یا از نقش مدیریت خارج کرد.");
    }
}

public sealed class AdminValidationException(string message) : InvalidOperationException(message);
