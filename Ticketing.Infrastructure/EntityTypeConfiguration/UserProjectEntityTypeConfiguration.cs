using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketing.Domain.Entities;

namespace Ticketing.Infrastructure.EntityTypeConfiguration;

public class UserProjectEntityTypeConfiguration : IEntityTypeConfiguration<UserProject>
{
    public void Configure(EntityTypeBuilder<UserProject> builder)
    {
        builder.ToTable("UserProjects");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.UserId, x.ProjectId }).IsUnique();
        builder.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("GETDATE()");

        builder.HasOne(x => x.Project)
               .WithMany()
               .HasForeignKey(x => x.ProjectId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
