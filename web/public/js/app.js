(function () {
  const { api, tarihGoster, tcGecerli } = window.Ortak;
  const $ = (id) => document.getElementById(id);

  const durum = { ayarlar: null, ilce: '', tarih: '', saat: '' };
  let takvim = null;

  // ---------- Genel ----------
  function hataGoster(mesaj) {
    const kutu = $('genelHata');
    kutu.textContent = mesaj;
    kutu.hidden = !mesaj;
    if (mesaj) kutu.scrollIntoView({ behavior: 'smooth', block: 'center' });
  }

  function adimaGit(n) {
    hataGoster('');
    for (let i = 1; i <= 4; i++) $('adim' + i).hidden = i !== n;
    document.querySelectorAll('.adimlar li').forEach((li) => {
      const a = Number(li.dataset.adim);
      li.classList.toggle('aktif', a === n);
      li.classList.toggle('tamam', a < n);
    });
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  function basaDon() {
    durum.ilce = '';
    durum.tarih = '';
    durum.saat = '';
    $('taahhut').checked = false;
    $('ilce').value = '';
    $('tarihGoster').value = '';
    $('randevuForm').reset();
    formHatalariniTemizle();
    takvimYukle();
    adimaGit(1);
  }

  document.querySelectorAll('[data-iptal]').forEach((b) =>
    b.addEventListener('click', () => {
      if (confirm('Randevu işlemini iptal etmek istediğinize emin misiniz?')) basaDon();
    }));

  // ---------- 1. adım ----------
  async function baslat() {
    try {
      const ayarlar = await api('/api/ayarlar');
      durum.ayarlar = ayarlar;
      $('vergiDairesiBaslik').textContent = ayarlar.vergiDairesi;
      $('acilisSaati').textContent = ayarlar.yeniGunAcilisSaati;
      for (const ilce of ayarlar.ilceler) {
        const o = document.createElement('option');
        o.value = ilce;
        o.textContent = ilce;
        $('ilce').append(o);
      }
      if (ayarlar.belge && ayarlar.belge.mevcut) {
        $('belgeLink').href = ayarlar.belge.url;
        $('belgeLink').textContent = `${ayarlar.belge.ad} - İndir`;
        $('belgeKutu').hidden = false;
      }
    } catch (e) {
      hataGoster(e.message);
    }

    try {
      const yanit = await fetch('/aciklama.html');
      $('aciklama').innerHTML = yanit.ok ? await yanit.text() : '';
    } catch {
      $('aciklama').textContent = '';
    }

    await takvimYukle();
  }

  async function takvimYukle() {
    try {
      const veri = await api('/api/gunler');
      takvim = window.Takvim($('takvim'), {
        gunler: veri.gunler,
        ilk: veri.ilk,
        son: veri.son,
        secildi(tarih) {
          durum.tarih = tarih;
          $('tarihGoster').value = tarihGoster(tarih);
          $('takvim').hidden = true;
        },
      });
    } catch (e) {
      hataGoster(e.message);
    }
  }

  function takvimAcKapa(e) {
    e.stopPropagation();
    $('takvim').hidden = !$('takvim').hidden;
  }
  $('takvimAc').addEventListener('click', takvimAcKapa);
  $('tarihGoster').addEventListener('click', takvimAcKapa);
  $('takvim').addEventListener('click', (e) => e.stopPropagation());
  document.addEventListener('click', () => { $('takvim').hidden = true; });

  $('ilerle1').addEventListener('click', async () => {
    if (!$('taahhut').checked) return hataGoster('Devam etmek için açıklamaları okuduğunuzu onaylamanız gerekmektedir.');
    if (!$('ilce').value) return hataGoster('Lütfen vefat edenin ikamet ettiği ilçeyi seçiniz.');
    if (!durum.tarih) return hataGoster('Lütfen takvimden randevu almak istediğiniz günü seçiniz.');
    durum.ilce = $('ilce').value;
    await saatleriYukle();
  });

  // ---------- 2. adım ----------
  async function saatleriYukle() {
    try {
      const veri = await api('/api/saatler?tarih=' + encodeURIComponent(durum.tarih));
      $('secilenGunYazi').textContent = tarihGoster(durum.tarih);
      const kap = $('saatler');
      kap.replaceChildren();
      for (const s of veri.saatler) {
        const b = document.createElement('button');
        b.type = 'button';
        b.textContent = s.saat;
        b.disabled = !s.uygun;
        b.title = s.uygun ? 'Uygun' : 'Dolu';
        b.onclick = () => saatSec(s.saat);
        kap.append(b);
      }
      adimaGit(2);
      if (!veri.saatler.some((s) => s.uygun)) {
        hataGoster('Seçtiğiniz günde boş randevu saati kalmamıştır. Lütfen başka bir gün seçiniz.');
      }
    } catch (e) {
      hataGoster(e.message);
    }
  }

  $('geri2').addEventListener('click', () => adimaGit(1));

  function saatSec(saat) {
    durum.saat = saat;
    $('gosterVd').value = durum.ayarlar ? durum.ayarlar.vergiDairesi : '';
    $('gosterTarih').value = tarihGoster(durum.tarih);
    $('gosterSaat').value = saat;
    adimaGit(3);
    $('vefatEdenAd').focus();
  }

  // ---------- 3. adım ----------
  const ALANLAR = ['vefatEdenAd', 'vefatEdenSoyad', 'vefatEdenTc', 'basvuranAd',
    'basvuranSoyad', 'basvuranDogumYili', 'basvuranTc', 'basvuranCep'];

  // Sayısal alanlara yalnızca rakam girilmesine izin ver.
  ['vefatEdenTc', 'basvuranTc', 'basvuranDogumYili', 'basvuranCep'].forEach((id) =>
    $(id).addEventListener('input', (e) => { e.target.value = e.target.value.replace(/\D/g, ''); }));

  function alanHatasi(id, mesaj) {
    const input = $(id);
    input.classList.toggle('hatali', !!mesaj);
    input.parentElement.querySelector('.alan-hata').textContent = mesaj || '';
  }

  function formHatalariniTemizle() {
    ALANLAR.forEach((id) => alanHatasi(id, ''));
  }

  function istemciDogrula(v) {
    const h = {};
    const isim = /^[A-Za-zÇĞİÖŞÜçğıöşüÂâÎîÛû .'-]{2,50}$/;
    for (const [id, ad] of [['vefatEdenAd', 'Ad'], ['vefatEdenSoyad', 'Soyad'], ['basvuranAd', 'Ad'], ['basvuranSoyad', 'Soyad']]) {
      if (!v[id].trim()) h[id] = `${ad} alanı zorunludur.`;
      else if (!isim.test(v[id].trim())) h[id] = `${ad} yalnızca harflerden oluşmalı (2-50 karakter).`;
    }
    if (!tcGecerli(v.vefatEdenTc)) h.vefatEdenTc = 'Geçerli bir T.C. Kimlik Numarası giriniz.';
    if (!tcGecerli(v.basvuranTc)) h.basvuranTc = 'Geçerli bir T.C. Kimlik Numarası giriniz.';
    else if (v.basvuranTc === v.vefatEdenTc) h.basvuranTc = 'Randevuyu alanın T.C. numarası vefat edenle aynı olamaz.';
    const yil = Number(v.basvuranDogumYili);
    if (!/^\d{4}$/.test(v.basvuranDogumYili) || yil < 1900 || yil > new Date().getFullYear()) {
      h.basvuranDogumYili = 'Geçerli bir doğum yılı giriniz (örn: 1980).';
    }
    if (!/^5\d{9}$/.test(v.basvuranCep)) h.basvuranCep = 'Cep numarasını başında 0 olmadan 5xxxxxxxxx şeklinde giriniz.';
    return h;
  }

  $('randevuForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    formHatalariniTemizle();
    hataGoster('');

    const v = {};
    ALANLAR.forEach((id) => { v[id] = $(id).value.trim(); });
    const hatalar = istemciDogrula(v);
    if (Object.keys(hatalar).length) {
      Object.entries(hatalar).forEach(([id, m]) => alanHatasi(id, m));
      $(Object.keys(hatalar)[0]).focus();
      return;
    }

    const btn = $('kaydet');
    btn.disabled = true;
    try {
      const veri = await api('/api/randevular', {
        method: 'POST',
        body: JSON.stringify({
          ...v,
          basvuranDogumYili: Number(v.basvuranDogumYili),
          ilce: durum.ilce,
          tarih: durum.tarih,
          saat: durum.saat,
          taahhut: true,
        }),
      });
      onayGoster(veri.randevu);
    } catch (err) {
      Object.entries(err.hatalar || {}).forEach(([id, m]) => { if ($(id)) alanHatasi(id, m); });
      hataGoster(err.message);
      if (err.durum === 409 && err.hatalar && err.hatalar.saat) {
        // Saat bu arada başkası tarafından alınmış: saat listesini yenile.
        await saatleriYukle();
        hataGoster(err.message);
      }
    } finally {
      btn.disabled = false;
    }
  });

  // ---------- 4. adım ----------
  function onayGoster(r) {
    const satirlar = [
      ['Randevu Numarası', r.randevuNo, 'no'],
      ['Vergi Dairesi', durum.ayarlar ? durum.ayarlar.vergiDairesi : ''],
      ['Randevu Tarihi', tarihGoster(r.tarih)],
      ['Randevu Saati', r.saat],
      ['Vefat Edenin İkamet Ettiği İlçe', r.ilce],
      ['Vefat Eden', `${r.vefatEdenAd} ${r.vefatEdenSoyad}`],
      ['Randevuyu Alan', `${r.basvuranAd} ${r.basvuranSoyad}`],
      ['Cep Numarası', r.basvuranCep],
    ];
    const govde = $('ozet');
    govde.replaceChildren();
    for (const [baslik, deger, sinif] of satirlar) {
      const tr = document.createElement('tr');
      if (sinif) tr.className = sinif;
      const th = document.createElement('th');
      th.textContent = baslik;
      const td = document.createElement('td');
      td.textContent = deger;
      tr.append(th, td);
      govde.append(tr);
    }
    adimaGit(4);
  }

  $('yeniRandevu').addEventListener('click', basaDon);

  baslat();
})();
