using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketing.Domain.Entities;

namespace Ticketing.Infrastructure.EntityTypeConfiguration;

public class TicketingTeamMemberEntityTypeConfiguration : IEntityTypeConfiguration<TicketingTeamMember>
{
    public void Configure(EntityTypeBuilder<TicketingTeamMember> builder)
    {
        builder.ToTable("TicketingTeamMembers");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TeamId, x.UserId }).IsUnique();
        builder.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("GETDATE()");

        builder.HasOne(x => x.User)
               .WithMany()
               .HasForeignKey(x => x.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
