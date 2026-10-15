using CommentsApp.CommentsApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CommentsApp.CommentsApp.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<FileEntity> Files => Set<FileEntity>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.Property(x => x.UserName).HasMaxLength(50).IsRequired();
            e.Property(x => x.Email).HasMaxLength(254).IsRequired();
            e.Property(x => x.Homepage).HasMaxLength(500);
            e.HasIndex(x => x.UserName);
            e.HasIndex(x => x.Email).IsUnique();
        });

        b.Entity<Comment>(e =>
        {
            e.Property(x => x.Text).IsRequired();
            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => x.ParentCommentId);

            e.HasOne(x => x.ParentComment)
             .WithMany(x => x.Replies)
             .HasForeignKey(x => x.ParentCommentId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.User)
             .WithMany(x => x.Comments)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<FileEntity>(e =>
        {
            e.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            e.Property(x => x.StoredName).HasMaxLength(120).IsRequired();
            e.Property(x => x.MimeType).HasMaxLength(100).IsRequired();

            e.HasOne(x => x.Comment)
             .WithOne(x => x.File)
             .HasForeignKey<FileEntity>(x => x.CommentId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

