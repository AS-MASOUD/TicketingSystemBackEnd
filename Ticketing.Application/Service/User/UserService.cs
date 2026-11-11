using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Ticketing.Application.Security;
using Ticketing.Domain.Contracts;
using Ticketing.Domain.Entities;
using Ticketing.Infrastructure.Database;

namespace Ticketing.Application.Service.User;

public class UserService : IUserService
{
    private readonly TicketingDbContext _dbContext;

    public UserService(TicketingDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<TicketingUser> AddAsync(TicketingUser entity)
    {
        await _dbContext.Users.AddAsync(entity);
        await _dbContext.SaveChangesAsync();
        return entity;
    }

    public async Task<TicketingUser> UpdateAsync(TicketingUser entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _dbContext.Users.Update(entity);
        await _dbContext.SaveChangesAsync();
        return entity;
    }

    public async Task<bool> DeleteAsync(TicketingUser entity)
    {
        _dbContext.Users.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<TicketingUser> GetAsync(Expression<Func<TicketingUser, bool>> expression)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(expression);
    }

    public async Task<List<TicketingUser>> ListAsync(Expression<Func<TicketingUser, bool>>? expression)
    {
        if (expression == null)
            return await _dbContext.Users.ToListAsync();
        return await _dbContext.Users.Where(expression).ToListAsync();
    }

    public async Task<TicketingUser?> GetByUsernameWithRolesAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return null;

        var normalized = username.Trim().ToLowerInvariant();
        return await _dbContext.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == normalized);
    }

    public async Task<TicketingUser?> GetByIdWithRolesAsync(int id)
    {
        return await _dbContext.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public Task<bool> VerifyPasswordAsync(TicketingUser user, string password)
    {
        if (user == null || !user.IsActive)
            return Task.FromResult(false);

        var isValid = SecurePasswordHasher.VerifyPassword(password, user.PasswordHash);
        return Task.FromResult(isValid);
    }

    public async Task SetPasswordAsync(TicketingUser user, string newPassword)
    {
        user.PasswordHash = SecurePasswordHasher.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<TicketingUser> CreateUserAsync(
        string username,
        string password,
        string? name,
        string? family,
        string? mobile,
        int defaultRoleId,
        IEnumerable<int> roleIds,
        int? explicitId = null)
    {
        var hash = SecurePasswordHasher.HashPassword(password);
        var user = new TicketingUser
        {
            Username = username.Trim(),
            PasswordHash = hash,
            Name = name,
            Family = family,
            Mobile = mobile,
            DefaultRoleId = defaultRoleId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        if (explicitId.HasValue)
        {
            user.Id = explicitId.Value;
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _dbContext.Database.BeginTransactionAsync();
                try
                {
                    await _dbContext.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[TicketingUsers] ON;");
                    await _dbContext.Users.AddAsync(user);
                    await _dbContext.SaveChangesAsync();
                    await _dbContext.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[TicketingUsers] OFF;");
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
        else
        {
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();
        }

        foreach (var rId in roleIds.Distinct())
        {
            string roleName = GetRoleName(rId);
            _dbContext.UserRoles.Add(new TicketingUserRole
            {
                UserId = user.Id,
                RoleId = rId,
                RoleName = roleName
            });
        }
        await _dbContext.SaveChangesAsync();

        return user;
    }

    public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null || !user.IsActive)
            return false;

        if (!SecurePasswordHasher.VerifyPassword(oldPassword, user.PasswordHash))
            return false;

        await SetPasswordAsync(user, newPassword);
        return true;
    }

    public async Task<IReadOnlyList<TicketingUserRole>> GetUserRolesAsync(int userId)
    {
        return await _dbContext.UserRoles
            .Where(r => r.UserId == userId)
            .ToListAsync();
    }

    public async Task EnsureSeedUsersAsync()
    {
        // بررسی وجود کاربر adminvira (شناسه ۳ تاریخی)
        var viraUser = await _dbContext.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Username == "adminvira");
        if (viraUser == null)
        {
            await CreateUserAsync(
                username: "adminvira",
                password: "Admin@Vira#2026",
                name: "مدیر",
                family: "ویرا",
                mobile: "09120000001",
                defaultRoleId: 5,
                roleIds: new[] { 5 },
                explicitId: 3
            );
        }

        // بررسی وجود کاربر admintaz (شناسه ۲ تاریخی)
        var tazUser = await _dbContext.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Username == "admintaz");
        if (tazUser == null)
        {
            await CreateUserAsync(
                username: "admintaz",
                password: "Admin@Taz#2026",
                name: "مدیر",
                family: "تعزیرات",
                mobile: "09120000002",
                defaultRoleId: 4,
                roleIds: new[] { 4 },
                explicitId: 2
            );
        }

        // بررسی وجود یک کاربر عادی پروژه جهت تست دسترسی‌های محدود
        var regularUser = await _dbContext.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Username == "mrnadaf");
        if (regularUser == null)
        {
            await CreateUserAsync(
                username: "mrnadaf",
                password: "User@123456",
                name: "کاربر",
                family: "ناداف",
                mobile: "09120000014",
                defaultRoleId: 2,
                roleIds: new[] { 2 },
                explicitId: 14
            );
        }
    }

    private static string GetRoleName(int roleId) => roleId switch
    {
        1 => "کارفرما / کارشناس",
        2 => "برنامه‌نویس و کارشناس",
        3 => "ناظر پروژه",
        4 => "مدیر فنی پروژه",
        5 => "مدیر ارشد سامانه",
        _ => "کارفرما / کارشناس"
    };
}
