using Microsoft.EntityFrameworkCore;
using AracSatisSistemi.Models;

namespace AracSatisSistemi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Dosya> Dosyalar => Set<Dosya>();
        public DbSet<Satis> Satislar => Set<Satis>();
        public DbSet<IptalIade> IptalIadeler => Set<IptalIade>();

        public DbSet<AracMarkasi> AracMarkalari => Set<AracMarkasi>();
        public DbSet<AracCinsi> AracCinsleri => Set<AracCinsi>();
        public DbSet<VergiDairesiKaydi> VergiDaireleri => Set<VergiDairesiKaydi>();
        public DbSet<OtoparkKaydi> Otoparklar => Set<OtoparkKaydi>();
        public DbSet<AracRengi> AracRenkleri => Set<AracRengi>();

        public DbSet<KomisyonAyari> KomisyonAyarlari => Set<KomisyonAyari>();
        public DbSet<GenelAyar> GenelAyarlar => Set<GenelAyar>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Dosya>()
                .HasIndex(d => d.DosyaNo)
                .IsUnique();

            modelBuilder.Entity<Dosya>()
                .HasMany(d => d.Satislar)
                .WithOne(s => s.Dosya!)
                .HasForeignKey(s => s.DosyaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Dosya>()
                .HasOne(d => d.IptalIade)
                .WithOne(i => i.Dosya!)
                .HasForeignKey<IptalIade>(i => i.DosyaId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
