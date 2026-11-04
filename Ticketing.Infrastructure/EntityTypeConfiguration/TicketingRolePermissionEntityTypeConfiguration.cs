using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketing.Domain.Entities;

namespace Ticketing.Infrastructure.EntityTypeConfiguration;

public class TicketingRolePermissionEntityTypeConfiguration : IEntityTypeConfiguration<TicketingRolePermission>
{
    public void Configure(EntityTypeBuilder<TicketingRolePermission> builder)
    {
        builder.ToTable("TicketingRolePermissions");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();
    }
}
