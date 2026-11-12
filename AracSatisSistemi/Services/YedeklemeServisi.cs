namespace AracSatisSistemi.Services
{
    // Veritabanının günlük ve haftalık (her Cuma) otomatik yedeğini alır. Uygulama
    // günlerce yeniden başlamadan açık kalsa bile (dotnet watch veya sunucuda systemd
    // ile) periyodik kontrol sayesinde yedekler alınmaya devam eder - yalnızca "uygulama
    // açılışında bir kere" yedek almak, uygulama hiç kapanmadan günler/haftalar geçerse
    // yetersiz kalırdı.
    //
    // - Günlük yedekler Backups/Gunluk/ klasörüne, günde bir kez alınır; en son
    //   GunlukSaklamaAdedi kadarı tutulur, eskileri silinir.
    // - Haftalık yedekler Backups/Haftalik/ klasörüne, her Cuma bir kez alınır ve
    //   HİÇBİR ZAMAN silinmez (kalıcı arşiv).
    public class YedeklemeServisi : BackgroundService
    {
        private const int GunlukSaklamaAdedi = 60;
        private static readonly TimeSpan KontrolAraligi = TimeSpan.FromHours(1);

        private readonly string _dbYolu;
        private readonly string _gunlukKlasor;
        private readonly string _haftalikKlasor;
        private readonly ILogger<YedeklemeServisi> _logger;

        public YedeklemeServisi(IWebHostEnvironment env, ILogger<YedeklemeServisi> logger)
        {
            _dbYolu = Path.Combine(env.ContentRootPath, "AracSatis.db");
            var kokKlasor = Path.Combine(env.ContentRootPath, "Backups");
            _gunlukKlasor = Path.Combine(kokKlasor, "Gunluk");
            _haftalikKlasor = Path.Combine(kokKlasor, "Haftalik");
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    YedekAl();
                }
                catch (Exception ex)
                {
                    // Yedekleme başarısız olsa bile uygulama/servis çalışmaya devam etsin.
                    _logger.LogError(ex, "Otomatik veritabanı yedeklemesi başarısız oldu.");
                }

                try
                {
                    await Task.Delay(KontrolAraligi, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Uygulama kapatılırken normal.
                }
            }
        }

        private void YedekAl()
        {
            if (!File.Exists(_dbYolu)) return;

            Directory.CreateDirectory(_gunlukKlasor);
            Directory.CreateDirectory(_haftalikKlasor);

            var bugun = DateTime.Now;

            // GÜNLÜK: bugüne ait bir yedek yoksa al.
            var gunlukYol = Path.Combine(_gunlukKlasor, $"AracSatis_{bugun:yyyyMMdd}.db");
            if (!File.Exists(gunlukYol))
            {
                File.Copy(_dbYolu, gunlukYol, overwrite: true);
            }

            // En son GunlukSaklamaAdedi kadar günlük yedek tutulur, eskiler silinir.
            var fazlaGunlukler = Directory.GetFiles(_gunlukKlasor, "AracSatis_*.db")
                .OrderByDescending(f => f)
                .Skip(GunlukSaklamaAdedi);
            foreach (var eski in fazlaGunlukler) File.Delete(eski);

            // HAFTALIK: bugün Cuma ise ve bu Cuma için henüz yedek alınmadıysa al.
            // Bu klasördeki dosyalar hiçbir zaman silinmez (kalıcı arşiv).
            if (bugun.DayOfWeek == DayOfWeek.Friday)
            {
                var haftalikYol = Path.Combine(_haftalikKlasor, $"AracSatis_Haftalik_{bugun:yyyyMMdd}.db");
                if (!File.Exists(haftalikYol))
                {
                    File.Copy(_dbYolu, haftalikYol, overwrite: true);
                }
            }
        }
    }
}
