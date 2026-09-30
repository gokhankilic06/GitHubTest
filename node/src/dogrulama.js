const ISIM_DESENI = /^[A-Za-zÇĞİÖŞÜçğıöşüÂâÎîÛû .'-]{2,50}$/u;

function tcGecerliMi(tc) {
  if (typeof tc !== 'string' || !/^[1-9][0-9]{10}$/.test(tc)) return false;
  const d = tc.split('').map(Number);
  const tek = d[0] + d[2] + d[4] + d[6] + d[8];
  const cift = d[1] + d[3] + d[5] + d[7];
  if ((((tek * 7 - cift) % 10) + 10) % 10 !== d[9]) return false;
  return d.slice(0, 10).reduce((a, b) => a + b, 0) % 10 === d[10];
}

function metin(deger) {
  return typeof deger === 'string' ? deger.trim().replace(/\s+/g, ' ') : '';
}

// Formdan gelen randevu verisini doğrular ve normalleştirir.
// Dönüş: { veri, hatalar } — hatalar boşsa veri kaydedilmeye hazırdır.
function randevuDogrula(govde, { ilceler, buYil }) {
  const g = govde && typeof govde === 'object' ? govde : {};
  const hatalar = {};
  const veri = {};

  if (g.taahhut !== true) {
    hatalar.taahhut = 'Açıklamaları okuduğunuzu onaylamanız gerekmektedir.';
  }

  veri.ilce = metin(g.ilce);
  if (!ilceler.includes(veri.ilce)) hatalar.ilce = 'Lütfen geçerli bir ilçe seçiniz.';

  veri.tarih = metin(g.tarih);
  veri.saat = metin(g.saat);

  for (const [alan, ad] of [['vefatEdenAd', 'Ad'], ['vefatEdenSoyad', 'Soyad'],
    ['basvuranAd', 'Ad'], ['basvuranSoyad', 'Soyad']]) {
    const deger = metin(g[alan]);
    if (!deger) hatalar[alan] = `${ad} alanı zorunludur.`;
    else if (!ISIM_DESENI.test(deger)) hatalar[alan] = `${ad} yalnızca harflerden oluşmalı (2-50 karakter).`;
    veri[alan] = deger.toLocaleUpperCase('tr-TR');
  }

  veri.vefatEdenTc = metin(g.vefatEdenTc);
  veri.basvuranTc = metin(g.basvuranTc);
  if (!tcGecerliMi(veri.vefatEdenTc)) hatalar.vefatEdenTc = 'Geçerli bir T.C. Kimlik Numarası giriniz.';
  if (!tcGecerliMi(veri.basvuranTc)) hatalar.basvuranTc = 'Geçerli bir T.C. Kimlik Numarası giriniz.';
  else if (veri.basvuranTc === veri.vefatEdenTc) {
    hatalar.basvuranTc = 'Randevuyu alanın T.C. numarası vefat edenle aynı olamaz.';
  }

  const yil = typeof g.basvuranDogumYili === 'string' ? Number(g.basvuranDogumYili.trim()) : g.basvuranDogumYili;
  if (!Number.isInteger(yil) || yil < 1900 || yil > buYil) {
    hatalar.basvuranDogumYili = 'Geçerli bir doğum yılı giriniz (örn: 1980).';
  }
  veri.basvuranDogumYili = yil;

  veri.basvuranCep = metin(g.basvuranCep).replace(/\D/g, '').replace(/^0(?=5\d{9}$)/, '');
  if (!/^5\d{9}$/.test(veri.basvuranCep)) {
    hatalar.basvuranCep = 'Cep numarasını başında 0 olmadan 5xxxxxxxxx şeklinde giriniz.';
  }

  return { veri, hatalar };
}

module.exports = { tcGecerliMi, randevuDogrula };
