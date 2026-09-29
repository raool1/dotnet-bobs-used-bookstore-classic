using Bookstore.Domain.Addresses;
using Bookstore.Domain.Books;
using Bookstore.Domain.Carts;
using Bookstore.Domain.Customers;
using Bookstore.Domain.Offers;
using Bookstore.Domain.Orders;
using Bookstore.Domain.ReferenceData;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Address> Address { get; set; }

        public DbSet<Book> Book { get; set; }

        public DbSet<Customer> Customer { get; set; }

        public DbSet<Order> Order { get; set; }

        public DbSet<ShoppingCart> ShoppingCart { get; set; }

        public DbSet<OrderItem> OrderItem { get; set; }

        public DbSet<Offer> Offer { get; set; }

        public DbSet<ReferenceDataItem> ReferenceData { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>().Property(x => x.Sub).HasColumnType("nvarchar").HasMaxLength(450);
            modelBuilder.Entity<Customer>().HasIndex(x => x.Sub).IsUnique();

            modelBuilder.Entity<Book>().HasOne(x => x.Publisher).WithMany().HasForeignKey(x => x.PublisherId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Book>().HasOne(x => x.BookType).WithMany().HasForeignKey(x => x.BookTypeId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Book>().HasOne(x => x.Genre).WithMany().HasForeignKey(x => x.GenreId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Book>().HasOne(x => x.Condition).WithMany().HasForeignKey(x => x.ConditionId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Offer>().HasOne(x => x.Publisher).WithMany().HasForeignKey(x => x.PublisherId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Offer>().HasOne(x => x.BookType).WithMany().HasForeignKey(x => x.BookTypeId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Offer>().HasOne(x => x.Genre).WithMany().HasForeignKey(x => x.GenreId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Offer>().HasOne(x => x.Condition).WithMany().HasForeignKey(x => x.ConditionId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>().HasOne(x => x.Customer).WithMany().OnDelete(DeleteBehavior.Restrict);

            // Update the Reference Data Table to Match the modern version
            modelBuilder.Entity<ReferenceDataItem>().ToTable("ReferenceData");

            modelBuilder.Entity<ShoppingCartItem>().HasKey(x => new { x.Id, x.ShoppingCartId });
            modelBuilder.Entity<ShoppingCartItem>().Property(x => x.Id).ValueGeneratedOnAdd();

            // Seed data (migrated from BookstoreDbInitializer)
            modelBuilder.Entity<ReferenceDataItem>().HasData(
                new { Id = 1, DataType = ReferenceDataType.BookType, Text = "Hardcover" },
                new { Id = 2, DataType = ReferenceDataType.BookType, Text = "Trade Paperback" },
                new { Id = 3, DataType = ReferenceDataType.BookType, Text = "Mass Market Paperback" },
                new { Id = 4, DataType = ReferenceDataType.Condition, Text = "New" },
                new { Id = 5, DataType = ReferenceDataType.Condition, Text = "Like New" },
                new { Id = 6, DataType = ReferenceDataType.Condition, Text = "Good" },
                new { Id = 7, DataType = ReferenceDataType.Condition, Text = "Acceptable" },
                new { Id = 8, DataType = ReferenceDataType.Genre, Text = "Biographies" },
                new { Id = 9, DataType = ReferenceDataType.Genre, Text = "Children's Books" },
                new { Id = 10, DataType = ReferenceDataType.Genre, Text = "History" },
                new { Id = 11, DataType = ReferenceDataType.Genre, Text = "Literature & Fiction" },
                new { Id = 12, DataType = ReferenceDataType.Genre, Text = "Mystery, Thriller & Suspense" },
                new { Id = 13, DataType = ReferenceDataType.Genre, Text = "Science Fiction & Fantasy" },
                new { Id = 14, DataType = ReferenceDataType.Genre, Text = "Travel" },
                new { Id = 15, DataType = ReferenceDataType.Publisher, Text = "Arcadia Books" },
                new { Id = 16, DataType = ReferenceDataType.Publisher, Text = "Astral Publishing" },
                new { Id = 17, DataType = ReferenceDataType.Publisher, Text = "Moonlight Publishing" },
                new { Id = 18, DataType = ReferenceDataType.Publisher, Text = "Dreamscape Press" },
                new { Id = 19, DataType = ReferenceDataType.Publisher, Text = "Enchanted Library" },
                new { Id = 20, DataType = ReferenceDataType.Publisher, Text = "Fantasia House" },
                new { Id = 21, DataType = ReferenceDataType.Publisher, Text = "Horizon Books" },
                new { Id = 22, DataType = ReferenceDataType.Publisher, Text = "Infinity Press" },
                new { Id = 23, DataType = ReferenceDataType.Publisher, Text = "Paradigm Publishing" },
                new { Id = 24, DataType = ReferenceDataType.Publisher, Text = "Aurora Publishing" }
            );

            modelBuilder.Entity<Book>().HasData(
                new { Id = 1, Name = "2020: The Apocalypse", Author = "Li Juan", ISBN = "6556784356", PublisherId = 15, BookTypeId = 1, GenreId = 13, ConditionId = 5, Price = 10.95M, Quantity = 25, Year = (int?)null, Summary = (string)null, CoverImageUrl = "/Content/Images/coverimages/apocalypse.png" },
                new { Id = 2, Name = "Children Of Iron", Author = "Nikki Wolf", ISBN = "7665438976", PublisherId = 16, BookTypeId = 1, GenreId = 11, ConditionId = 6, Price = 13.95M, Quantity = 3, Year = (int?)null, Summary = (string)null, CoverImageUrl = "/Content/Images/coverimages/childrenofiron.png" },
                new { Id = 3, Name = "Gold In The Dark", Author = "Richard Roe", ISBN = "5442280765", PublisherId = 17, BookTypeId = 1, GenreId = 13, ConditionId = 5, Price = 6.50M, Quantity = 10, Year = (int?)null, Summary = (string)null, CoverImageUrl = "/Content/Images/coverimages/goldinthedark.png" },
                new { Id = 4, Name = "Leagues Of Smoke", Author = "Pat Candella", ISBN = "4556789542", PublisherId = 18, BookTypeId = 2, GenreId = 11, ConditionId = 7, Price = 3M, Quantity = 1, Year = (int?)null, Summary = (string)null, CoverImageUrl = "/Content/Images/coverimages/leaguesofsmoke.png" },
                new { Id = 5, Name = "Alone With The Stars", Author = "Carlos Salazar", ISBN = "4563358087", PublisherId = 19, BookTypeId = 2, GenreId = 12, ConditionId = 5, Price = 15.95M, Quantity = 5, Year = (int?)null, Summary = (string)null, CoverImageUrl = "/Content/Images/coverimages/alonewiththestars.png" },
                new { Id = 6, Name = "The Girl In The Polaroid", Author = "Terri Whitlock", ISBN = "2354435678", PublisherId = 20, BookTypeId = 1, GenreId = 12, ConditionId = 6, Price = 8.25M, Quantity = 2, Year = (int?)null, Summary = (string)null, CoverImageUrl = "/Content/Images/coverimages/girlinthepolaroid.png" },
                new { Id = 7, Name = "1001 Jokes", Author = "Mary Major", ISBN = "6554789632", PublisherId = 21, BookTypeId = 2, GenreId = 11, ConditionId = 5, Price = 13.95M, Quantity = 7, Year = (int?)null, Summary = (string)null, CoverImageUrl = "/Content/Images/coverimages/1001jokes.png" },
                new { Id = 8, Name = "My Search For Meaning", Author = "Mateo Jackson", ISBN = "4558786554", PublisherId = 22, BookTypeId = 3, GenreId = 8, ConditionId = 7, Price = 5M, Quantity = 15, Year = (int?)null, Summary = (string)null, CoverImageUrl = "/Content/Images/coverimages/mysearchformeaning.png" }
            );
        }
    }
}