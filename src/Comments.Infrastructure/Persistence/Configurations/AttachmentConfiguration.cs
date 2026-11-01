using Comments.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comments.Infrastructure.Persistence.Configurations;

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> b)
    {
        b.ToTable("attachments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Kind).HasConversion<short>();
        b.Property(x => x.StoragePath).HasMaxLength(512).IsRequired();
        b.Property(x => x.OriginalName).HasMaxLength(255).IsRequired();
        b.HasIndex(x => x.CommentId);
    }
}
