using Disney.WebAPI.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Disney.WebAPI
{
    public class DisneyDbContext : IdentityDbContext
    {
        //dotnet ef migrations add Initial -o Infrastructure/DisneyMigrations
        //dotnet ef migrations script FifthMigrationName ThirdMigrationName
        //dotnet ef migrations remove
        //dotnet ef database update

        public DisneyDbContext() { }

        public DisneyDbContext(DbContextOptions<DisneyDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var connectionString = configuration.GetConnectionString("AppDb");
            optionsBuilder.UseSqlServer(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Personaje> Personaje { get; set; }
        public DbSet<Pelicula> Pelicula { get; set; }
        public DbSet<Genero> Genero { get; set; }
    }
}
