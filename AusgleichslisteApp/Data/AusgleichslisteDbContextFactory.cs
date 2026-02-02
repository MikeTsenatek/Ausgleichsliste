using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AusgleichslisteApp.Data
{
    /// <summary>
    /// Design-time DbContext Factory für Entity Framework Migrations
    /// </summary>
    public class AusgleichslisteDbContextFactory : IDesignTimeDbContextFactory<AusgleichslisteDbContext>
    {
        public AusgleichslisteDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AusgleichslisteDbContext>();
            
            // Verwende eine Standard-Connection für Design-Time
            optionsBuilder.UseSqlite("Data Source=ausgleichsliste.db");
            
            return new AusgleichslisteDbContext(optionsBuilder.Options);
        }
    }
}