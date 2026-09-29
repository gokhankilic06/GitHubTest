import threading
from io import BytesIO

from django.contrib.auth.models import User
from django.db import connection
from django.test import TestCase, TransactionTestCase, skipUnlessDBFeature
from django.urls import reverse
from openpyxl import load_workbook

from .models import Sinif, Soru, VeliKodu, VeliYaniti, KodKullanilmis, yanit_kaydet


def cevaplar(secim):
    return {f'soru_{s.id}': secim for s in Soru.objects.filter(aktif=True)}


class AnketTesti(TestCase):
    def setUp(self):
        self.s1a = Sinif.objects.get(ad='1-A')
        self.s2b = Sinif.objects.get(ad='2-B')
        self.admin = User.objects.create_superuser('yonetici', 'y@example.com', 'sifre-12345')

    def gonder(self, kod, ad='Ali Veli', secim='A', **ekstra):
        return self.client.post(reverse('anket:anket'), {'kod': kod, 'ad_soyad': ad, **cevaplar(secim), **ekstra})

    def test_baslangic_verisi(self):
        self.assertEqual(Soru.objects.count(), 13)
        self.assertEqual(Sinif.objects.count(), 22)

    def test_kod_uretimi_benzersiz(self):
        kodlar = VeliKodu.uret(self.s1a, 50)
        self.assertEqual(len(set(kodlar)), 50)
        self.assertEqual(VeliKodu.objects.filter(sinif=self.s1a).count(), 50)

    def test_kod_kontrol(self):
        kod = VeliKodu.uret(self.s1a, 1)[0]
        r = self.client.get(reverse('anket:kod_kontrol', args=[kod.lower()]))
        self.assertEqual(r.json(), {'className': '1-A'})
        self.assertEqual(self.client.get(reverse('anket:kod_kontrol', args=['YOKKOD'])).status_code, 404)

    def test_tek_kullanimlik_kod_akisi(self):
        kod = VeliKodu.uret(self.s1a, 1)[0]
        r = self.gonder(kod)
        self.assertRedirects(r, reverse('anket:tesekkurler'))
        yanit = VeliYaniti.objects.get()
        self.assertEqual(yanit.sinif, self.s1a)
        self.assertEqual(yanit.cevaplar.count(), 13)

        # Aynı kod ikinci kez kullanılamaz
        r = self.gonder(kod, ad='Başka')
        self.assertEqual(r.status_code, 400)
        self.assertContains(r, 'daha önce kullanılmış', status_code=400)
        self.assertEqual(VeliYaniti.objects.count(), 1)
        self.assertEqual(self.client.get(reverse('anket:kod_kontrol', args=[kod])).status_code, 409)

    def test_eksik_bilgi_reddedilir_ve_secimler_korunur(self):
        kod = VeliKodu.uret(self.s1a, 1)[0]
        veri = {'kod': kod, 'ad_soyad': 'Ali', **cevaplar('B')}
        ilk = Soru.objects.get(sira=1)
        del veri[f'soru_{ilk.id}']
        r = self.client.post(reverse('anket:anket'), veri)
        self.assertEqual(r.status_code, 400)
        self.assertContains(r, 'tüm soruları yanıtlayınız', status_code=400)
        self.assertContains(r, 'class="question missing"', count=1, status_code=400)
        self.assertContains(r, 'value="B" checked', count=12, status_code=400)
        self.assertEqual(VeliYaniti.objects.count(), 0)
        self.assertIsNone(VeliKodu.objects.get(kod=kod).kullanim)

        self.assertEqual(self.gonder(kod, ad='  ').status_code, 400)
        self.assertEqual(self.gonder('YANLIS').status_code, 400)

    def test_panel_giris_gerektirir(self):
        for ad in ['anket:panel', 'anket:kodlar', 'anket:sonuc_excel', 'anket:kod_excel']:
            r = self.client.get(reverse(ad))
            self.assertEqual(r.status_code, 302, ad)
            self.assertIn('/admin/login/', r['Location'])

    def test_panel_sinif_filtresi_ve_istatistik(self):
        k1, k2, k3 = VeliKodu.uret(self.s1a, 3)
        (k4,) = VeliKodu.uret(self.s2b, 1)
        self.gonder(k1, 'Ali', 'A')
        self.gonder(k2, 'Ayşe', 'B')
        self.gonder(k4, 'Mehmet', 'B')
        self.client.force_login(self.admin)

        r = self.client.get(reverse('anket:panel'))
        self.assertEqual(r.context['toplam'], 3)
        ilk = r.context['soru_istatistik'][0]
        self.assertEqual((ilk['a'], ilk['b'], ilk['yuzde_a'], ilk['yuzde_b']), (1, 2, 33, 67))
        ozet = {o['sinif'].ad: o for o in r.context['ozet']}
        self.assertEqual((ozet['1-A']['katilan'], ozet['1-A']['kod_sayisi'], ozet['1-A']['oran']), (2, 3, 67))
        self.assertContains(r, '3 kişi ankete katılmıştır')

        r = self.client.get(reverse('anket:panel'), {'sinif': '1-A'})
        self.assertEqual(r.context['toplam'], 2)
        ilk = r.context['soru_istatistik'][0]
        self.assertEqual((ilk['a'], ilk['b'], ilk['yuzde_a']), (1, 1, 50))
        self.assertContains(r, '1-A sınıfından 2 kişi')

    def test_excel_raporu(self):
        k1, k2 = VeliKodu.uret(self.s1a, 2)
        (k3,) = VeliKodu.uret(self.s2b, 1)
        self.gonder(k1, 'Ali', 'A')
        self.gonder(k3, 'Mehmet', 'B')
        self.client.force_login(self.admin)

        r = self.client.get(reverse('anket:sonuc_excel'), {'sinif': '1-A'})
        self.assertEqual(r.status_code, 200)
        wb = load_workbook(BytesIO(r.content))
        self.assertEqual(wb.sheetnames, ['Katılımcılar', 'Soru Bazlı Dağılım', 'Bireysel Yanıtlar', 'Şık Bazlı Sonuçlar', 'Sınıf Özeti'])
        katilim = wb['Katılımcılar']
        self.assertEqual(katilim.max_row, 2)
        self.assertEqual(katilim['C2'].value, '1-A')
        self.assertEqual(wb['Şık Bazlı Sonuçlar'].max_row, 1 + 26)

        r = self.client.get(reverse('anket:kod_excel'))
        self.assertEqual(load_workbook(BytesIO(r.content))['Veli Kodları'].max_row, 4)

    def test_yanit_silinince_kod_serbest_kalir(self):
        kod = VeliKodu.uret(self.s1a, 1)[0]
        self.gonder(kod)
        self.client.force_login(self.admin)
        yanit = VeliYaniti.objects.get()
        r = self.client.post(reverse('anket:yanit_sil', args=[yanit.pk]))
        self.assertEqual(r.status_code, 302)
        self.assertFalse(VeliYaniti.objects.exists())
        self.assertIsNone(VeliKodu.objects.get(kod=kod).kullanim)

    def test_kod_yonetimi(self):
        self.client.force_login(self.admin)
        r = self.client.post(reverse('anket:kodlar'), {'uret_sinif': '1-A', 'adet': '5'})
        self.assertEqual(r.status_code, 302)
        self.assertEqual(VeliKodu.objects.count(), 5)
        self.client.post(reverse('anket:kodlar'), {'uret_sinif': '1-A', 'adet': '999'})
        self.client.post(reverse('anket:kodlar'), {'uret_sinif': 'Yok', 'adet': '5'})
        self.assertEqual(VeliKodu.objects.count(), 5)

        kullanilan = VeliKodu.objects.first().kod
        self.gonder(kullanilan)
        self.client.force_login(self.admin)

        # Kullanılmış kod silinemez
        self.client.post(reverse('anket:kod_sil', args=[kullanilan]))
        self.assertTrue(VeliKodu.objects.filter(kod=kullanilan).exists())

        r = self.client.get(reverse('anket:kod_yazdir'))
        self.assertContains(r, 'class="code-slip"', count=4)

        self.client.post(reverse('anket:kullanilmayanlari_sil'))
        self.assertEqual(list(VeliKodu.objects.values_list('kod', flat=True)), [kullanilan])

    def test_sifirla(self):
        kod = VeliKodu.uret(self.s1a, 1)[0]
        self.gonder(kod)
        self.client.force_login(self.admin)
        self.client.post(reverse('anket:sifirla'))
        self.assertFalse(VeliYaniti.objects.exists())
        self.assertIsNone(VeliKodu.objects.get(kod=kod).kullanim)

    def test_pasif_soru_ankette_gorunmez(self):
        Soru.objects.filter(sira=13).update(aktif=False)
        kod = VeliKodu.uret(self.s1a, 1)[0]
        r = self.client.get(reverse('anket:anket'))
        self.assertContains(r, 'class="question"', count=12)
        self.assertRedirects(self.gonder(kod), reverse('anket:tesekkurler'))


class EszamanlilikTesti(TransactionTestCase):
    """Aynı koda eşzamanlı gönderimlerden yalnızca biri kabul edilmeli (PostgreSQL'de gerçek eşzamanlılık)."""

    @skipUnlessDBFeature('has_select_for_update')
    def test_ayni_kod_eszamanli(self):
        sinif = Sinif.objects.create(ad='Eşzamanlılık')
        soru = Soru.objects.create(sira=999, secenek_a='a', secenek_b='b', kisa_a='a', kisa_b='b')
        kod = VeliKodu.uret(sinif, 1)[0]
        secimler = {soru.id: 'A'}
        sonuclar = []

        def gonder(i):
            try:
                yanit_kaydet(kod, f'Veli {i}', secimler)
                sonuclar.append('ok')
            except KodKullanilmis:
                sonuclar.append('red')
            finally:
                connection.close()

        threads = [threading.Thread(target=gonder, args=(i,)) for i in range(5)]
        for t in threads:
            t.start()
        for t in threads:
            t.join()
        self.assertEqual(sonuclar.count('ok'), 1)
        self.assertEqual(VeliYaniti.objects.count(), 1)
