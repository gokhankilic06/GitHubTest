(function () {
  const { api, tarihGoster } = window.Ortak;
  const $ = (id) => document.getElementById(id);

  function hataGoster(mesaj) {
    $('genelHata').textContent = mesaj;
    $('genelHata').hidden = !mesaj;
  }

  async function yukle(tarih) {
    hataGoster('');
    try {
      const veri = await api('/api/yonetim/randevular' + (tarih ? '?tarih=' + encodeURIComponent(tarih) : ''));
      ciz(veri.randevular);
    } catch (e) {
      hataGoster(e.message);
    }
  }

  function ciz(randevular) {
    const govde = $('liste');
    govde.replaceChildren();
    const aktif = randevular.filter((r) => r.durum === 'aktif').length;
    $('sayac').textContent = `${randevular.length} kayıt (${aktif} aktif)`;
    for (const r of randevular) {
      const tr = document.createElement('tr');
      if (r.durum !== 'aktif') tr.className = 'iptal';
      const hucreler = [
        r.randevuNo, tarihGoster(r.tarih), r.saat, r.ilce,
        `${r.vefatEdenAd} ${r.vefatEdenSoyad}`, r.vefatEdenTc,
        `${r.basvuranAd} ${r.basvuranSoyad}`, r.basvuranDogumYili, r.basvuranTc, r.basvuranCep,
        r.durum === 'aktif' ? 'Aktif' : 'İptal',
      ];
      for (const h of hucreler) {
        const td = document.createElement('td');
        td.textContent = h;
        tr.append(td);
      }
      const islem = document.createElement('td');
      if (r.durum === 'aktif') {
        const b = document.createElement('button');
        b.className = 'btn sari kucuk';
        b.textContent = 'İptal Et';
        b.onclick = async () => {
          if (!confirm(`${r.randevuNo} numaralı randevu iptal edilsin mi?`)) return;
          try {
            await api(`/api/yonetim/randevular/${r.id}/iptal`, { method: 'POST' });
            yukle($('tarih').value);
          } catch (e) {
            hataGoster(e.message);
          }
        };
        islem.append(b);
      }
      tr.append(islem);
      govde.append(tr);
    }
  }

  $('listele').addEventListener('click', () => yukle($('tarih').value));
  $('tumu').addEventListener('click', () => { $('tarih').value = ''; yukle(''); });
  yukle('');
})();
