using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MyBoards.Entities.Configurations
{
    public class WorkItemConfiguration : IEntityTypeConfiguration<WorkItem>
    {
        public void Configure(EntityTypeBuilder<WorkItem> eb)
        {
            eb.HasOne(wi => wi.State)
               .WithMany() // stan moze miec wiele WorkItemów
               .HasForeignKey(s => s.StateId);


            eb.Property(wi => wi.Area).HasColumnType("varchar(200)");
            eb.Property(wi => wi.IterationPath).HasColumnName("Iteration_Path");
            eb.Property(wi => wi.Priority).HasDefaultValue(1);

            eb.HasMany(wi => wi.Comments)   // relacja jeden do wielu
            .WithOne(c => c.WorkItem)
            .HasForeignKey(c => c.WorkItemId);

            eb.HasOne(wi => wi.Author)    // relacja jeden do wielu 
            .WithMany(u => u.WorkItems)
            .HasForeignKey(wi => wi.AuthorId);

            // relacja wiele do wielu z tabelą łączącą co zawiera konkretne powiązania

            eb.HasMany(wi => wi.Tags)
            .WithMany(t => t.WorkItems)
            .UsingEntity<WorkItemTag>(
                 wi => wi.HasOne(wit => wit.Tag)
                 .WithMany()
                 .HasForeignKey(wit => wit.TagId),

                 wi => wi.HasOne(wit => wit.WorkItem)
                 .WithMany()
                 .HasForeignKey(wit => wit.WorkItemId),

                 wit =>
                 {
                     wit.HasKey(k => new { k.TagId, k.WorkItemId });
                     wit.Property(p => p.PublicationDate).HasDefaultValueSql("getutcdate()");
                 }

                 );
        }
    }
}
