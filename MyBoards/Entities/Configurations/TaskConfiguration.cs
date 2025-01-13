using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MyBoards.Entities.Configurations
{
    public class TaskConfiguration : IEntityTypeConfiguration<Task>
    {
        public void Configure(EntityTypeBuilder<Task> eb)
        {
            eb.Property(t => t.Activity)
             .HasMaxLength(200);
            eb.Property(t => t.RemaingWork)
            .HasPrecision(14, 2);
        }
    }
}
