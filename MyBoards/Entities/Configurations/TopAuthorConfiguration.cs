using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MyBoards.Entities.Configurations
{
    public class TopAuthorConfiguration : IEntityTypeConfiguration<ViewModels.TopAuthor>
    {
        public void Configure(EntityTypeBuilder<ViewModels.TopAuthor> eb)
        {
            eb.ToView("View_TopAuthors");
            eb.HasNoKey();
        }
    }
}
