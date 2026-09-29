namespace Bookstore.Data
{
    // This class previously inherited from EF6's DropCreateDatabaseIfModelChanges<ApplicationDbContext>
    // and contained seed data in a Seed() override. EF Core has no equivalent initializer pattern.
    // The seed data has been migrated to HasData() calls in ApplicationDbContext.OnModelCreating,
    // and the Database.SetInitializer() call has been removed.
    // This class is retained empty to avoid deleting a source file; it can be safely removed.
    public class BookstoreDbInitializer
    {
    }
}