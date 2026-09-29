from urllib.parse import urlencode

from django.conf import settings
from django.contrib import messages
from django.contrib.admin.views.decorators import staff_member_required
from django.db import transaction
from django.http import HttpResponse, JsonResponse
from django.shortcuts import get_object_or_404, redirect, render
from django.urls import reverse
from django.views.decorators.http import require_GET, require_POST

from . import excel
from .istatistik import sinif_ozeti, sonuclar
from .models import KodKullanilmis, Sinif, Soru, VeliKodu, VeliYaniti, normalize_kod, yanit_kaydet

AZAMI_KOD = 200
XLSX = 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'


def _kod_durumu(kod):
    """(sınıf, hata) döndürür."""
    if not kod:
        return None, 'Lütfen veli kodunu giriniz.'
    vk = VeliKodu.objects.select_related('sinif').filter(kod=kod).first()
    if not vk:
        return None, 'Veli kodu bulunamadı. Lütfen kodu kontrol ediniz.'
    if vk.kullanildi:
        return None, 'Bu veli kodu daha önce kullanılmış.'
    return vk.sinif, None


# ---------------------------------------------------------------- Veli tarafı

def anket(request):
    sorular = list(Soru.objects.filter(aktif=True))
    ctx = {'sorular': sorular, 'soru_metni': settings.ANKET_SORU_METNI, 'hatalar': [], 'eksikler': set()}

    if request.method == 'GET':
        kod = normalize_kod(request.GET.get('kod'))
        ctx['kod'] = kod
        if kod:
            ctx['kod_sinif'], ctx['kod_hata'] = _kod_durumu(kod)
        return render(request, 'anket/anket.html', ctx)

    kod = normalize_kod(request.POST.get('kod'))
    ad_soyad = ' '.join(request.POST.get('ad_soyad', '').split())[:100]
    secimler = {s.id: request.POST.get(f'soru_{s.id}') for s in sorular}
    sinif, kod_hata = _kod_durumu(kod)
    ctx.update(kod=kod, ad_soyad=ad_soyad, kod_sinif=sinif, kod_hata=kod_hata)
    for s in sorular:
        s.secili = secimler[s.id]  # hata olursa seçimler korunur

    if kod_hata:
        ctx['hatalar'].append(kod_hata)
    if not ad_soyad:
        ctx['hatalar'].append('Lütfen adınızı ve soyadınızı giriniz.')
    ctx['eksikler'] = {sid for sid, v in secimler.items() if v not in ('A', 'B')}
    if ctx['eksikler']:
        ctx['hatalar'].append('Lütfen tüm soruları yanıtlayınız. Eksik sorular kırmızı ile işaretlenmiştir.')

    if not ctx['hatalar']:
        try:
            yanit_kaydet(kod, ad_soyad, secimler)
            return redirect('anket:tesekkurler')
        except KodKullanilmis:
            ctx['hatalar'].append('Bu veli kodu daha önce kullanılmış.')

    return render(request, 'anket/anket.html', ctx, status=400)


def tesekkurler(request):
    return render(request, 'anket/tesekkurler.html')


@require_GET
def kod_kontrol(request, kod):
    sinif, hata = _kod_durumu(normalize_kod(kod))
    if hata:
        return JsonResponse({'error': hata}, status=409 if 'kullanılmış' in hata else 404)
    return JsonResponse({'className': sinif.ad})


# ---------------------------------------------------------------- Yönetici paneli

def _secili_sinif(request):
    ad = request.GET.get('sinif') or request.POST.get('sinif')
    return Sinif.objects.filter(ad=ad).first() if ad else None


def _sinif_sorgu(sinif):
    return '?' + urlencode({'sinif': sinif.ad}) if sinif else ''


@staff_member_required
def panel(request):
    sinif = _secili_sinif(request)
    veri = sonuclar(sinif)
    ozet = sinif_ozeti()
    grafik = {
        'sorular': [f'{s["soru"].sira}. Soru' for s in veri['soru_istatistik']],
        'kisaA': [s['soru'].kisa_a for s in veri['soru_istatistik']],
        'kisaB': [s['soru'].kisa_b for s in veri['soru_istatistik']],
        'a': [s['a'] for s in veri['soru_istatistik']],
        'b': [s['b'] for s in veri['soru_istatistik']],
        'yuzdeA': [s['yuzde_a'] for s in veri['soru_istatistik']],
        'yuzdeB': [s['yuzde_b'] for s in veri['soru_istatistik']],
        'siniflar': [o['sinif'].ad for o in ozet if o['katilan']],
        'katilan': [o['katilan'] for o in ozet if o['katilan']],
    }
    return render(request, 'anket/panel.html', {
        **veri,
        'ozet': ozet,
        'grafik': grafik,
        'siniflar': Sinif.objects.all(),
        'secili_sinif': sinif,
        'sinif_sorgu': _sinif_sorgu(sinif),
        'sekme': 'sonuclar',
    })


@staff_member_required
@require_POST
def yanit_sil(request, pk):
    yanit = get_object_or_404(VeliYaniti, pk=pk)
    yanit.delete()
    messages.success(request, f'"{yanit.ad_soyad}" adlı velinin yanıtları silindi. Velinin kodu yeniden kullanılabilir.')
    return redirect(reverse('anket:panel') + _sinif_sorgu(_secili_sinif(request)))


@staff_member_required
@require_POST
def sifirla(request):
    with transaction.atomic():
        VeliYaniti.objects.all().delete()
        VeliKodu.objects.update(kullanim=None)
    messages.success(request, 'Tüm anket yanıtları silindi. Kodlar yeniden kullanılabilir.')
    return redirect('anket:panel')


@staff_member_required
def sonuc_excel(request):
    sinif = _secili_sinif(request)
    icerik = excel.sonuc_raporu(sonuclar(sinif), sinif_ozeti())
    ek = f'-{sinif.ad}' if sinif else ''
    resp = HttpResponse(icerik, content_type=XLSX)
    resp['Content-Disposition'] = f'attachment; filename="veli-anket-sonuclari{ek}.xlsx"'
    return resp


# ---------------------------------------------------------------- Veli kodları

def _kodlar(sinif):
    qs = VeliKodu.objects.select_related('sinif', 'yanit')
    return qs.filter(sinif=sinif) if sinif else qs


@staff_member_required
def kodlar(request):
    if request.method == 'POST':
        sinif = Sinif.objects.filter(ad=request.POST.get('uret_sinif')).first()
        try:
            adet = int(request.POST.get('adet', ''))
        except ValueError:
            adet = 0
        if not sinif:
            messages.error(request, 'Geçerli bir sınıf seçiniz.')
        elif not 1 <= adet <= AZAMI_KOD:
            messages.error(request, f'Kod sayısı 1 ile {AZAMI_KOD} arasında olmalıdır.')
        else:
            uretilen = VeliKodu.uret(sinif, adet)
            messages.success(request, f'{sinif.ad} sınıfı için {len(uretilen)} kod üretildi.')
            return redirect(reverse('anket:kodlar') + _sinif_sorgu(sinif))
        return redirect('anket:kodlar')

    sinif = _secili_sinif(request)
    liste = list(_kodlar(sinif))
    kullanilan = sum(1 for k in liste if k.kullanildi)
    return render(request, 'anket/kodlar.html', {
        'kodlar': liste,
        'kullanilan': kullanilan,
        'kullanilmayan': len(liste) - kullanilan,
        'siniflar': Sinif.objects.all(),
        'secili_sinif': sinif,
        'sinif_sorgu': _sinif_sorgu(sinif),
        'azami_kod': AZAMI_KOD,
        'site': request.build_absolute_uri('/'),
        'sekme': 'kodlar',
    })


@staff_member_required
@require_POST
def kod_sil(request, kod):
    silinen, _ = VeliKodu.objects.filter(kod=kod, kullanim__isnull=True).delete()
    if silinen:
        messages.success(request, f'{kod} kodu silindi.')
    else:
        messages.error(request, 'Kod bulunamadı veya kullanılmış (kullanılmış kodlar silinemez).')
    return redirect(reverse('anket:kodlar') + _sinif_sorgu(_secili_sinif(request)))


@staff_member_required
@require_POST
def kullanilmayanlari_sil(request):
    sinif = _secili_sinif(request)
    silinen, _ = _kodlar(sinif).filter(kullanim__isnull=True).delete()
    messages.success(request, f'{silinen} kullanılmamış kod silindi.')
    return redirect(reverse('anket:kodlar') + _sinif_sorgu(sinif))


@staff_member_required
def kod_excel(request):
    sinif = _secili_sinif(request)
    ek = f'-{sinif.ad}' if sinif else ''
    resp = HttpResponse(excel.kod_raporu(_kodlar(sinif)), content_type=XLSX)
    resp['Content-Disposition'] = f'attachment; filename="veli-kodlari{ek}.xlsx"'
    return resp


@staff_member_required
def kod_yazdir(request):
    sinif = _secili_sinif(request)
    return render(request, 'anket/kod_yazdir.html', {
        'kodlar': _kodlar(sinif).filter(kullanim__isnull=True),
        'site': request.build_absolute_uri('/'),
    })
