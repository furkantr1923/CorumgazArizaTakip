using Microsoft.EntityFrameworkCore;

namespace CorumgazArizaTakip.Models
{
    public class ArizaDbContext : DbContext
    {
        public ArizaDbContext(DbContextOptions<ArizaDbContext> options) : base(options)
        {
        }

        public DbSet<Ariza> Arizalar { get; set; }
        public DbSet<Kullanici> Kullanicilar { get; set; }
        public DbSet<Teknisyen> Teknisyenler { get; set; }
        public DbSet<DurumGecmisi> DurumGecmisleri { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Ariza>()
                .HasIndex(a => a.TakipKodu)
                .IsUnique();

            modelBuilder.Entity<Kullanici>()
                .HasIndex(k => k.KullaniciAdi)
                .IsUnique();
        }

        public override int SaveChanges()
        {
            TakipKodlariniKontrolEt();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            TakipKodlariniKontrolEt();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void TakipKodlariniKontrolEt()
        {
            var yeniArizalar = ChangeTracker.Entries<Ariza>()
                .Where(e => e.State == EntityState.Added)
                .Select(e => e.Entity)
                .ToList();

            var buIslemdeKullanilan = new HashSet<string>();

            foreach (var ariza in yeniArizalar)
            {
                while (string.IsNullOrEmpty(ariza.TakipKodu)
                       || buIslemdeKullanilan.Contains(ariza.TakipKodu)
                       || Arizalar.Any(a => a.TakipKodu == ariza.TakipKodu))
                {
                    ariza.TakipKodu = Ariza.YeniTakipKodu();
                }

                buIslemdeKullanilan.Add(ariza.TakipKodu);
            }
        }
    }
}