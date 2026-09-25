using System.ComponentModel.DataAnnotations;

namespace AracSatisSistemi.Models
{
    // "Güncellemeler" ekranından yönetilebilen basit referans listeleri.
    // Bu sayede araç markası / cinsi / vergi dairesi / otopark listeleri
    // koda dokunmadan eklenip çıkarılabilir.

    public class AracMarkasi
    {
        public int Id { get; set; }
        [Required] public string Ad { get; set; } = string.Empty;
    }

    public class AracCinsi
    {
        public int Id { get; set; }
        [Required] public string Ad { get; set; } = string.Empty;

        // Otopark ücret tarifesinde hangi kategoriye (Küçük Araç / Minibüs-Kamyonet /
        // Büyük Araç / Motor) girdiği. Alıcı Ödeme Bilgileri ekranındaki otopark ücreti
        // hesaplamasında kullanılır.
        [Display(Name = "Otopark Ücret Kategorisi")]
        public OtoparkAracKategorisi OtoparkKategorisi { get; set; } = OtoparkAracKategorisi.KucukArac;
    }

    public class VergiDairesiKaydi
    {
        public int Id { get; set; }
        [Required] public string Ad { get; set; } = string.Empty;
        [Required] public string Sehir { get; set; } = string.Empty;
    }

    public class OtoparkKaydi
    {
        public int Id { get; set; }
        [Required] public string Ad { get; set; } = string.Empty;
    }

    public class AracRengi
    {
        public int Id { get; set; }
        [Required] public string Ad { get; set; } = string.Empty;
    }

    // Komisyon üyeleri - tek satırlık ayar; her yeni dosyada bu bilgiler
    // otomatik gelir, güncelleme yetkisi olan kullanıcı burayı değiştirebilir.
    public class KomisyonAyari
    {
        public int Id { get; set; }
        [Display(Name = "Komisyon Başkanı")] public string Baskan { get; set; } = string.Empty;
        [Display(Name = "Üye 1")] public string Uye1 { get; set; } = string.Empty;
        [Display(Name = "Üye 2")] public string Uye2 { get; set; } = string.Empty;
        [Display(Name = "Üye 3")] public string Uye3 { get; set; } = string.Empty;
        [Display(Name = "Üye 4")] public string Uye4 { get; set; } = string.Empty;
    }

    // Komisyon başkanlığı/üyeliği yapabilecek memurların listesi. Güncellemeler ekranından
    // eklenip çıkarılır; Komisyon İşlemleri ekranında ve varsayılan Komisyon Bilgileri
    // ayarında buradan seçim yapılır.
    public class Memur
    {
        public int Id { get; set; }
        [Required, Display(Name = "Adı Soyadı")]
        public string AdSoyad { get; set; } = string.Empty;
    }

    // Genel sistem ayarları (günlük otopark ücreti gibi elden verilen tarifeler)
    public class GenelAyar
    {
        public int Id { get; set; }

        // Eski tekli (kategori ayrımı olmayan) günlük otopark ücreti - geriye dönük uyumluluk
        // için korunuyor; yeni hesaplamalar kategoriye göre aşağıdaki tarifeleri kullanır.
        [Display(Name = "Günlük Otopark Ücreti (TL)")]
        public decimal GunlukOtoparkUcreti { get; set; }

        // ---- KADEMELİ OTOPARK ÜCRET TARİFESİ (araç kategorisi + süreye göre) ----
        // İlk 180 gün (6 ay) "6 Aya Kadar" tarifesi, sonraki günler "6 Aydan Sonra"
        // (genelde ilkin yarısı) tarifesi üzerinden hesaplanır.
        [Display(Name = "Küçük Araç - 6 Aya Kadar (TL/gün)")]
        public decimal KucukArac6AyaKadar { get; set; }
        [Display(Name = "Küçük Araç - 6 Aydan Sonra (TL/gün)")]
        public decimal KucukArac6AydanSonra { get; set; }

        [Display(Name = "Minibüs/Kamyonet - 6 Aya Kadar (TL/gün)")]
        public decimal MinibusKamyonet6AyaKadar { get; set; }
        [Display(Name = "Minibüs/Kamyonet - 6 Aydan Sonra (TL/gün)")]
        public decimal MinibusKamyonet6AydanSonra { get; set; }

        [Display(Name = "Büyük Araç/Kamyon/Tır - 6 Aya Kadar (TL/gün)")]
        public decimal BuyukArac6AyaKadar { get; set; }
        [Display(Name = "Büyük Araç/Kamyon/Tır - 6 Aydan Sonra (TL/gün)")]
        public decimal BuyukArac6AydanSonra { get; set; }

        [Display(Name = "Motor - 6 Aya Kadar (TL/gün)")]
        public decimal Motor6AyaKadar { get; set; }
        [Display(Name = "Motor - 6 Aydan Sonra (TL/gün)")]
        public decimal Motor6AydanSonra { get; set; }

        [Display(Name = "Çekici Ücreti (TL, sabit)")]
        public decimal CekiciUcretiSabit { get; set; }

        // Kategoriye göre (6 aya kadar, 6 aydan sonra) günlük ücret çiftini döner.
        public (decimal IlkDonem, decimal IkinciDonem) OtoparkTarifesi(OtoparkAracKategorisi kategori) => kategori switch
        {
            OtoparkAracKategorisi.KucukArac => (KucukArac6AyaKadar, KucukArac6AydanSonra),
            OtoparkAracKategorisi.MinibusKamyonet => (MinibusKamyonet6AyaKadar, MinibusKamyonet6AydanSonra),
            OtoparkAracKategorisi.BuyukArac => (BuyukArac6AyaKadar, BuyukArac6AydanSonra),
            OtoparkAracKategorisi.Motor => (Motor6AyaKadar, Motor6AydanSonra),
            _ => (0m, 0m)
        };
    }

    // Daha önce araç satın almış kişi/kurumların kaydı. Alıcı Ödeme Bilgileri ekranında
    // Vergi Numarası girilip "Kayıtlı Alıcılardan Seç" ile bulunduğunda kimlik bilgileri
    // otomatik doldurulur; ilk kez araç alan biri için formdan manuel girilip buraya kaydedilir.
    public class AliciKaydi
    {
        public int Id { get; set; }
        [Required, Display(Name = "Vergi Numarası (TCKN/VKN)")]
        public string VergiNumarasi { get; set; } = string.Empty;
        [Required, Display(Name = "Adı Soyadı / Ünvanı")]
        public string AdiSoyadiUnvani { get; set; } = string.Empty;
        [Display(Name = "Vekili")]
        public string? Vekili { get; set; }
        [Display(Name = "Adresi")]
        public string? Adresi { get; set; }
        [Display(Name = "Telefon")]
        public string? Telefon { get; set; }
    }

    // Resmi tatil günleri: ihale (satış) tarihi hesaplanırken bu günler atlanır.
    // Sabit tarihli milli/resmi bayramlar başlangıçta otomatik yüklenir; Ramazan/Kurban
    // Bayramı gibi dini bayramlar her yıl değiştiğinden Güncellemeler ekranından elle
    // eklenmelidir.
    public class ResmiTatil
    {
        public int Id { get; set; }
        [Display(Name = "Tarih")]
        [DataType(DataType.Date)]
        public DateTime Tarih { get; set; }
        [Display(Name = "Açıklama")]
        public string? Aciklama { get; set; }
    }
}
