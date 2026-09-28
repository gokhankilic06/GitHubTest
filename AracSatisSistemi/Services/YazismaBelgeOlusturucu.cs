using System.Globalization;
using AracSatisSistemi.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace AracSatisSistemi.Services
{
    // Yazışma ekranındaki iki belge üretim butonu için Word (.docx) çıktısı oluşturur:
    // 1) "[Vergi Dairesi Adı]" butonu -> doğrudan mükellefin bağlı olduğu vergi dairesine yazı,
    // 2) "Üst Yazı" butonu -> Gelir İdaresi Başkanlığı antetli, alacaklı vergi dairesine yazı.
    // İkisi de kullanıcının paylaştığı örnek belge görüntülerinden birebir uyarlanmıştır.
    public static class YazismaBelgeOlusturucu
    {
        private static readonly CultureInfo TrTR = new("tr-TR");

        public static byte[] VergiDairesiYazisiOlustur(Dosya dosya, Satis? satis, decimal kdvTutari, decimal dvTutari, decimal toplamTutar, decimal otoparkTutari)
        {
            var acilisParagraflari = new List<Paragraph>
            {
                Baslik(HitapSekli(dosya.VergiDairesiAdi)),
                Baslik("ANKARA"),
                Bos(),
                Par($"İlgi : {IlgiYaziSayisi(dosya)} sayılı yazınız.", JustificationValues.Left),
                Bos(),
                Par(AcilisCumlesiDogrudan(dosya, satis, kdvTutari, dvTutari, toplamTutar), ilkSatirGirinti: 567)
            };

            return BelgeOlustur(acilisParagraflari, GovdeParagraflari(dosya, satis, kdvTutari, dvTutari, otoparkTutari));
        }

        public static byte[] UstYaziOlustur(Dosya dosya, Satis? satis, decimal kdvTutari, decimal dvTutari, decimal toplamTutar, decimal otoparkTutari)
        {
            var acilisParagraflari = new List<Paragraph>
            {
                Baslik("T.C."),
                Baslik("GELİR İDARESİ BAŞKANLIĞI"),
                Baslik("Ankara"),
                Bos(),
                Par($"Sayı : {dosya.DosyaNo}", JustificationValues.Left),
                Par("Konu : Araç Satışı", JustificationValues.Left),
                Bos(),
                Baslik(HitapSekli(dosya.AlacakliVergiDairesi)),
                Baslik("ANKARA"),
                Bos(),
                Par($"İlgi : {IlgiYaziSayisi(dosya)} sayılı yazınız.", JustificationValues.Left),
                Bos(),
                Par(AcilisCumlesiUstYazi(dosya, satis, kdvTutari, dvTutari, toplamTutar), ilkSatirGirinti: 567)
            };

            return BelgeOlustur(acilisParagraflari, GovdeParagraflari(dosya, satis, kdvTutari, dvTutari, otoparkTutari));
        }

        private static string IlgiYaziSayisi(Dosya dosya) =>
            string.IsNullOrWhiteSpace(dosya.VergiDairesiYaziSayisi) ? "……………………………" : dosya.VergiDairesiYaziSayisi;

        // "X Vergi Dairesi Müdürlüğü" adını resmi yazı hitabına çevirir: "X VERGİ DAİRESİ MÜDÜRLÜĞÜNE".
        // Vergi dairesi listesi adı zaten "... Müdürlüğü" ile bittiğinden ek olarak bu ibare
        // tekrar eklenmez, yalnızca "-ne" hâl eki uygulanır.
        private static string HitapSekli(string vergiDairesiAdi)
        {
            if (string.IsNullOrWhiteSpace(vergiDairesiAdi)) return "……………………………… VERGİ DAİRESİ MÜDÜRLÜĞÜNE";
            var ad = vergiDairesiAdi.Trim();
            var hitap = ad.EndsWith("Müdürlüğü", StringComparison.OrdinalIgnoreCase)
                ? ad[..^"Müdürlüğü".Length] + "Müdürlüğüne"
                : ad + " Müdürlüğüne";
            return hitap.ToUpper(TrTR);
        }

        private static (string borcluNo, string borcluNoTuru) BorcluNo(Dosya dosya)
        {
            if (!string.IsNullOrWhiteSpace(dosya.TCKimlikNumarasi)) return (dosya.TCKimlikNumarasi!, "T.C. kimlik");
            if (!string.IsNullOrWhiteSpace(dosya.VergiKimlikNumarasi)) return (dosya.VergiKimlikNumarasi!, "vergi kimlik");
            return ("……………", "vergi kimlik");
        }

        private static string AliciBilgisi(Satis? satis)
        {
            var no = satis?.AliciVKN ?? satis?.AliciTCKN ?? "……………";
            var ad = satis?.AliciAdiSoyadi ?? "……………";
            var vekilEki = !string.IsNullOrWhiteSpace(satis?.AliciVekili) ? $" (Vek: {satis!.AliciVekili})" : "";
            return $"{no} vergi kimlik numaralı {ad}{vekilEki}";
        }

        private static string DvOraniYuzde(Dosya dosya) => (dosya.DamgaVergisiOrani / 10m).ToString("0.00", TrTR);

        private static string AcilisCumlesiDogrudan(Dosya dosya, Satis? satis, decimal kdvTutari, decimal dvTutari, decimal toplamTutar)
        {
            var (borcluNo, borcluNoTuru) = BorcluNo(dosya);
            var satisBedeli = satis?.SatisBedeli ?? 0m;
            var satisTarihi = (satis?.SatisTarihi ?? dosya.KayitTarihi).ToString("dd.MM.yyyy", TrTR);
            return $"Dairenizin {borcluNo} {borcluNoTuru} numarasında kayıtlı mükellefi {dosya.AdiSoyadiUnvani}'ya ait {dosya.Plaka} plakalı {dosya.ModelYili} model {dosya.AracMarkasi} marka araç {satisTarihi} tarihinde {AliciBilgisi(satis)}'ye {satisBedeli.ToString("N2", TrTR)} TL + (%{dosya.KdvOrani.ToString("0.00", TrTR)}) {kdvTutari.ToString("N2", TrTR)} TL KDV + (%{DvOraniYuzde(dosya)}) {dvTutari.ToString("N2", TrTR)} TL Damga Vergisi olmak üzere toplam {toplamTutar.ToString("N2", TrTR)} TL ({SayiYaziCevirici.TutarYaziyla(toplamTutar)}) bedelle satılmıştır.";
        }

        private static string AcilisCumlesiUstYazi(Dosya dosya, Satis? satis, decimal kdvTutari, decimal dvTutari, decimal toplamTutar)
        {
            var (borcluNo, _) = BorcluNo(dosya);
            var satisBedeli = satis?.SatisBedeli ?? 0m;
            var satisTarihi = (satis?.SatisTarihi ?? dosya.KayitTarihi).ToString("dd.MM.yyyy", TrTR);
            return $"{dosya.VergiDairesiAdi}'nün {borcluNo} vergi kimlik numarasında kayıtlı {dosya.AdiSoyadiUnvani}'a ait {dosya.Plaka} plakalı {dosya.ModelYili} model {dosya.AracMarkasi} marka araç {satisTarihi} tarihinde {AliciBilgisi(satis)}'ya {satisBedeli.ToString("N2", TrTR)} TL + (%{dosya.KdvOrani.ToString("0.00", TrTR)}) {kdvTutari.ToString("N2", TrTR)} TL KDV + (%{DvOraniYuzde(dosya)}) {dvTutari.ToString("N2", TrTR)} TL Damga Vergisi olmak üzere toplam {toplamTutar.ToString("N2", TrTR)} TL ({SayiYaziCevirici.TutarYaziyla(toplamTutar)}) bedelle satılmıştır.";
        }

        private static List<Paragraph> GovdeParagraflari(Dosya dosya, Satis? satis, decimal kdvTutari, decimal dvTutari, decimal otoparkTutari)
        {
            var satisBedeli = satis?.SatisBedeli ?? 0m;
            var teminat = dosya.Teminat;
            var kalanSonTutar = teminat > 0 ? satisBedeli - teminat : satisBedeli;
            var tahsilTarihi = satis?.VerilenSure ?? (satis?.SatisTarihi ?? dosya.KayitTarihi).AddDays(3);
            var cekiciBedeli = satis?.CekiciBedeli ?? 0m;

            string teminatParagrafi = teminat > 0
                ? $"Araç satış bedeli olan {satisBedeli.ToString("N2", TrTR)} TL'den söz konusu araç için teminat olarak yatırılan {teminat.ToString("N2", TrTR)} TL tutarın mahsubundan sonra kalan {kalanSonTutar.ToString("N2", TrTR)} TL tutar ile birlikte {kdvTutari.ToString("N2", TrTR)} TL KDV ile {dvTutari.ToString("N2", TrTR)} TL Damga Vergisinin {tahsilTarihi:dd.MM.yyyy} tarihi mesai bitimine kadar tahsil edilerek;"
                : $"Araç satış bedeli olan {satisBedeli.ToString("N2", TrTR)} TL tutar ile birlikte {kdvTutari.ToString("N2", TrTR)} TL KDV ile {dvTutari.ToString("N2", TrTR)} TL Damga Vergisinin {tahsilTarihi:dd.MM.yyyy} tarihi mesai bitimine kadar tahsil edilerek;";

            return new List<Paragraph>
            {
                Bos(),
                Par(teminatParagrafi, ilkSatirGirinti: 567),
                Bos(),
                Par($"- Satış bedelinden öncelikle aracın otopark ücreti {otoparkTutari.ToString("N2", TrTR)} TL ve {cekiciBedeli.ToString("N2", TrTR)} TL çekici ücretinin ilgili otoparktan faturası alınmak suretiyle ödemesinin yapılması,"),
                Par("- Aracın motorlu taşıtlar vergisi borçlarının mahsubunun yapılmasına müteakip,"),
                Par("- Aracın alıcısı adına tescili için ilgili noterliğe gerekli yazışmaların yapılması,"),
                Par("- Aracın bulunduğu otoparktan çıkarılması için ilgili otoparka yazı yazılması,"),
                Par("- Aracın otoparkta kaldığı süre ile ilgili olarak TÜVTÜRK'e gerekli yazışmaların yapılması ve yazıların birer suretinin alıcıya verilmesi gerekmektedir."),
                Bos(),
                Par("Gereğinin yapılarak sonucundan Müdürlüğümüze bilgi verilmesini arz ederim.", ilkSatirGirinti: 567)
            };
        }

        private static byte[] BelgeOlustur(List<Paragraph> acilisParagraflari, List<Paragraph> govdeParagraflari)
        {
            using var ms = new MemoryStream();
            using (var belge = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
            {
                var anaBolum = belge.AddMainDocumentPart();
                anaBolum.Document = new Document();
                var govde = anaBolum.Document.AppendChild(new Body());

                foreach (var p in acilisParagraflari) govde.AppendChild(p);
                foreach (var p in govdeParagraflari) govde.AppendChild(p);

                govde.AppendChild(new SectionProperties(
                    new PageSize { Width = 11906, Height = 16838 },
                    new PageMargin { Top = 1417, Bottom = 1417, Left = 1417, Right = 1134, Header = 708, Footer = 708 }));

                anaBolum.Document.Save();
            }
            return ms.ToArray();
        }

        private static Paragraph Bos() => Par(string.Empty);

        private static Paragraph Baslik(string metin) => Par(metin, JustificationValues.Center, kalin: true);

        private static Paragraph Par(string metin, JustificationValues? hiza = null, bool kalin = false, int? ilkSatirGirinti = null)
        {
            var pPr = new ParagraphProperties(
                new Justification { Val = hiza ?? JustificationValues.Both },
                new SpacingBetweenLines { After = "160", Line = "300", LineRule = LineSpacingRuleValues.Auto });
            if (ilkSatirGirinti.HasValue)
            {
                pPr.Append(new Indentation { FirstLine = ilkSatirGirinti.Value.ToString() });
            }

            var rPr = new RunProperties(new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" }, new FontSize { Val = "24" });
            if (kalin) rPr.Append(new Bold());

            var run = new Run(rPr, new Text(metin) { Space = SpaceProcessingModeValues.Preserve });
            return new Paragraph(pPr, run);
        }
    }
}
