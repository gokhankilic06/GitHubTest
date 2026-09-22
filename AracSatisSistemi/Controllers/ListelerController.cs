using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AracSatisSistemi.Data;
using AracSatisSistemi.Models;
using ClosedXML.Excel;

namespace AracSatisSistemi.Controllers
{
    public class ListelerController : Controller
    {
        private readonly AppDbContext _db;
        public ListelerController(AppDbContext db) => _db = db;

        // Araç Durum Listesi + Satış Sonuç Listesi
        // "Satış türü ve/veya tarih seçilerek liste alınabilmeli" gereksinimi.
        public async Task<IActionResult> Index(SatisTuru? satisTuru, DateTime? baslangic, DateTime? bitis)
        {
            ViewBag.SatisTuru = satisTuru;
            ViewBag.Baslangic = baslangic?.ToString("yyyy-MM-dd");
            ViewBag.Bitis = bitis?.ToString("yyyy-MM-dd");

            // Araç Durum Listesi: tüm dosyalar (durumuna göre)
            var dosyaSorgu = _db.Dosyalar.AsQueryable();
            if (baslangic.HasValue) dosyaSorgu = dosyaSorgu.Where(d => d.KayitTarihi >= baslangic.Value);
            if (bitis.HasValue) dosyaSorgu = dosyaSorgu.Where(d => d.KayitTarihi <= bitis.Value);
            ViewBag.AracDurumListesi = await dosyaSorgu.OrderByDescending(d => d.KayitTarihi).ToListAsync();

            // Satış Sonuç Listesi: satış türü / tarih aralığına göre filtrelenmiş satışlar
            var satisSorgu = _db.Satislar.Include(s => s.Dosya).AsQueryable();
            if (satisTuru.HasValue) satisSorgu = satisSorgu.Where(s => s.SatisTuru == satisTuru.Value);
            if (baslangic.HasValue) satisSorgu = satisSorgu.Where(s => s.SatisTarihi >= baslangic.Value);
            if (bitis.HasValue) satisSorgu = satisSorgu.Where(s => s.SatisTarihi <= bitis.Value);
            ViewBag.SatisSonucListesi = await satisSorgu.OrderByDescending(s => s.SatisTarihi).ToListAsync();

            return View();
        }

        // Filtrelenmiş listeleri (Araç Durum Listesi + Satış Sonuç Listesi) Excel olarak indirir.
        public async Task<IActionResult> ExcelIndir(SatisTuru? satisTuru, DateTime? baslangic, DateTime? bitis)
        {
            var dosyaSorgu = _db.Dosyalar.AsQueryable();
            if (baslangic.HasValue) dosyaSorgu = dosyaSorgu.Where(d => d.KayitTarihi >= baslangic.Value);
            if (bitis.HasValue) dosyaSorgu = dosyaSorgu.Where(d => d.KayitTarihi <= bitis.Value);
            var aracListesi = await dosyaSorgu.OrderByDescending(d => d.KayitTarihi).ToListAsync();

            var satisSorgu = _db.Satislar.Include(s => s.Dosya).AsQueryable();
            if (satisTuru.HasValue) satisSorgu = satisSorgu.Where(s => s.SatisTuru == satisTuru.Value);
            if (baslangic.HasValue) satisSorgu = satisSorgu.Where(s => s.SatisTarihi >= baslangic.Value);
            if (bitis.HasValue) satisSorgu = satisSorgu.Where(s => s.SatisTarihi <= bitis.Value);
            var satisListesi = await satisSorgu.OrderByDescending(s => s.SatisTarihi).ToListAsync();

            using var wb = new XLWorkbook();

            var ws1 = wb.Worksheets.Add("Arac Durum Listesi");
            string[] b1 = { "Dosya No", "Plaka", "Marka", "Cins", "Adı Soyadı", "Durum", "Muhammen Bedel" };
            for (int i = 0; i < b1.Length; i++) ws1.Cell(1, i + 1).Value = b1[i];
            int r = 2;
            foreach (var d in aracListesi)
            {
                ws1.Cell(r, 1).Value = d.DosyaNo;
                ws1.Cell(r, 2).Value = d.Plaka;
                ws1.Cell(r, 3).Value = d.AracMarkasi;
                ws1.Cell(r, 4).Value = d.AracCinsi;
                ws1.Cell(r, 5).Value = d.AdiSoyadiUnvani;
                ws1.Cell(r, 6).Value = d.Durum.ToString();
                ws1.Cell(r, 7).Value = d.MuhammenBedel;
                r++;
            }
            ws1.Columns().AdjustToContents();

            var ws2 = wb.Worksheets.Add("Satis Sonuc Listesi");
            string[] b2 = { "Dosya No", "Satış Türü", "Satış Tarihi", "Sonuç", "Satış Bedeli", "Alıcı" };
            for (int i = 0; i < b2.Length; i++) ws2.Cell(1, i + 1).Value = b2[i];
            r = 2;
            foreach (var s in satisListesi)
            {
                ws2.Cell(r, 1).Value = s.Dosya?.DosyaNo;
                ws2.Cell(r, 2).Value = s.SatisTuru.ToString();
                ws2.Cell(r, 3).Value = s.SatisTarihi;
                ws2.Cell(r, 4).Value = s.SatisSonucu.ToString();
                ws2.Cell(r, 5).Value = s.SatisBedeli;
                ws2.Cell(r, 6).Value = s.AliciAdiSoyadi;
                r++;
            }
            ws2.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Listeler_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }
    }
}
