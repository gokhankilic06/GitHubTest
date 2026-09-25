using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AracSatisSistemi.Models
{
    // KAYIT İŞLEMLERİ ekranına karşılık gelen ana entity.
    public class Dosya
    {
        public int Id { get; set; }

        // Sistem tarafından otomatik üretilir (ör: 2026/000123)
        [Display(Name = "Dosya No")]
        public string DosyaNo { get; set; } = string.Empty;

        public DateTime KayitTarihi { get; set; } = DateTime.Now;

        // ---- İLGİLİ VERGİ DAİRESİ ----
        [Required, Display(Name = "Vergi Dairesi Adı")]
        public string VergiDairesiAdi { get; set; } = string.Empty;

        [Display(Name = "Vergi Dairesi Yazı Tarihi")]
        [DataType(DataType.Date)]
        public DateTime? VergiDairesiYaziTarihi { get; set; }

        [Display(Name = "Vergi Dairesi Yazı Sayısı")]
        public string? VergiDairesiYaziSayisi { get; set; }

        [Required, Display(Name = "Alacaklı Vergi Dairesi")]
        public string AlacakliVergiDairesi { get; set; } = string.Empty;

        // ---- BORÇLU MÜKELLEF BİLGİLERİ ----
        [Display(Name = "Vergi Kimlik Numarası")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "Vergi Kimlik Numarası 10 haneli olmalıdır.")]
        public string? VergiKimlikNumarasi { get; set; }

        [Display(Name = "T.C. Kimlik Numarası")]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "T.C. Kimlik Numarası 11 haneli olmalıdır.")]
        public string? TCKimlikNumarasi { get; set; }

        [Required, Display(Name = "Adı Soyadı / Unvanı")]
        public string AdiSoyadiUnvani { get; set; } = string.Empty;

        [Display(Name = "Kanuni Temsilci / Ortak vb.")]
        public string? KanuniTemsilci { get; set; }

        // ---- ARAÇ BİLGİ GİRİŞİ ----
        [Required, Display(Name = "Plaka")]
        public string Plaka { get; set; } = string.Empty;

        [Required, Display(Name = "Cinsi")]
        public string AracCinsi { get; set; } = string.Empty;

        [Required, Display(Name = "Markası")]
        public string AracMarkasi { get; set; } = string.Empty;

        [Display(Name = "Tipi")]
        public string? AracTipi { get; set; }

        [Display(Name = "Model Yılı")]
        public int ModelYili { get; set; }

        [Display(Name = "Renk")]
        public string? Renk { get; set; }

        [Display(Name = "Ruhsat Var mı?")]
        public bool RuhsatVar { get; set; }

        [Display(Name = "Anahtar Var mı?")]
        public bool AnahtarVar { get; set; }

        [Display(Name = "Çekici Var mı?")]
        public bool CekiciVar { get; set; }

        [Display(Name = "Aracın Bulunduğu Otopark")]
        public string? OtoparkAdi { get; set; }

        [Display(Name = "Hasar Durumu")]
        public string? HasarDurumu { get; set; }

        [Display(Name = "Muhammen Bedeli (TL)")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MuhammenBedel { get; set; }

        // %1 - %10 - %20 seçenekleri
        [Display(Name = "KDV Oranı (%)")]
        public int KdvOrani { get; set; } = 1;

        [Display(Name = "Otoparka Giriş Tarihi")]
        [DataType(DataType.Date)]
        public DateTime? OtoparkaGirisTarihi { get; set; }

        // ---- KOMİSYON BİLGİLERİ (kayıt anındaki anlık görüntü - snapshot) ----
        [Display(Name = "Komisyon Başkanı")]
        public string? KomisyonBaskani { get; set; }
        [Display(Name = "Komisyon Üyesi 1")]
        public string? KomisyonUye1 { get; set; }
        [Display(Name = "Komisyon Üyesi 2")]
        public string? KomisyonUye2 { get; set; }
        [Display(Name = "Komisyon Üyesi 3")]
        public string? KomisyonUye3 { get; set; }
        [Display(Name = "Komisyon Üyesi 4")]
        public string? KomisyonUye4 { get; set; }

        public DosyaDurumu Durum { get; set; } = DosyaDurumu.Kayitli;

        // ---- SATIŞ BİLGİLERİ (Kayıt anında otomatik hesaplanan planlanan ihale tarihleri) ----
        // İhaleler her hafta Çarşamba günü yapılır. 1. Satış = kayıttan sonraki ilk Çarşamba,
        // 2. Satış = 1. Satıştan bir sonraki Çarşamba. Resmi tatile denk gelirse bir hafta ertelenir.
        // Sonuç/bedel/alıcı bilgileri ihale gerçekleştikten sonra "Satışa İlişkin Bilgiler"
        // ekranından (Satis entity'si olarak) ayrıca girilir.
        [Display(Name = "1. Satış Tarihi")]
        [DataType(DataType.Date)]
        public DateTime Satis1Tarihi { get; set; }

        [Display(Name = "2. Satış Tarihi")]
        [DataType(DataType.Date)]
        public DateTime Satis2Tarihi { get; set; }

        // Navigasyon
        public List<Satis> Satislar { get; set; } = new();
        public IptalIade? IptalIade { get; set; }

        // ---- HESAPLANAN (VERİTABANINDA TUTULMAYAN) ALANLAR ----
        // Muhammen bedel 10.000 TL üzerindeyse %5 teminat hesaplanır.
        [NotMapped]
        [Display(Name = "Teminat (TL)")]
        public decimal Teminat => MuhammenBedel > 10000m ? Math.Round(MuhammenBedel * 0.05m, 2) : 0m;

        // Damga vergisi oranı sabit: binde 5,69
        [NotMapped]
        public decimal DamgaVergisiOrani => 5.69m;

        [NotMapped]
        [Display(Name = "Damga Vergisi Tutarı (TL)")]
        public decimal DamgaVergisiTutari => Math.Round(MuhammenBedel * DamgaVergisiOrani / 1000m, 2);

        // Aktif bir dosyanın süreçte tam olarak hangi aşamada olduğunu gösterir (ör. 2. Satışta
        // "Alıcı Çıkmadı" sonucu girilince otomatik olarak "Pazarlık Bekliyor" olur). Satislar
        // navigasyon koleksiyonunun yüklenmiş (Include) olması gerekir.
        [NotMapped]
        public string AsamaGorunen
        {
            get
            {
                if (Durum == DosyaDurumu.Satildi) return "Satıldı";
                if (Durum == DosyaDurumu.IptalEdildi) return "İptal Edildi";
                if (Durum == DosyaDurumu.IadeEdildi) return "İade Edildi";
                if (Durum == DosyaDurumu.Kapandi) return "Kapandı";

                var siralama = new[] { SatisTuru.BirinciSatis, SatisTuru.IkinciSatis, SatisTuru.Pazarlik, SatisTuru.Madde6183_86 };
                foreach (var tur in siralama)
                {
                    var mevcut = Satislar.FirstOrDefault(s => s.SatisTuru == tur);
                    if (mevcut == null)
                    {
                        return tur switch
                        {
                            SatisTuru.BirinciSatis => "1. Satış Bekliyor",
                            SatisTuru.IkinciSatis => "2. Satış Bekliyor",
                            SatisTuru.Pazarlik => "Pazarlık Bekliyor",
                            SatisTuru.Madde6183_86 => "6183/86 Bekliyor",
                            _ => tur.ToString()
                        };
                    }
                }
                return "Süreç Tamamlandı (Satılamadı)";
            }
        }

        // Ekranda / yazdırmada gösterilen Türkçe durum adı (ör. "Kayitli" -> "Aktif").
        [NotMapped]
        public string DurumGorunenAd => Durum switch
        {
            DosyaDurumu.Kayitli => "Aktif",
            DosyaDurumu.SatisaCikti => "Satışta",
            DosyaDurumu.Satildi => "Satıldı",
            DosyaDurumu.IptalEdildi => "İptal Edildi",
            DosyaDurumu.IadeEdildi => "İade Edildi",
            DosyaDurumu.Kapandi => "Kapandı",
            _ => Durum.ToString()
        };

        // Genel liste ayrıştırması için: dosya süreci devam ediyorsa (kayıtlı / satışa çıktı)
        // AKTİF, süreç bir şekilde sonuçlanmışsa (satıldı / iptal / iade / kapandı) PASİF sayılır.
        // Satış sonucu "Satıldı" girilip kaydedildiği anda Durum=Satildi olur ve dosya PASİF'e düşer.
        [NotMapped]
        public bool Aktif => Durum == DosyaDurumu.Kayitli || Durum == DosyaDurumu.SatisaCikti;

        [NotMapped]
        public string GenelDurumGorunen => Aktif ? "Aktif" : "Pasif";
    }
}
