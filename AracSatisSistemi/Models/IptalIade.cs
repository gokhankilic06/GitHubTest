using System.ComponentModel.DataAnnotations;

namespace AracSatisSistemi.Models
{
    // İADE İPTAL İŞLEMLERİ + DOSYA KAPATMA ekranına karşılık gelen entity.
    public class IptalIade
    {
        public int Id { get; set; }

        [Required]
        public int DosyaId { get; set; }
        public Dosya? Dosya { get; set; }

        // ---- SATIŞ İPTALİ ----
        [Display(Name = "Satış İptali")]
        public bool SatisIptaliSecildi { get; set; }
        [Display(Name = "İptal Yazı Tarihi")]
        [DataType(DataType.Date)]
        public DateTime? IptalYaziTarihi { get; set; }
        [Display(Name = "İptal Yazı Sayısı")]
        public string? IptalYaziSayisi { get; set; }
        [Display(Name = "İptal Nedeni")]
        public string? IptalNedeni { get; set; }

        // ---- SATIŞ İADESİ ----
        [Display(Name = "Satış İadesi")]
        public bool SatisIadesiSecildi { get; set; }
        [Display(Name = "İade Yazı Tarihi")]
        [DataType(DataType.Date)]
        public DateTime? IadeYaziTarihi { get; set; }
        [Display(Name = "İade Yazı Sayısı")]
        public string? IadeYaziSayisi { get; set; }
        [Display(Name = "İade Nedeni")]
        public string? IadeNedeni { get; set; }
        [Display(Name = "İade Edilecek Tutar (TL)")]
        [System.ComponentModel.DataAnnotations.Schema.Column(TypeName = "decimal(18,2)")]
        public decimal? IadeTutari { get; set; }

        // İptal edilirken satılmış olan Satış kaydı silinir (dosya yeniden satışa
        // çıkabilsin diye); bu iki alan o satışın özetini (kim, ne kadar) kalıcı
        // olarak saklar - Dosya Detayı ve bu ekranda geçmiş bilgi olarak gösterilir.
        [Display(Name = "İptal Edilen Satış Türü")]
        public string? IptalEdilenSatisTuru { get; set; }
        [Display(Name = "İptal Edilen Satış Bedeli (TL)")]
        [System.ComponentModel.DataAnnotations.Schema.Column(TypeName = "decimal(18,2)")]
        public decimal? IptalEdilenSatisBedeli { get; set; }
        [Display(Name = "İptal Edilen Satış - Alıcı")]
        public string? IptalEdilenAliciAdiSoyadi { get; set; }

        // ---- DOSYA KAPATMA ----
        [Display(Name = "Dosya Kapatıldı mı?")]
        public bool DosyaKapatildi { get; set; }
        [Display(Name = "Vergi Dairesi Yazı Tarihi")]
        [DataType(DataType.Date)]
        public DateTime? KapatmaVergiDairesiYaziTarihi { get; set; }
        [Display(Name = "Vergi Dairesi Yazı Sayısı")]
        public string? KapatmaVergiDairesiYaziSayisi { get; set; }
    }
}
