using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace FileAnalysisService.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<AnalysisResult> AnalysisResults { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<AnalysisResult>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FileId).IsRequired();
                entity.Property(e => e.ParagraphCount).IsRequired();
                entity.Property(e => e.WordCount).IsRequired();
                entity.Property(e => e.CharacterCount).IsRequired();
                entity.Property(e => e.WordCloudLocation).IsRequired(false);
                entity.Property(e => e.AnalyzedAt).IsRequired();
            });
        }
    }
}
