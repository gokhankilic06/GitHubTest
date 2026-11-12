namespace AracSatisSistemiWinForms.Modeller
{
    // Sunucudaki (AracSatisSistemi/Api/*) JSON API ile birebir uyumlu veri şekilleri.
    // JsonSerializerDefaults.Web ile büyük/küçük harf duyarsız eşleşme kullanıldığından
    // (PropertyNameCaseInsensitive = true), buradaki PascalCase adlar sunucunun camelCase
    // JSON alanlarıyla otomatik eşleşir.

    public class GirisYaniti
    {
        public string Token { get; set; } = "";
        public DateTime GecerlilikSonu { get; set; }
        public string KullaniciAdi { get; set; } = "";
        public string AdSoyad { get; set; } = "";
    }

    public class ReferansOge
    {
        public int Id { get; set; }
        public string Ad { get; set; } = "";
        public override string ToString() => Ad;
    }

    public class VergiDairesiOge
    {
        public int Id { get; set; }
        public string Ad { get; set; } = "";
        public string Sehir { get; set; } = "";
        public override string ToString() => Ad;
    }

    public class KayitFormuVerisi
    {
        public List<ReferansOge> Cinsler { get; set; } = new();
        public List<ReferansOge> Markalar { get; set; } = new();
        public List<ReferansOge> Otoparklar { get; set; } = new();
        public List<ReferansOge> Renkler { get; set; } = new();
        public int MaksimumModelYili { get; set; }
        public List<VergiDairesiOge> AnkaraVergiDaireleri { get; set; } = new();
        public List<VergiDairesiOge> VergiDaireleri { get; set; } = new();
        public List<string> Sehirler { get; set; } = new();
        public Dictionary<string, List<string>> ModelSozlugu { get; set; } = new();
    }

    public class YeniDosyaOnerileri
    {
        public DateTime Satis1Onerisi { get; set; }
        public DateTime Satis2Onerisi { get; set; }
        public string? KomisyonBaskani { get; set; }
        public string? KomisyonUye1 { get; set; }
        public string? KomisyonUye2 { get; set; }
        public string? KomisyonUye3 { get; set; }
    }

    public class DosyaOzeti
    {
        public int Id { get; set; }
        public string DosyaNo { get; set; } = "";
        public DateTime KayitTarihi { get; set; }
        public string Plaka { get; set; } = "";
        public string AdiSoyadiUnvani { get; set; } = "";
        public string VergiDairesiAdi { get; set; } = "";
        public string AracMarkasi { get; set; } = "";
        public string AracCinsi { get; set; } = "";
        public string Durum { get; set; } = "";
        public string DurumGorunenAd { get; set; } = "";
        public string GenelDurumGorunen { get; set; } = "";
        public string AsamaGorunen { get; set; } = "";
        public string AsamaRozetSinifi { get; set; } = "";
        public bool Aktif { get; set; }
    }

    public class SatisKaydi
    {
        public int Id { get; set; }
        public string SatisTuru { get; set; } = "";
        public string SatisTuruGorunen { get; set; } = "";
        public DateTime SatisTarihi { get; set; }
        public string SatisSonucu { get; set; } = "";
        public string SonucGorunen { get; set; } = "";
        public decimal? SatisBedeli { get; set; }
        public string? AliciAdiSoyadi { get; set; }
    }

    public class Dosya
    {
        public int Id { get; set; }
        public string DosyaNo { get; set; } = "";
        public DateTime KayitTarihi { get; set; }
        public string VergiDairesiAdi { get; set; } = "";
        public DateTime? VergiDairesiYaziTarihi { get; set; }
        public string? VergiDairesiYaziSayisi { get; set; }
        public string AlacakliVergiDairesi { get; set; } = "";
        public string? VergiKimlikNumarasi { get; set; }
        public string? TcKimlikNumarasi { get; set; }
        public string AdiSoyadiUnvani { get; set; } = "";
        public string? KanuniTemsilci { get; set; }
        public string Plaka { get; set; } = "";
        public string AracCinsi { get; set; } = "";
        public string AracMarkasi { get; set; } = "";
        public string? AracTipi { get; set; }
        public int ModelYili { get; set; }
        public string? Renk { get; set; }
        public bool RuhsatVar { get; set; }
        public bool AnahtarVar { get; set; }
        public bool CekiciVar { get; set; }
        public string? OtoparkAdi { get; set; }
        public string? HasarDurumu { get; set; }
        public decimal MuhammenBedel { get; set; }
        public int KdvOrani { get; set; }
        public DateTime? OtoparkaGirisTarihi { get; set; }
        public string? KomisyonBaskani { get; set; }
        public string? KomisyonUye1 { get; set; }
        public string? KomisyonUye2 { get; set; }
        public string? KomisyonUye3 { get; set; }
        public string Durum { get; set; } = "";
        public string DurumGorunenAd { get; set; } = "";
        public string AsamaGorunen { get; set; } = "";
        public string AsamaRozetSinifi { get; set; } = "";
        public DateTime Satis1Tarihi { get; set; }
        public DateTime Satis2Tarihi { get; set; }
        public decimal Teminat { get; set; }
        public decimal DamgaVergisiOrani { get; set; }
        public decimal DamgaVergisiTutari { get; set; }
        public List<SatisKaydi> Satislar { get; set; } = new();
    }

    // Kayıt İşlemleri formundan sunucuya gönderilen veri.
    public class DosyaFormVerisi
    {
        public string VergiDairesiAdi { get; set; } = "";
        public DateTime? VergiDairesiYaziTarihi { get; set; }
        public string? VergiDairesiYaziSayisi { get; set; }
        public string AlacakliVergiDairesi { get; set; } = "";
        public string? VergiKimlikNumarasi { get; set; }
        public string? TcKimlikNumarasi { get; set; }
        public string AdiSoyadiUnvani { get; set; } = "";
        public string? KanuniTemsilci { get; set; }
        public string Plaka { get; set; } = "";
        public string AracCinsi { get; set; } = "";
        public string AracMarkasi { get; set; } = "";
        public string? AracTipi { get; set; }
        public int ModelYili { get; set; }
        public string? Renk { get; set; }
        public bool RuhsatVar { get; set; }
        public bool AnahtarVar { get; set; }
        public bool CekiciVar { get; set; }
        public string? OtoparkAdi { get; set; }
        public string? HasarDurumu { get; set; }
        public decimal MuhammenBedel { get; set; }
        public int KdvOrani { get; set; } = 1;
        public decimal DamgaVergisiOrani { get; set; } = 5.69m;
        public DateTime? OtoparkaGirisTarihi { get; set; }
        public string? KomisyonBaskani { get; set; }
        public string? KomisyonUye1 { get; set; }
        public string? KomisyonUye2 { get; set; }
        public string? KomisyonUye3 { get; set; }
        public DateTime Satis1Tarihi { get; set; }
        public DateTime Satis2Tarihi { get; set; }
    }
}
