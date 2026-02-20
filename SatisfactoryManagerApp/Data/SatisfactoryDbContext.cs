using Microsoft.EntityFrameworkCore;
using SatisfactoryManagerApp.Models;

namespace SatisfactoryManagerApp.Data
{
    public class SatisfactoryDbContext : DbContext
    {
        // Tablas reales en la base de datos SQLite
        public DbSet<Item> Items { get; set; }
        public DbSet<Machine> Machines { get; set; }
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<RecipeIngredient> RecipeIngredients { get; set; }

        // Aquí le decimos a .NET qué motor de base de datos usar y dónde guardarlo
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Esto creará un archivo llamado "satisfactory.db" 
            // en la carpeta donde se ejecute tu programa.
            optionsBuilder.UseSqlite("Data Source=satisfactory.db");
        }
    }
}