using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MyBoards.Entities.Configurations
{
    public class CommentConfiguration : IEntityTypeConfiguration<Comment>
    {
        public void Configure(EntityTypeBuilder<Comment> eb)
        {
            eb.Property(c => c.CreatedDate).HasDefaultValueSql("getutcdate()"); // format aktualnej daty sql
            eb.Property(c => c.UpdatedDate).ValueGeneratedOnUpdate();

            // relacja jeden do wielu

            eb.HasOne(c => c.Author)
                .WithMany(a => a.Comments)
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.ClientCascade);  // automatyczne kaskadowe usuwanie 
        }
    }
}
