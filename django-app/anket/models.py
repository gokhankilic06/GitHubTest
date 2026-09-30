from django.db import models, transaction


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


class VeliYaniti(models.Model):
    ad_soyad = models.CharField('Ad Soyad', max_length=100)
    sinif = models.ForeignKey(Sinif, on_delete=models.PROTECT, related_name='yanitlar', verbose_name='Sınıf')
    tarih = models.DateTimeField('Tarih', auto_now_add=True)

    class Meta:
        verbose_name = 'Veli yanıtı'
        verbose_name_plural = 'Veli yanıtları'
        ordering = ['id']

    def __str__(self):
        return f'{self.ad_soyad} ({self.sinif})'


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


def yanit_kaydet(sinif, ad_soyad, secimler):
    """Veli yanıtını kaydeder. secimler: {soru_id: 'A'|'B'}."""
    with transaction.atomic():
        yanit = VeliYaniti.objects.create(ad_soyad=ad_soyad, sinif=sinif)
        Cevap.objects.bulk_create([Cevap(yanit=yanit, soru_id=sid, secim=s) for sid, s in secimler.items()])
        return yanit
