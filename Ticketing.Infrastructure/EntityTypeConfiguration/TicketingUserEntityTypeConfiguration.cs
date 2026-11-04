using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketing.Domain.Entities;

namespace Ticketing.Infrastructure.EntityTypeConfiguration;

public class TicketingUserEntityTypeConfiguration : IEntityTypeConfiguration<TicketingUser>
{
    public void Configure(EntityTypeBuilder<TicketingUser> builder)
    {
        builder.ToTable("TicketingUsers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Username).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Username).IsUnique();
        builder.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100);
        builder.Property(x => x.Family).HasMaxLength(100);
        builder.Property(x => x.Mobile).HasMaxLength(50);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.DefaultRoleId).IsRequired().HasDefaultValue(2);
        builder.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("GETDATE()");

        builder.HasMany(x => x.UserRoles)
               .WithOne(x => x.User)
               .HasForeignKey(x => x.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
