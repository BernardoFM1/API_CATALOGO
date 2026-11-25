using APIVitrine.Models;
using Microsoft.EntityFrameworkCore;

namespace APIVitrine.Context
{
    public class VitrineDbContext : DbContext
    {
        public VitrineDbContext(DbContextOptions<VitrineDbContext> options) : base(options)
        {
        }

        public DbSet<ProdutoVitrine> ProdutosVitrine { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ProdutoVitrine>()
                .Property(p => p.Preco)
                .HasPrecision(10, 2);
        }
    }
}
