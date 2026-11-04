using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketing.Domain.Entities;

namespace Ticketing.Infrastructure.EntityTypeConfiguration;

public class TicketingUserRoleEntityTypeConfiguration : IEntityTypeConfiguration<TicketingUserRole>
{
    public void Configure(EntityTypeBuilder<TicketingUserRole> builder)
    {
        builder.ToTable("TicketingUserRoles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RoleId).IsRequired();
        builder.Property(x => x.RoleName).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();
    }
}
