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

        // Satış sürecinin dört sabit aşaması + "Alıcıya Terk" (6183 sayılı Kanun Md. 85/86)
        // terminal sonucu. Hangi aşamanın bekleniyor olduğu Satislar geçmişinden (Asama
        // özelliği) hesaplanır; SatisController ve IptalIadeController bu tek kaynağı kullanır.
        public enum SurecAsamasi
        {
            BirinciSatisBekliyor,
            IkinciSatisBekliyor,
            PazarlikBekliyor,
            Madde6183_86Bekliyor,
            AliciyaTerkBekliyor,
            // Hem Pazarlık hem (modellenmemiş bir yol yüzünden) başka aşama kalmadığında;
            // pratikte oluşması beklenmez ama güvenlik amaçlı bir "dur" durumu olarak tutulur.
            SurecTamamlandi
        }

        // Aktif bir dosyanın süreçte tam olarak hangi aşamada olduğunu belirler. İki temel dal
        // vardır: (1) bir aşamada HİÇ alıcı çıkmazsa ("Alıcı Çıkmadı") bir sonraki PLANLI
        // aşamaya (1.Satış->2.Satış->Pazarlık) geçilir; (2) bir aşamayı kazanan alıcı işlemini
        // TAMAMLAMAZSA ("Alıcı İşlem Yapmadı") hemen ardından TEK BİR yeniden-satış denemesi
        // yapılır (2.Satış'ın kendisi ya da 6183/86 Tek Satış) - bu deneme de alıcı bulamazsa
        // (sonucu ne olursa olsun) süreç "Alıcıya Terk" ile sonuçlanır; Pazarlık'a hiç girilmez.
        // Satislar navigasyon koleksiyonunun yüklenmiş (Include) olması gerekir.
        [NotMapped]
        public SurecAsamasi Asama
        {
            get
            {
                var birinci = Satislar.FirstOrDefault(s => s.SatisTuru == SatisTuru.BirinciSatis);
                if (birinci == null) return SurecAsamasi.BirinciSatisBekliyor;

                var ikinci = Satislar.FirstOrDefault(s => s.SatisTuru == SatisTuru.IkinciSatis);
                if (ikinci == null) return SurecAsamasi.IkinciSatisBekliyor;

                if (birinci.SatisSonucu == SatisSonucu.AliciIslemYapmadi)
                {
                    // 2. Satış, 1. Satışı kazanıp kaçan alıcı yüzünden yapılan yeniden-satış
                    // denemesidir: burada da hiç alıcı çıkmazsa doğrudan Alıcıya Terk'e gidilir.
                    if (ikinci.SatisSonucu == SatisSonucu.AliciCikmadi) return SurecAsamasi.AliciyaTerkBekliyor;

                    if (ikinci.SatisSonucu == SatisSonucu.AliciIslemYapmadi)
                    {
                        var tek = Satislar.FirstOrDefault(s => s.SatisTuru == SatisTuru.Madde6183_86);
                        return tek == null ? SurecAsamasi.Madde6183_86Bekliyor : SurecAsamasi.AliciyaTerkBekliyor;
                    }

                    return SurecAsamasi.SurecTamamlandi; // ikinci == Satildi ise bu noktaya hiç gelinmez (Durum=Satildi olur)
                }

                if (ikinci.SatisSonucu == SatisSonucu.AliciCikmadi)
                {
                    // Her iki planlı ihalede de hiç alıcı çıkmadı - gerçek "satılamadı", Pazarlık'a girilir.
                    var pazarlik = Satislar.FirstOrDefault(s => s.SatisTuru == SatisTuru.Pazarlik);
                    if (pazarlik == null) return SurecAsamasi.PazarlikBekliyor;

                    if (pazarlik.SatisSonucu == SatisSonucu.AliciCikmadi) return SurecAsamasi.SurecTamamlandi;

                    if (pazarlik.SatisSonucu == SatisSonucu.AliciIslemYapmadi)
                    {
                        var tek = Satislar.FirstOrDefault(s => s.SatisTuru == SatisTuru.Madde6183_86);
                        return tek == null ? SurecAsamasi.Madde6183_86Bekliyor : SurecAsamasi.AliciyaTerkBekliyor;
                    }

                    return SurecAsamasi.SurecTamamlandi;
                }

                // ikinci == AliciIslemYapmadi: 2. Satışı kazanıp kaçan alıcı yüzünden 6183/86
                // Tek Satış ile bir kez daha denenir; o da alıcı bulamazsa Alıcıya Terk olunur.
                var tekSatis = Satislar.FirstOrDefault(s => s.SatisTuru == SatisTuru.Madde6183_86);
                return tekSatis == null ? SurecAsamasi.Madde6183_86Bekliyor : SurecAsamasi.AliciyaTerkBekliyor;
            }
        }

        // "Alıcıya Terk" aşamasındayken, 6183 sayılı Kanun Md. 85/86 uyarınca aracın resen
        // terk edileceği kişi: en son "kazanıp işlem yapmadığı" tespit edilen satış kaydı.
        [NotMapped]
        public Satis? TerkEdilecekSatis
        {
            get
            {
                if (Asama != SurecAsamasi.AliciyaTerkBekliyor) return null;

                var siralama = new[] { SatisTuru.Madde6183_86, SatisTuru.Pazarlik, SatisTuru.IkinciSatis, SatisTuru.BirinciSatis };
                foreach (var tur in siralama)
                {
                    var kayit = Satislar.FirstOrDefault(s => s.SatisTuru == tur);
                    if (kayit?.SatisSonucu == SatisSonucu.AliciIslemYapmadi) return kayit;
                }
                return null;
            }
        }

        // İki ihale arası fark: bir aşamayı kazanıp işlem yapmayan alıcıdan sonra yapılan
        // yeniden-satış denemesi GERÇEKTEN satılırsa (yeni bir alıcı bulunursa) ama bu
        // yeni bedel öncekinden DÜŞÜKSE, aradaki fark önceki (işlem yapmayan) alıcıdan
        // tahsil edilir (6183 sayılı Kanun Md. 85/86). Dosya bu durumda normal şekilde
        // "Satıldı" olarak kapanır; bu alan sadece ek bir tahsilat uyarısı amaçlıdır.
        [NotMapped]
        public (string KimdenTahsilEdilecek, decimal FarkTutari)? IkiIhaleArasiFark
        {
            get
            {
                var siralama = new[] { SatisTuru.BirinciSatis, SatisTuru.IkinciSatis, SatisTuru.Pazarlik, SatisTuru.Madde6183_86 };
                Satis? islemYapmayan = null;
                foreach (var tur in siralama)
                {
                    var kayit = Satislar.FirstOrDefault(s => s.SatisTuru == tur);
                    if (kayit == null) break;

                    if (kayit.SatisSonucu == SatisSonucu.AliciIslemYapmadi)
                    {
                        islemYapmayan = kayit;
                        continue;
                    }

                    if (kayit.SatisSonucu == SatisSonucu.Satildi)
                    {
                        if (islemYapmayan?.SatisBedeli.HasValue == true && kayit.SatisBedeli.HasValue
                            && kayit.SatisBedeli.Value < islemYapmayan.SatisBedeli.Value)
                        {
                            return (islemYapmayan.AliciAdiSoyadi ?? "-", islemYapmayan.SatisBedeli.Value - kayit.SatisBedeli.Value);
                        }
                        return null;
                    }

                    islemYapmayan = null; // AliciCikmadi - zincir kırılır, fark senaryosu oluşmaz.
                }
                return null;
            }
        }

        // Yukarıdaki Asama'nın ekranlarda gösterilen Türkçe karşılığı.
        [NotMapped]
        public string AsamaGorunen
        {
            get
            {
                if (Durum == DosyaDurumu.Satildi) return "Satıldı";
                if (Durum == DosyaDurumu.IptalEdildi) return "İptal Edildi";
                if (Durum == DosyaDurumu.IadeEdildi) return "İade Edildi";
                if (Durum == DosyaDurumu.Kapandi) return "Kapandı";
                if (Durum == DosyaDurumu.AliciyaTerkEdildi) return "Alıcıya Terk Edildi";

                return Asama switch
                {
                    SurecAsamasi.BirinciSatisBekliyor => "1. Satış Bekliyor",
                    SurecAsamasi.IkinciSatisBekliyor => "2. Satış Bekliyor",
                    SurecAsamasi.PazarlikBekliyor => "Pazarlık Bekliyor",
                    SurecAsamasi.Madde6183_86Bekliyor => "6183/86 Bekliyor (Tek Satış)",
                    SurecAsamasi.AliciyaTerkBekliyor => "Alıcıya Terk Bekliyor",
                    _ => "Süreç Tamamlandı (Satılamadı)"
                };
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
            DosyaDurumu.AliciyaTerkEdildi => "Alıcıya Terk Edildi",
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
