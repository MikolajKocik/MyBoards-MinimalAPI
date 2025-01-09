using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
using MyBoards.Entities.ViewModels;

namespace MyBoards.Entities
{
    public class MyBoardsContext : DbContext
    {
        public MyBoardsContext(DbContextOptions<MyBoardsContext> options) : base(options)
        {

        }
        public DbSet<WorkItem> WorkItems { get; set; }
        public DbSet<Issue> Issues { get; set; }
        public DbSet<Epic> Epics { get; set; }
        public DbSet<Task> Tasks { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<WorkItemState> WorkItemStates { get; set; }
        public DbSet<WorkItemTag> WorkItemTag { get; set; } // dla tabeli łączącej
        public DbSet<TopAuthor> ViewTopAuthors { get; set; } // ViewModel (działania takie jak dodawanie,
                                                             // usuwanie, modyfikacja nie są możliwe
                                                             // dla DbSet View modelu

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<WorkItemState>()
                .Property(s => s.Value)
                .IsRequired()
                .HasMaxLength(60);

            modelBuilder.Entity<Epic>()
                .Property(e => e.EndDate)
                .HasPrecision(3);

            modelBuilder.Entity<Task>(eb =>
            {
                eb.Property(t => t.Activity)
                .HasMaxLength(200);
                eb.Property(t => t.RemaingWork)
                .HasPrecision(14, 2);
            });

            modelBuilder.Entity<Issue>()
                .Property(i => i.Efford)
                .HasColumnType("decimal(5,2)");

            // relacja jeden do wielu z nową encją WorkItemState (nowa tabela)

            modelBuilder.Entity<WorkItem>(eb =>
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
            });

            modelBuilder.Entity<Comment>(eb =>
            {
                eb.Property(c => c.CreatedDate).HasDefaultValueSql("getutcdate()"); // format aktualnej daty sql
                eb.Property(c => c.UpdatedDate).ValueGeneratedOnUpdate();
            });

            modelBuilder.Entity<Comment>()  // relacja jeden do wielu
                .HasOne(c => c.Author)
                .WithMany(a => a.Comments)
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.ClientCascade);  // automatyczne kaskadowe usuwanie 

            // relacja modelu 1 do 1 między tabelami 'User' a 'Addresses'

            modelBuilder.Entity<User>()
                .HasOne(u => u.Address)
                .WithOne(a => a.User)
                .HasForeignKey<Address>(a => a.UserId);

            // podejście model seed data

            modelBuilder.Entity<WorkItemState>()
                .HasData(new WorkItemState() { Id = 1, Value = "To Do" },
                new WorkItemState() { Id = 2, Value = "Doing" },
                new WorkItemState() { Id = 3, Value = "Done" });
            // musimy okreslić konkretne wartości do seedowania dla tych 3 nowych obiektów dla EF

            // view model

            modelBuilder.Entity<TopAuthor>(eb =>
            {
                // mapujemy widok do danej encji
                eb.ToView("View_TopAuthors");
                eb.HasNoKey();
            });
        }

    }
}
