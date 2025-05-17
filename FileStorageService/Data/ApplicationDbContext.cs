using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace FileStorageService.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<FileMetadata> Files { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<FileMetadata>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.Hash).IsRequired();
                entity.Property(e => e.Location).IsRequired();
                entity.Property(e => e.UploadedAt).IsRequired();
            });
        }
    }
}
