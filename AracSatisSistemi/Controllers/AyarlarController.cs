using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AracSatisSistemi.Data;
using AracSatisSistemi.Models;

namespace AracSatisSistemi.Controllers
{
    public class AyarlarController : Controller
    {
        private readonly AppDbContext _db;
        public AyarlarController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            ViewBag.Komisyon = await _db.KomisyonAyarlari.FirstOrDefaultAsync();
            ViewBag.GenelAyar = await _db.GenelAyarlar.FirstOrDefaultAsync();
            ViewBag.Markalar = await _db.AracMarkalari.OrderBy(a => a.Ad).ToListAsync();
            ViewBag.Cinsler = await _db.AracCinsleri.OrderBy(a => a.Ad).ToListAsync();
            ViewBag.VergiDaireleri = await _db.VergiDaireleri.OrderBy(a => a.Sehir).ThenBy(a => a.Ad).ToListAsync();
            ViewBag.Otoparklar = await _db.Otoparklar.OrderBy(a => a.Ad).ToListAsync();
            ViewBag.Renkler = await _db.AracRenkleri.OrderBy(a => a.Ad).ToListAsync();
            ViewBag.ResmiTatiller = await _db.ResmiTatiller.OrderBy(t => t.Tarih).ToListAsync();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> KomisyonGuncelle(KomisyonAyari form)
        {
            var kayit = await _db.KomisyonAyarlari.FirstOrDefaultAsync();
            if (kayit == null) { kayit = form; _db.KomisyonAyarlari.Add(kayit); }
            else
            {
                kayit.Baskan = form.Baskan;
                kayit.Uye1 = form.Uye1;
                kayit.Uye2 = form.Uye2;
                kayit.Uye3 = form.Uye3;
                kayit.Uye4 = form.Uye4;
            }
            await _db.SaveChangesAsync();
            TempData["Basari"] = "Komisyon bilgileri güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> TarifeGuncelle(GenelAyar form)
        {
            var kayit = await _db.GenelAyarlar.FirstOrDefaultAsync();
            if (kayit == null) { kayit = form; _db.GenelAyarlar.Add(kayit); }
            else
            {
                kayit.GunlukOtoparkUcreti = form.GunlukOtoparkUcreti;
                kayit.KucukArac6AyaKadar = form.KucukArac6AyaKadar;
                kayit.KucukArac6AydanSonra = form.KucukArac6AydanSonra;
                kayit.MinibusKamyonet6AyaKadar = form.MinibusKamyonet6AyaKadar;
                kayit.MinibusKamyonet6AydanSonra = form.MinibusKamyonet6AydanSonra;
                kayit.BuyukArac6AyaKadar = form.BuyukArac6AyaKadar;
                kayit.BuyukArac6AydanSonra = form.BuyukArac6AydanSonra;
                kayit.Motor6AyaKadar = form.Motor6AyaKadar;
                kayit.Motor6AydanSonra = form.Motor6AydanSonra;
                kayit.CekiciUcretiSabit = form.CekiciUcretiSabit;
            }
            await _db.SaveChangesAsync();
            TempData["Basari"] = "Otopark ücret tarifesi güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        // ---- Basit liste ekle/sil işlemleri (marka, cins, vergi dairesi, otopark) ----
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkaEkle(string ad)
        {
            if (!string.IsNullOrWhiteSpace(ad)) { _db.AracMarkalari.Add(new AracMarkasi { Ad = ad.Trim() }); await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkaSil(int id)
        {
            var kayit = await _db.AracMarkalari.FindAsync(id);
            if (kayit != null) { _db.AracMarkalari.Remove(kayit); await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CinsEkle(string ad, OtoparkAracKategorisi otoparkKategorisi)
        {
            if (!string.IsNullOrWhiteSpace(ad))
            {
                _db.AracCinsleri.Add(new AracCinsi { Ad = ad.Trim(), OtoparkKategorisi = otoparkKategorisi });
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CinsSil(int id)
        {
            var kayit = await _db.AracCinsleri.FindAsync(id);
            if (kayit != null) { _db.AracCinsleri.Remove(kayit); await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

        // Bir araç cinsinin otopark ücret kategorisini günceller (ör. "Kamyon" için Büyük Araç).
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CinsKategoriGuncelle(int id, OtoparkAracKategorisi otoparkKategorisi)
        {
            var kayit = await _db.AracCinsleri.FindAsync(id);
            if (kayit != null)
            {
                kayit.OtoparkKategorisi = otoparkKategorisi;
                await _db.SaveChangesAsync();
                TempData["Basari"] = $"\"{kayit.Ad}\" için otopark kategorisi güncellendi.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> VergiDairesiEkle(string ad, string sehir)
        {
            if (!string.IsNullOrWhiteSpace(ad) && !string.IsNullOrWhiteSpace(sehir))
            {
                _db.VergiDaireleri.Add(new VergiDairesiKaydi { Ad = ad.Trim(), Sehir = sehir.Trim() });
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // Türkiye genelindeki hazır vergi dairesi listesini (eksik olanları) veritabanına yükler.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> VergiDaireleriTamListeyiYukle()
        {
            var mevcutlar = await _db.VergiDaireleri
                .Select(x => x.Sehir + "|" + x.Ad)
                .ToListAsync();
            var mevcutSet = mevcutlar.ToHashSet();

            var eksikler = TumTurkiyeVergiDaireleri.Liste
                .Where(x => !mevcutSet.Contains(x.Sehir + "|" + x.Ad))
                .Select(x => new VergiDairesiKaydi { Ad = x.Ad, Sehir = x.Sehir })
                .ToList();

            if (eksikler.Count > 0)
            {
                _db.VergiDaireleri.AddRange(eksikler);
                await _db.SaveChangesAsync();
                TempData["Basari"] = $"{eksikler.Count} yeni vergi dairesi eklendi.";
            }
            else
            {
                TempData["Basari"] = "Tüm liste zaten yüklü.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> VergiDairesiSil(int id)
        {
            var kayit = await _db.VergiDaireleri.FindAsync(id);
            if (kayit != null) { _db.VergiDaireleri.Remove(kayit); await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> OtoparkEkle(string ad)
        {
            if (!string.IsNullOrWhiteSpace(ad)) { _db.Otoparklar.Add(new OtoparkKaydi { Ad = ad.Trim() }); await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> OtoparkSil(int id)
        {
            var kayit = await _db.Otoparklar.FindAsync(id);
            if (kayit != null) { _db.Otoparklar.Remove(kayit); await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RenkEkle(string ad)
        {
            if (!string.IsNullOrWhiteSpace(ad)) { _db.AracRenkleri.Add(new AracRengi { Ad = ad.Trim() }); await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RenkSil(int id)
        {
            var kayit = await _db.AracRenkleri.FindAsync(id);
            if (kayit != null) { _db.AracRenkleri.Remove(kayit); await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResmiTatilEkle(DateTime tarih, string? aciklama)
        {
            if (!await _db.ResmiTatiller.AnyAsync(t => t.Tarih == tarih.Date))
            {
                _db.ResmiTatiller.Add(new ResmiTatil { Tarih = tarih.Date, Aciklama = aciklama });
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResmiTatilSil(int id)
        {
            var kayit = await _db.ResmiTatiller.FindAsync(id);
            if (kayit != null) { _db.ResmiTatiller.Remove(kayit); await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

        // ---- YEDEKLEME ----

        // Tüm verileri (dosyalar, satışlar, iptal/iade, referans listeleri) tek bir Excel
        // dosyasına (her tablo ayrı sayfa) aktararak indirir. Bu, veri güvenliği için
        // ekstra bir yedek olarak kullanılabilir.
        public async Task<IActionResult> ExcelYedekIndir()
        {
            var dosyalar = await _db.Dosyalar.AsNoTracking().ToListAsync();
            var satislar = await _db.Satislar.AsNoTracking().Include(s => s.Dosya).ToListAsync();
            var iptalIadeler = await _db.IptalIadeler.AsNoTracking().Include(i => i.Dosya).ToListAsync();

            using var wb = new ClosedXML.Excel.XLWorkbook();

            var wsDosya = wb.Worksheets.Add("Dosyalar");
            string[] dosyaBaslik = { "Dosya No", "Kayıt Tarihi", "Vergi Dairesi", "Alacaklı VD", "TCKN", "VKN", "Adı Soyadı", "Plaka", "Marka", "Cins", "Model Yılı", "Muhammen Bedel", "Teminat", "Damga Vergisi", "Durum" };
            for (int i = 0; i < dosyaBaslik.Length; i++) wsDosya.Cell(1, i + 1).Value = dosyaBaslik[i];
            int r = 2;
            foreach (var d in dosyalar)
            {
                wsDosya.Cell(r, 1).Value = d.DosyaNo;
                wsDosya.Cell(r, 2).Value = d.KayitTarihi;
                wsDosya.Cell(r, 3).Value = d.VergiDairesiAdi;
                wsDosya.Cell(r, 4).Value = d.AlacakliVergiDairesi;
                wsDosya.Cell(r, 5).Value = d.TCKimlikNumarasi;
                wsDosya.Cell(r, 6).Value = d.VergiKimlikNumarasi;
                wsDosya.Cell(r, 7).Value = d.AdiSoyadiUnvani;
                wsDosya.Cell(r, 8).Value = d.Plaka;
                wsDosya.Cell(r, 9).Value = d.AracMarkasi;
                wsDosya.Cell(r, 10).Value = d.AracCinsi;
                wsDosya.Cell(r, 11).Value = d.ModelYili;
                wsDosya.Cell(r, 12).Value = d.MuhammenBedel;
                wsDosya.Cell(r, 13).Value = d.Teminat;
                wsDosya.Cell(r, 14).Value = d.DamgaVergisiTutari;
                wsDosya.Cell(r, 15).Value = d.Durum.ToString();
                r++;
            }
            wsDosya.Columns().AdjustToContents();

            var wsSatis = wb.Worksheets.Add("Satislar");
            string[] satisBaslik = { "Dosya No", "Satış Türü", "Satış Tarihi", "Sonuç", "Satış Bedeli", "Alıcı", "Vade", "Otopark Ücreti" };
            for (int i = 0; i < satisBaslik.Length; i++) wsSatis.Cell(1, i + 1).Value = satisBaslik[i];
            r = 2;
            foreach (var s in satislar)
            {
                wsSatis.Cell(r, 1).Value = s.Dosya?.DosyaNo;
                wsSatis.Cell(r, 2).Value = s.SatisTuru.ToString();
                wsSatis.Cell(r, 3).Value = s.SatisTarihi;
                wsSatis.Cell(r, 4).Value = s.SatisSonucu.ToString();
                wsSatis.Cell(r, 5).Value = s.SatisBedeli;
                wsSatis.Cell(r, 6).Value = s.AliciAdiSoyadi;
                wsSatis.Cell(r, 7).Value = s.Vade;
                wsSatis.Cell(r, 8).Value = s.OtoparkUcretiToplam;
                r++;
            }
            wsSatis.Columns().AdjustToContents();

            var wsIptal = wb.Worksheets.Add("IptalIadeKapama");
            string[] iptalBaslik = { "Dosya No", "Satış İptali", "İptal Nedeni", "Satış İadesi", "İade Nedeni", "Dosya Kapatıldı" };
            for (int i = 0; i < iptalBaslik.Length; i++) wsIptal.Cell(1, i + 1).Value = iptalBaslik[i];
            r = 2;
            foreach (var x in iptalIadeler)
            {
                wsIptal.Cell(r, 1).Value = x.Dosya?.DosyaNo;
                wsIptal.Cell(r, 2).Value = x.SatisIptaliSecildi ? "Evet" : "Hayır";
                wsIptal.Cell(r, 3).Value = x.IptalNedeni;
                wsIptal.Cell(r, 4).Value = x.SatisIadesiSecildi ? "Evet" : "Hayır";
                wsIptal.Cell(r, 5).Value = x.IadeNedeni;
                wsIptal.Cell(r, 6).Value = x.DosyaKapatildi ? "Evet" : "Hayır";
                r++;
            }
            wsIptal.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;
            var dosyaAdi = $"AracSatisYedek_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", dosyaAdi);
        }

        // Ham SQLite veritabanı dosyasının (.db) doğrudan indirilmesi - fiziksel yedek olarak kullanılabilir.
        public IActionResult VeritabaniYedekIndir()
        {
            var dbYolu = Path.Combine(Directory.GetCurrentDirectory(), "AracSatis.db");
            if (!System.IO.File.Exists(dbYolu))
            {
                TempData["Hata"] = "Veritabanı dosyası bulunamadı.";
                return RedirectToAction(nameof(Index));
            }
            var bytes = System.IO.File.ReadAllBytes(dbYolu);
            var dosyaAdi = $"AracSatis_Yedek_{DateTime.Now:yyyyMMdd_HHmmss}.db";
            return File(bytes, "application/octet-stream", dosyaAdi);
        }

        // ---- ACCESS (.accdb) YEDEK OLUŞTURMA ----
        // NOT: Bu özellik yalnızca Windows'ta ve bilgisayarda "Microsoft Access Database Engine"
        // (ücretsiz Microsoft dağıtımı) veya Microsoft Access kurulu olduğunda çalışır, çünkü
        // .accdb formatı Microsoft'un ACE OLEDB sürücüsünü gerektirir. Kurulu değilse aşağıda
        // anlaşılır bir hata mesajı gösterilir ve Excel yedek alternatif olarak önerilir.
        public async Task<IActionResult> AccessYedekOlustur()
        {
            if (!OperatingSystem.IsWindows())
            {
                TempData["Hata"] = "Access (.accdb) dosyası oluşturma özelliği yalnızca Windows üzerinde çalışır. " +
                    "Bunun yerine 'Excel Yedek İndir' seçeneğini kullanıp indirilen dosyayı Access'te " +
                    "Dış Veri Al > Excel yoluyla içe aktarabilirsiniz.";
                return RedirectToAction(nameof(Index));
            }

            var klasor = Path.Combine(Directory.GetCurrentDirectory(), "Backups");
            Directory.CreateDirectory(klasor);
            var accdbYolu = Path.Combine(klasor, $"AracSatisAccessYedek_{DateTime.Now:yyyyMMdd_HHmmss}.accdb");

            try
            {
                AccdbOlustur(accdbYolu);
                var dosyalar = await _db.Dosyalar.AsNoTracking().ToListAsync();
                var satislar = await _db.Satislar.AsNoTracking().Include(s => s.Dosya).ToListAsync();
                var iptalIadeler = await _db.IptalIadeler.AsNoTracking().Include(i => i.Dosya).ToListAsync();
                AccdbTablolariniDoldur(accdbYolu, dosyalar, satislar, iptalIadeler);
            }
            catch (Exception ex)
            {
                TempData["Hata"] = "Access (.accdb) dosyası oluşturulamadı: " + ex.Message +
                    " Bilgisayarınızda 'Microsoft Access Database Engine 2016 Redistributable' " +
                    "(veya Microsoft Access) kurulu olduğundan emin olun. Alternatif olarak " +
                    "'Excel Yedek İndir' seçeneğini kullanabilirsiniz.";
                return RedirectToAction(nameof(Index));
            }

            var bytes = System.IO.File.ReadAllBytes(accdbYolu);
            return File(bytes, "application/msaccess", Path.GetFileName(accdbYolu));
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private static void AccdbOlustur(string yol)
        {
            // ADOX (Microsoft ADO Ext. for DDL and Security) COM bileşeni üzerinden,
            // derleme zamanında COM referansına ihtiyaç duymadan (late-binding) boş bir
            // .accdb veritabanı dosyası oluşturulur.
            var catalogType = Type.GetTypeFromProgID("ADOX.Catalog")
                ?? throw new InvalidOperationException("ADOX bileşeni bulunamadı.");
            dynamic catalog = Activator.CreateInstance(catalogType)!;
            try
            {
                string baglantiDizesi = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={yol};";
                catalog.Create(baglantiDizesi);
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(catalog);
            }
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private static void AccdbTablolariniDoldur(string yol, List<Dosya> dosyalar, List<Satis> satislar, List<IptalIade> iptalIadeler)
        {
            string baglantiDizesi = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={yol};";
            using var conn = new System.Data.OleDb.OleDbConnection(baglantiDizesi);
            conn.Open();

            void Calistir(string sql)
            {
                using var cmd = new System.Data.OleDb.OleDbCommand(sql, conn);
                cmd.ExecuteNonQuery();
            }

            Calistir(@"CREATE TABLE Dosyalar (
                DosyaNo TEXT(20), KayitTarihi DATETIME, VergiDairesi TEXT(150), AlacakliVD TEXT(150),
                TCKN TEXT(11), VKN TEXT(10), AdiSoyadi TEXT(150), Plaka TEXT(15), Marka TEXT(60),
                Cins TEXT(60), ModelYili INTEGER, MuhammenBedel CURRENCY, Teminat CURRENCY,
                DamgaVergisi CURRENCY, Durum TEXT(30))");

            Calistir(@"CREATE TABLE Satislar (
                DosyaNo TEXT(20), SatisTuru TEXT(30), SatisTarihi DATETIME, Sonuc TEXT(30),
                SatisBedeli CURRENCY, Alici TEXT(150), Vade DATETIME, OtoparkUcreti CURRENCY)");

            Calistir(@"CREATE TABLE IptalIadeKapama (
                DosyaNo TEXT(20), SatisIptali TEXT(10), IptalNedeni MEMO,
                SatisIadesi TEXT(10), IadeNedeni MEMO, DosyaKapatildi TEXT(10))");

            foreach (var d in dosyalar)
            {
                using var cmd = new System.Data.OleDb.OleDbCommand(
                    "INSERT INTO Dosyalar (DosyaNo, KayitTarihi, VergiDairesi, AlacakliVD, TCKN, VKN, AdiSoyadi, Plaka, Marka, Cins, ModelYili, MuhammenBedel, Teminat, DamgaVergisi, Durum) " +
                    "VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)", conn);
                cmd.Parameters.AddWithValue("?", d.DosyaNo);
                cmd.Parameters.AddWithValue("?", d.KayitTarihi);
                cmd.Parameters.AddWithValue("?", d.VergiDairesiAdi);
                cmd.Parameters.AddWithValue("?", d.AlacakliVergiDairesi);
                cmd.Parameters.AddWithValue("?", (object?)d.TCKimlikNumarasi ?? DBNull.Value);
                cmd.Parameters.AddWithValue("?", (object?)d.VergiKimlikNumarasi ?? DBNull.Value);
                cmd.Parameters.AddWithValue("?", d.AdiSoyadiUnvani);
                cmd.Parameters.AddWithValue("?", d.Plaka);
                cmd.Parameters.AddWithValue("?", d.AracMarkasi);
                cmd.Parameters.AddWithValue("?", d.AracCinsi);
                cmd.Parameters.AddWithValue("?", d.ModelYili);
                cmd.Parameters.AddWithValue("?", d.MuhammenBedel);
                cmd.Parameters.AddWithValue("?", d.Teminat);
                cmd.Parameters.AddWithValue("?", d.DamgaVergisiTutari);
                cmd.Parameters.AddWithValue("?", d.Durum.ToString());
                cmd.ExecuteNonQuery();
            }

            foreach (var s in satislar)
            {
                using var cmd = new System.Data.OleDb.OleDbCommand(
                    "INSERT INTO Satislar (DosyaNo, SatisTuru, SatisTarihi, Sonuc, SatisBedeli, Alici, Vade, OtoparkUcreti) " +
                    "VALUES (?,?,?,?,?,?,?,?)", conn);
                cmd.Parameters.AddWithValue("?", s.Dosya?.DosyaNo ?? "");
                cmd.Parameters.AddWithValue("?", s.SatisTuru.ToString());
                cmd.Parameters.AddWithValue("?", s.SatisTarihi);
                cmd.Parameters.AddWithValue("?", s.SatisSonucu.ToString());
                cmd.Parameters.AddWithValue("?", (object?)s.SatisBedeli ?? DBNull.Value);
                cmd.Parameters.AddWithValue("?", (object?)s.AliciAdiSoyadi ?? "");
                cmd.Parameters.AddWithValue("?", s.Vade);
                cmd.Parameters.AddWithValue("?", s.OtoparkUcretiToplam);
                cmd.ExecuteNonQuery();
            }

            foreach (var x in iptalIadeler)
            {
                using var cmd = new System.Data.OleDb.OleDbCommand(
                    "INSERT INTO IptalIadeKapama (DosyaNo, SatisIptali, IptalNedeni, SatisIadesi, IadeNedeni, DosyaKapatildi) " +
                    "VALUES (?,?,?,?,?,?)", conn);
                cmd.Parameters.AddWithValue("?", x.Dosya?.DosyaNo ?? "");
                cmd.Parameters.AddWithValue("?", x.SatisIptaliSecildi ? "Evet" : "Hayır");
                cmd.Parameters.AddWithValue("?", (object?)x.IptalNedeni ?? "");
                cmd.Parameters.AddWithValue("?", x.SatisIadesiSecildi ? "Evet" : "Hayır");
                cmd.Parameters.AddWithValue("?", (object?)x.IadeNedeni ?? "");
                cmd.Parameters.AddWithValue("?", x.DosyaKapatildi ? "Evet" : "Hayır");
                cmd.ExecuteNonQuery();
            }
        }
    }
}
