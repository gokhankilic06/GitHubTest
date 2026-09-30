from urllib.parse import urlencode

from django.conf import settings
from django.contrib import messages
from django.contrib.admin.views.decorators import staff_member_required
from django.http import HttpResponse
from django.shortcuts import get_object_or_404, redirect, render
from django.urls import reverse
from django.views.decorators.http import require_POST

from . import excel
from .istatistik import sinif_ozeti, sonuclar
from .models import Sinif, Soru, VeliYaniti, yanit_kaydet

XLSX = 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'


# ---------------------------------------------------------------- Veli tarafı

def anket(request):
    sorular = list(Soru.objects.filter(aktif=True))
    ctx = {
        'sorular': sorular,
        'siniflar': Sinif.objects.all(),
        'soru_metni': settings.ANKET_SORU_METNI,
        'hatalar': [],
        'eksikler': set(),
    }

    if request.method == 'GET':
        return render(request, 'anket/anket.html', ctx)

    ad_soyad = ' '.join(request.POST.get('ad_soyad', '').split())[:100]
    sinif = Sinif.objects.filter(ad=request.POST.get('sinif')).first()
    secimler = {s.id: request.POST.get(f'soru_{s.id}') for s in sorular}
    ctx.update(ad_soyad=ad_soyad, secili_sinif=sinif)
    for s in sorular:
        s.secili = secimler[s.id]  # hata olursa seçimler korunur

    if not ad_soyad:
        ctx['hatalar'].append('Lütfen adınızı ve soyadınızı giriniz.')
    if not sinif:
        ctx['hatalar'].append('Lütfen sınıf seçiniz.')
    ctx['eksikler'] = {sid for sid, v in secimler.items() if v not in ('A', 'B')}
    if ctx['eksikler']:
        ctx['hatalar'].append('Lütfen tüm soruları yanıtlayınız. Eksik sorular kırmızı ile işaretlenmiştir.')

    if not ctx['hatalar']:
        yanit_kaydet(sinif, ad_soyad, secimler)
        return redirect('anket:tesekkurler')

    return render(request, 'anket/anket.html', ctx, status=400)


def tesekkurler(request):
    return render(request, 'anket/tesekkurler.html')


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
    })


@staff_member_required
@require_POST
def yanit_sil(request, pk):
    yanit = get_object_or_404(VeliYaniti, pk=pk)
    yanit.delete()
    messages.success(request, f'"{yanit.ad_soyad}" adlı velinin yanıtları silindi.')
    return redirect(reverse('anket:panel') + _sinif_sorgu(_secili_sinif(request)))


@staff_member_required
@require_POST
def sifirla(request):
    VeliYaniti.objects.all().delete()
    messages.success(request, 'Tüm anket yanıtları silindi.')
    return redirect('anket:panel')


@staff_member_required
def sonuc_excel(request):
    sinif = _secili_sinif(request)
    icerik = excel.sonuc_raporu(sonuclar(sinif), sinif_ozeti())
    ek = f'-{sinif.ad}' if sinif else ''
    resp = HttpResponse(icerik, content_type=XLSX)
    resp['Content-Disposition'] = f'attachment; filename="veli-anket-sonuclari{ek}.xlsx"'
    return resp
