"""Sonuç hesaplamaları: soru bazlı dağılım, bireysel tablo, sınıf özeti."""

from django.db.models import Count, Q

from .models import Cevap, Sinif, Soru, VeliYaniti


def yuzde(n, toplam):
    # .5 yukarı yuvarlanır (Node sürümündeki Math.round ile aynı sonuç)
    return int(n * 100 / toplam + 0.5) if toplam else 0


def sonuclar(sinif=None):
    sorular = list(Soru.objects.filter(aktif=True))
    qs = VeliYaniti.objects.all()
    if sinif:
        qs = qs.filter(sinif=sinif)
    yanitlar = list(qs.select_related('sinif', 'kod').order_by('id'))
    toplam = len(yanitlar)
    cevaplar = Cevap.objects.filter(yanit__in=qs.values('id'))

    # Her yanıtın seçimleri: {yanit_id: {soru_id: 'A'|'B'}}
    secimler = {}
    for yanit_id, soru_id, secim in cevaplar.values_list('yanit_id', 'soru_id', 'secim'):
        secimler.setdefault(yanit_id, {})[soru_id] = secim

    sayimlar = {
        r['soru_id']: r
        for r in cevaplar.values('soru_id')
        .annotate(a=Count('id', filter=Q(secim='A')), b=Count('id', filter=Q(secim='B')))
    }

    soru_istatistik = []
    for s in sorular:
        c = sayimlar.get(s.id, {'a': 0, 'b': 0})
        soru_istatistik.append({
            'soru': s,
            'a': c['a'], 'b': c['b'],
            'yuzde_a': yuzde(c['a'], toplam), 'yuzde_b': yuzde(c['b'], toplam),
        })

    satirlar = [
        {'yanit': y, 'secimler': [secimler.get(y.id, {}).get(s.id, '') for s in sorular]}
        for y in yanitlar
    ]

    return {'sorular': sorular, 'toplam': toplam, 'soru_istatistik': soru_istatistik, 'satirlar': satirlar}


def sinif_ozeti():
    ozet = []
    for s in Sinif.objects.annotate(katilan=Count('yanitlar', distinct=True), kod_sayisi=Count('kodlar', distinct=True)):
        if s.katilan or s.kod_sayisi:
            ozet.append({'sinif': s, 'katilan': s.katilan, 'kod_sayisi': s.kod_sayisi, 'oran': yuzde(s.katilan, s.kod_sayisi)})
    return ozet
