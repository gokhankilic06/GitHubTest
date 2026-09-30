(function () {
  const { api, tarihGoster, tcGecerli } = window.Ortak;
  const $ = (id) => document.getElementById(id);

  let kimlik = null; // son başarılı sorgunun { randevuNo, basvuranTc } bilgisi

  function mesaj(id, metin) {
    $(id).textContent = metin;
    $(id).hidden = !metin;
  }

  function alanHatasi(id, metin) {
    $(id).classList.toggle('hatali', !!metin);
    $(id).parentElement.querySelector('.alan-hata').textContent = metin || '';
  }

  $('basvuranTc').addEventListener('input', (e) => { e.target.value = e.target.value.replace(/\D/g, ''); });
  $('randevuNo').addEventListener('input', (e) => {
    e.target.value = e.target.value.toUpperCase().replace(/[^A-Z0-9]/g, '');
  });

  function sonucGoster(r) {
    const durumlar = {
      aktif: ['Aktif', 'aktif'],
      iptal: ['İptal Edildi', 'iptal'],
      gecmis: ['Tarihi Geçmiş', 'gecmis'],
    };
    const anahtar = r.durum === 'aktif' && !r.iptalEdilebilir ? 'gecmis' : r.durum;
    const [durumYazi, durumSinif] = durumlar[anahtar] || [r.durum, ''];

    const satirlar = [
      ['Randevu Numarası', r.randevuNo, 'no'],
      ['Durum', null],
      ['Vergi Dairesi', r.vergiDairesi],
      ['Randevu Tarihi', tarihGoster(r.tarih)],
      ['Randevu Saati', r.saat],
      ['Vefat Edenin İkamet Ettiği İlçe', r.ilce],
      ['Vefat Eden', `${r.vefatEdenAd} ${r.vefatEdenSoyad}`],
      ['Randevuyu Alan', `${r.basvuranAd} ${r.basvuranSoyad}`],
    ];
    const govde = $('ozet');
    govde.replaceChildren();
    for (const [baslik, deger, sinif] of satirlar) {
      const tr = document.createElement('tr');
      if (sinif) tr.className = sinif;
      const th = document.createElement('th');
      th.textContent = baslik;
      const td = document.createElement('td');
      if (deger === null) {
        const etiket = document.createElement('span');
        etiket.className = 'durum-etiket ' + durumSinif;
        etiket.textContent = durumYazi;
        td.append(etiket);
      } else {
        td.textContent = deger;
      }
      tr.append(th, td);
      govde.append(tr);
    }
    $('iptalEt').hidden = !r.iptalEdilebilir;
    $('sorguForm').hidden = true;
    $('sonuc').hidden = false;
  }

  $('sorguForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    mesaj('genelHata', '');
    mesaj('bilgi', '');
    const randevuNo = $('randevuNo').value.trim().toUpperCase();
    const basvuranTc = $('basvuranTc').value.trim();

    alanHatasi('randevuNo', /^[A-Z0-9]{8}$/.test(randevuNo) ? '' : 'Randevu numarası 8 karakterden oluşmalıdır.');
    alanHatasi('basvuranTc', tcGecerli(basvuranTc) ? '' : 'Geçerli bir T.C. Kimlik Numarası giriniz.');
    if (document.querySelector('#sorguForm .hatali')) return;

    const btn = $('sorgula');
    btn.disabled = true;
    try {
      const veri = await api('/api/randevu-sorgula', {
        method: 'POST',
        body: JSON.stringify({ randevuNo, basvuranTc }),
      });
      kimlik = { randevuNo, basvuranTc };
      sonucGoster(veri.randevu);
    } catch (err) {
      mesaj('genelHata', err.message);
    } finally {
      btn.disabled = false;
    }
  });

  $('iptalEt').addEventListener('click', async () => {
    if (!kimlik) return;
    if (!confirm('Randevunuz iptal edilecektir. Bu işlem geri alınamaz. Emin misiniz?')) return;
    mesaj('genelHata', '');
    const btn = $('iptalEt');
    btn.disabled = true;
    try {
      const veri = await api('/api/randevu-iptal', { method: 'POST', body: JSON.stringify(kimlik) });
      sonucGoster(veri.randevu);
      mesaj('bilgi', 'Randevunuz iptal edilmiştir.');
    } catch (err) {
      mesaj('genelHata', err.message);
    } finally {
      btn.disabled = false;
    }
  });

  $('yeniSorgu').addEventListener('click', () => {
    kimlik = null;
    $('sorguForm').reset();
    mesaj('genelHata', '');
    mesaj('bilgi', '');
    $('sonuc').hidden = true;
    $('sorguForm').hidden = false;
    $('randevuNo').focus();
  });
})();
