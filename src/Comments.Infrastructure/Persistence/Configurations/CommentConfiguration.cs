using Comments.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comments.Infrastructure.Persistence.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> b)
    {
        b.ToTable("comments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Text).HasMaxLength(5000).IsRequired();
        b.Property(x => x.TextHtml).HasMaxLength(20000).IsRequired();

        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Parent).WithMany(x => x.Replies).HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Attachments).WithOne().HasForeignKey(x => x.CommentId).OnDelete(DeleteBehavior.Cascade);

        b.Navigation(x => x.Replies).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(x => x.Attachments).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Корневые комментарии: LIFO и пагинация; ответы к родителю.
        b.HasIndex(x => new { x.ParentId, x.CreatedAt }).IsDescending(false, true);
        b.HasIndex(x => x.UserId);
    }
}
