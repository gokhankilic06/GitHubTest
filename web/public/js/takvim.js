// Uygun günleri yeşil, diğerlerini gri gösteren basit takvim bileşeni.
window.Takvim = function (kok, { gunler, ilk, son, secildi }) {
  const AYLAR = ['Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran', 'Temmuz',
    'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık'];
  const GUNLER = ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz'];

  const ilkAy = ilk.slice(0, 7);
  const sonAy = son.slice(0, 7);
  let gosterilenAy = ilkAy; // "YYYY-MM"
  let secili = null;

  const iki = (n) => String(n).padStart(2, '0');

  function ayKaydir(ay, fark) {
    let [y, m] = ay.split('-').map(Number);
    m += fark;
    while (m < 1) { m += 12; y--; }
    while (m > 12) { m -= 12; y++; }
    return `${y}-${iki(m)}`;
  }

  function ciz() {
    const [y, m] = gosterilenAy.split('-').map(Number);
    const gunSayisi = new Date(Date.UTC(y, m, 0)).getUTCDate();
    const basBosluk = (new Date(Date.UTC(y, m - 1, 1)).getUTCDay() + 6) % 7; // Pazartesi başlangıç

    kok.replaceChildren();

    const ust = document.createElement('div');
    ust.className = 'takvim-ust';
    const onceki = document.createElement('button');
    onceki.type = 'button';
    onceki.textContent = '‹';
    onceki.setAttribute('aria-label', 'Önceki ay');
    onceki.disabled = gosterilenAy <= ilkAy;
    onceki.onclick = () => { gosterilenAy = ayKaydir(gosterilenAy, -1); ciz(); };
    const sonraki = document.createElement('button');
    sonraki.type = 'button';
    sonraki.textContent = '›';
    sonraki.setAttribute('aria-label', 'Sonraki ay');
    sonraki.disabled = gosterilenAy >= sonAy;
    sonraki.onclick = () => { gosterilenAy = ayKaydir(gosterilenAy, 1); ciz(); };
    const baslik = document.createElement('span');
    baslik.textContent = `${AYLAR[m - 1]} ${y}`;
    ust.append(onceki, baslik, sonraki);

    const izgara = document.createElement('div');
    izgara.className = 'takvim-izgara';
    for (const g of GUNLER) {
      const e = document.createElement('div');
      e.className = 'gun-adi';
      e.textContent = g;
      izgara.append(e);
    }
    for (let i = 0; i < basBosluk; i++) {
      const e = document.createElement('div');
      e.className = 'bos';
      izgara.append(e);
    }
    for (let g = 1; g <= gunSayisi; g++) {
      const tarih = `${y}-${iki(m)}-${iki(g)}`;
      const b = document.createElement('button');
      b.type = 'button';
      b.textContent = g;
      const durum = gunler[tarih];
      if (durum === 'uygun') {
        b.className = 'uygun';
        b.title = 'Randevu için uygun';
        b.onclick = () => { secili = tarih; ciz(); secildi(tarih); };
      } else {
        b.disabled = true;
        b.title = durum === 'dolu' ? 'Bu günün tüm randevuları dolu' : 'Randevu alınamaz';
      }
      if (tarih === secili) b.classList.add('secili');
      izgara.append(b);
    }

    kok.append(ust, izgara);
  }

  ciz();

  return {
    sifirla() { secili = null; gosterilenAy = ilkAy; ciz(); },
  };
};
