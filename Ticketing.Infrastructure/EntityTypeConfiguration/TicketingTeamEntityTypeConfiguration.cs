using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketing.Domain.Entities;

namespace Ticketing.Infrastructure.EntityTypeConfiguration;

public class TicketingTeamEntityTypeConfiguration : IEntityTypeConfiguration<TicketingTeam>
{
    public void Configure(EntityTypeBuilder<TicketingTeam> builder)
    {
        builder.ToTable("TicketingTeams");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("GETDATE()");

        builder.HasMany(x => x.Members)
               .WithOne(x => x.Team)
               .HasForeignKey(x => x.TeamId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
