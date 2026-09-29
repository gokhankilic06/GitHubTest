import secrets

from django.db import models, transaction
from django.db.models.signals import pre_delete
from django.dispatch import receiver
from django.utils import timezone

KOD_ALFABESI = 'ABCDEFGHJKMNPQRSTUVWXYZ23456789'  # karışabilecek 0/O, 1/I/L yok
KOD_UZUNLUGU = 6


class Sinif(models.Model):
    ad = models.CharField('Sınıf adı', max_length=30, unique=True)
    sira = models.PositiveIntegerField('Sıra', default=0)

    class Meta:
        verbose_name = 'Sınıf'
        verbose_name_plural = 'Sınıflar'
        ordering = ['sira', 'ad']

    def __str__(self):
        return self.ad


class Soru(models.Model):
    sira = models.PositiveIntegerField('Sıra', unique=True)
    secenek_a = models.TextField('A seçeneği')
    secenek_b = models.TextField('B seçeneği')
    kisa_a = models.CharField('A kısa metin', max_length=200, help_text='Sonuç tablosunda görünür')
    kisa_b = models.CharField('B kısa metin', max_length=200, help_text='Sonuç tablosunda görünür')
    aktif = models.BooleanField('Aktif', default=True)

    class Meta:
        verbose_name = 'Soru'
        verbose_name_plural = 'Sorular'
        ordering = ['sira']

    def __str__(self):
        return f'{self.sira}. Soru'

    @property
    def secenekler(self):
        return [('A', self.secenek_a), ('B', self.secenek_b)]


def normalize_kod(deger):
    return ''.join(ch for ch in str(deger or '').upper() if ch.isascii() and ch.isalnum())[:20]


class VeliKodu(models.Model):
    kod = models.CharField('Kod', max_length=20, unique=True)
    sinif = models.ForeignKey(Sinif, on_delete=models.PROTECT, related_name='kodlar', verbose_name='Sınıf')
    olusturma = models.DateTimeField('Oluşturulma', auto_now_add=True)
    kullanim = models.DateTimeField('Kullanıldığı tarih', null=True, blank=True)

    class Meta:
        verbose_name = 'Veli kodu'
        verbose_name_plural = 'Veli kodları'
        ordering = ['sinif__sira', 'sinif__ad', 'olusturma', 'kod']

    def __str__(self):
        return self.kod

    @property
    def kullanildi(self):
        return self.kullanim is not None

    @staticmethod
    def rastgele_kod():
        return ''.join(secrets.choice(KOD_ALFABESI) for _ in range(KOD_UZUNLUGU))

    @classmethod
    def uret(cls, sinif, adet):
        """Belirtilen sınıf için benzersiz kodlar üretir ve listesini döndürür."""
        uretilen = []
        for _ in range(10):
            eksik = adet - len(uretilen)
            if eksik <= 0:
                break
            adaylar = {cls.rastgele_kod() for _ in range(eksik * 2)}
            adaylar -= set(cls.objects.filter(kod__in=adaylar).values_list('kod', flat=True))
            yeni = [cls(kod=k, sinif=sinif) for k in list(adaylar)[:eksik]]
            cls.objects.bulk_create(yeni, ignore_conflicts=True)
            uretilen += [v.kod for v in yeni]
        return uretilen


class VeliYaniti(models.Model):
    kod = models.OneToOneField(VeliKodu, on_delete=models.SET_NULL, null=True, blank=True, related_name='yanit', verbose_name='Veli kodu')
    ad_soyad = models.CharField('Ad Soyad', max_length=100)
    sinif = models.ForeignKey(Sinif, on_delete=models.PROTECT, related_name='yanitlar', verbose_name='Sınıf')
    tarih = models.DateTimeField('Tarih', auto_now_add=True)

    class Meta:
        verbose_name = 'Veli yanıtı'
        verbose_name_plural = 'Veli yanıtları'
        ordering = ['id']

    def __str__(self):
        return f'{self.ad_soyad} ({self.sinif})'


@receiver(pre_delete, sender=VeliYaniti)
def kodu_serbest_birak(sender, instance, **kwargs):
    # Yanıt silinince (tek tek veya toplu) veli kodu yeniden kullanılabilir hale gelir
    if instance.kod_id:
        VeliKodu.objects.filter(pk=instance.kod_id).update(kullanim=None)


class Cevap(models.Model):
    SECIMLER = [('A', 'A'), ('B', 'B')]

    yanit = models.ForeignKey(VeliYaniti, on_delete=models.CASCADE, related_name='cevaplar')
    soru = models.ForeignKey(Soru, on_delete=models.CASCADE, related_name='cevaplar')
    secim = models.CharField('Seçim', max_length=1, choices=SECIMLER)

    class Meta:
        verbose_name = 'Cevap'
        verbose_name_plural = 'Cevaplar'
        constraints = [models.UniqueConstraint(fields=['yanit', 'soru'], name='yanit_soru_tekil')]

    def __str__(self):
        return f'{self.soru}: {self.secim}'


class KodKullanilmis(Exception):
    pass


def yanit_kaydet(kod, ad_soyad, secimler):
    """
    Veli yanıtını kaydeder. secimler: {soru_id: 'A'|'B'}.
    Kod koşullu UPDATE ile işaretlenir; aynı kodla eşzamanlı iki gönderimden yalnızca biri geçer.
    """
    with transaction.atomic():
        guncellenen = VeliKodu.objects.filter(kod=kod, kullanim__isnull=True).update(kullanim=timezone.now())
        if not guncellenen:
            raise KodKullanilmis()
        veli_kodu = VeliKodu.objects.select_related('sinif').get(kod=kod)
        yanit = VeliYaniti.objects.create(kod=veli_kodu, ad_soyad=ad_soyad, sinif=veli_kodu.sinif)
        Cevap.objects.bulk_create([Cevap(yanit=yanit, soru_id=sid, secim=s) for sid, s in secimler.items()])
        return yanit
