// Üç backend (node, python, csharp) için ortak API sözleşme testi.
// Kullanım: BACKEND=node|python|csharp node --test test/api.test.mjs
import { test, before, after } from 'node:test';
import assert from 'node:assert/strict';
import { spawn } from 'node:child_process';
import { existsSync, mkdtempSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const KOK = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const BACKEND = process.env.BACKEND || 'node';
const PORT = Number(process.env.TEST_PORT) || 3900 + Math.floor(Math.random() * 90);
const TABAN = `http://127.0.0.1:${PORT}`;
const YETKI = 'Basic ' + Buffer.from('yonetici:test-sifre').toString('base64');

const KOMUTLAR = {
  node: ['node', ['--no-warnings=ExperimentalWarning', 'node/src/server.js']],
  python: [existsSync(path.join(KOK, 'python/.venv/bin/python')) ? 'python/.venv/bin/python' : 'python3', ['python/app.py']],
  csharp: ['dotnet', ['run', '--project', 'csharp', '--no-launch-profile']],
};

let surec;
let geciciDizin;
let cikti = '';

// Algoritmaya uygun T.C. Kimlik numarası üretir.
function tcUret(tohum) {
  const d = String(100000000 + tohum).slice(0, 9).split('').map(Number);
  d[0] = d[0] || 1;
  const tek = d[0] + d[2] + d[4] + d[6] + d[8];
  const cift = d[1] + d[3] + d[5] + d[7];
  d.push((((tek * 7 - cift) % 10) + 10) % 10);
  d.push(d.reduce((a, b) => a + b, 0) % 10);
  return d.join('');
}

async function istek(yol, { yontem = 'GET', govde, yetki } = {}) {
  const basliklar = { 'Content-Type': 'application/json' };
  if (yetki) basliklar.Authorization = yetki;
  const yanit = await fetch(TABAN + yol, {
    method: yontem,
    headers: basliklar,
    body: govde === undefined ? undefined : JSON.stringify(govde),
  });
  const metin = await yanit.text();
  let json = null;
  try { json = JSON.parse(metin); } catch { /* JSON değil */ }
  return { durum: yanit.status, json, metin, basliklar: yanit.headers };
}

function randevu(ek = {}) {
  return {
    taahhut: true,
    ilce: 'Çankaya',
    tarih: '2026-10-22',
    saat: '09:00',
    vefatEdenAd: 'ahmet',
    vefatEdenSoyad: 'yılmaz',
    vefatEdenTc: tcUret(11111111),
    basvuranAd: 'ayşe  fatma',
    basvuranSoyad: 'şimşek',
    basvuranDogumYili: 1980,
    basvuranTc: tcUret(22222222),
    basvuranCep: '5321234567',
    ...ek,
  };
}

before(async () => {
  geciciDizin = mkdtempSync(path.join(tmpdir(), 'randevu-test-'));
  const [komut, argumanlar] = KOMUTLAR[BACKEND];
  surec = spawn(komut, argumanlar, {
    cwd: KOK,
    env: {
      ...process.env,
      PORT: String(PORT),
      DB_PATH: path.join(geciciDizin, 'test.db'),
      // 20 Ekim 2026 Salı, saat 18:00 (İstanbul) — yeni gün açılmış durumda.
      SIMDI: '2026-10-20T18:00:00+03:00',
      ADMIN_USER: 'yonetici',
      ADMIN_PASSWORD: 'test-sifre',
      ASPNETCORE_URLS: `http://127.0.0.1:${PORT}`,
    },
    stdio: ['ignore', 'pipe', 'pipe'],
  });
  surec.stdout.on('data', (d) => { cikti += d; });
  surec.stderr.on('data', (d) => { cikti += d; });

  const sonTarih = Date.now() + 180_000;
  while (Date.now() < sonTarih) {
    if (surec.exitCode !== null) throw new Error(`Sunucu kapandı:\n${cikti}`);
    try {
      const y = await fetch(TABAN + '/api/ayarlar');
      if (y.ok) return;
    } catch { /* henüz hazır değil */ }
    await new Promise((r) => setTimeout(r, 300));
  }
  throw new Error(`Sunucu zamanında başlamadı:\n${cikti}`);
});

after(() => {
  if (surec) surec.kill('SIGTERM');
  if (geciciDizin) rmSync(geciciDizin, { recursive: true, force: true });
});

test('ayarlar: vergi dairesi, 25 ilçe ve belge bilgisi döner', async () => {
  const { durum, json } = await istek('/api/ayarlar');
  assert.equal(durum, 200);
  assert.equal(json.vergiDairesi, 'Ankara Veraset ve Harçlar Vergi Dairesi');
  assert.equal(json.ilceler.length, 25);
  assert.ok(json.ilceler.includes('Keçiören'));
  assert.equal(json.yeniGunAcilisSaati, '17:00');
  assert.equal(json.belge.url, '/belgeler/veraset-beyannamesi.pdf');
  assert.equal(typeof json.belge.mevcut, 'boolean');
});

test('günler: pencere yarından başlar, 17:00 sonrası 14. gün açılır; hafta sonu ve tatil kapalı', async () => {
  const { durum, json } = await istek('/api/gunler');
  assert.equal(durum, 200);
  assert.equal(json.bugun, '2026-10-20');
  assert.equal(json.ilk, '2026-10-21');
  assert.equal(json.son, '2026-11-03');
  assert.equal(Object.keys(json.gunler).length, 14);
  assert.equal(json.gunler['2026-10-20'], undefined);
  assert.equal(json.gunler['2026-10-21'], 'uygun');
  assert.equal(json.gunler['2026-10-24'], 'kapali'); // Cumartesi
  assert.equal(json.gunler['2026-10-25'], 'kapali'); // Pazar
  assert.equal(json.gunler['2026-10-28'], 'uygun'); // yarım gün
  assert.equal(json.gunler['2026-10-29'], 'kapali'); // Cumhuriyet Bayramı
  assert.equal(json.gunler['2026-11-03'], 'uygun');
});

test('saatler: tam gün 24, yarım gün 12 saat; kapalı gün 400', async () => {
  const tam = await istek('/api/saatler?tarih=2026-10-22');
  assert.equal(tam.durum, 200);
  assert.equal(tam.json.saatler.length, 24);
  assert.deepEqual(tam.json.saatler[0], { saat: '09:00', uygun: true });
  assert.equal(tam.json.saatler[11].saat, '11:45');
  assert.equal(tam.json.saatler[12].saat, '13:30');
  assert.equal(tam.json.saatler[23].saat, '16:15');

  const yarim = await istek('/api/saatler?tarih=2026-10-28');
  assert.equal(yarim.json.saatler.length, 12);
  assert.equal(yarim.json.saatler.at(-1).saat, '11:45');

  for (const t of ['2026-10-24', '2026-10-29', '2026-10-20', '2026-11-04', '2026-13-01', 'abc', '']) {
    const r = await istek('/api/saatler?tarih=' + t);
    assert.equal(r.durum, 400, t);
    assert.ok(r.json.hata);
  }
});

let olusan;

test('randevu kaydı: geçerli veri 201 döner, isimler büyük harfe çevrilir', async () => {
  const { durum, json } = await istek('/api/randevular', { yontem: 'POST', govde: randevu() });
  assert.equal(durum, 201, JSON.stringify(json));
  olusan = json.randevu;
  assert.match(olusan.randevuNo, /^[A-Z0-9]{8}$/);
  assert.equal(olusan.tarih, '2026-10-22');
  assert.equal(olusan.saat, '09:00');
  assert.equal(olusan.ilce, 'Çankaya');
  assert.equal(olusan.vefatEdenAd, 'AHMET');
  assert.equal(olusan.vefatEdenSoyad, 'YILMAZ');
  assert.equal(olusan.basvuranAd, 'AYŞE FATMA');
  assert.equal(olusan.basvuranSoyad, 'ŞİMŞEK');
  assert.equal(olusan.basvuranDogumYili, 1980);
  assert.equal(olusan.durum, 'aktif');
  assert.equal(typeof olusan.id, 'number');
});

test('dolu saat artık uygun görünmez ve ikinci kez alınamaz (409)', async () => {
  const s = await istek('/api/saatler?tarih=2026-10-22');
  assert.deepEqual(s.json.saatler[0], { saat: '09:00', uygun: false });

  const r = await istek('/api/randevular', {
    yontem: 'POST',
    govde: randevu({ vefatEdenTc: tcUret(33333333) }),
  });
  assert.equal(r.durum, 409);
  assert.ok(r.json.hatalar.saat);
});

test('aynı vefat eden için ikinci aktif randevu alınamaz (409)', async () => {
  const r = await istek('/api/randevular', { yontem: 'POST', govde: randevu({ saat: '09:15' }) });
  assert.equal(r.durum, 409);
  assert.ok(r.json.hatalar.vefatEdenTc);
  assert.match(r.json.hata, /22\.10\.2026 09:00/);
});

test('doğrulama hataları alan bazında 400 döner', async () => {
  const r = await istek('/api/randevular', {
    yontem: 'POST',
    govde: randevu({
      taahhut: false,
      ilce: 'İstanbul',
      vefatEdenAd: '',
      vefatEdenSoyad: 'Y1lmaz',
      vefatEdenTc: '12345678901',
      basvuranDogumYili: 1850,
      basvuranCep: '4321234567',
      saat: '12:15',
    }),
  });
  assert.equal(r.durum, 400);
  for (const alan of ['taahhut', 'ilce', 'vefatEdenAd', 'vefatEdenSoyad', 'vefatEdenTc', 'basvuranDogumYili', 'basvuranCep', 'saat']) {
    assert.ok(r.json.hatalar[alan], `${alan} için hata bekleniyordu: ${JSON.stringify(r.json.hatalar)}`);
  }
  assert.equal(r.json.hatalar.basvuranAd, undefined);
});

test('pencere dışı, hafta sonu ve tatil günlerine kayıt yapılamaz', async () => {
  for (const tarih of ['2026-10-20', '2026-10-24', '2026-10-29', '2026-11-04']) {
    const r = await istek('/api/randevular', {
      yontem: 'POST',
      govde: randevu({ tarih, vefatEdenTc: tcUret(44444444) }),
    });
    assert.equal(r.durum, 400, tarih);
    assert.ok(r.json.hatalar.tarih, tarih);
  }
});

test('başvuran ile vefat edenin T.C. numarası aynı olamaz; cep numarası başındaki 0 kabul edilir', async () => {
  const ayni = await istek('/api/randevular', {
    yontem: 'POST',
    govde: randevu({ vefatEdenTc: tcUret(55555555), basvuranTc: tcUret(55555555) }),
  });
  assert.equal(ayni.durum, 400);
  assert.ok(ayni.json.hatalar.basvuranTc);

  const sifirli = await istek('/api/randevular', {
    yontem: 'POST',
    govde: randevu({ vefatEdenTc: tcUret(66666666), saat: '10:00', basvuranCep: '05321234567', basvuranDogumYili: '1975' }),
  });
  assert.equal(sifirli.durum, 201, JSON.stringify(sifirli.json));
  assert.equal(sifirli.json.randevu.basvuranCep, '5321234567');
  assert.equal(sifirli.json.randevu.basvuranDogumYili, 1975);
});

test('geçersiz JSON gövdesi 400 döner', async () => {
  const y = await fetch(TABAN + '/api/randevular', {
    method: 'POST', headers: { 'Content-Type': 'application/json' }, body: '{bozuk',
  });
  assert.equal(y.status, 400);
});

test('yönetim: kimlik doğrulaması olmadan 401', async () => {
  const r = await istek('/api/yonetim/randevular');
  assert.equal(r.durum, 401);
  assert.match(r.basliklar.get('www-authenticate') || '', /Basic/);
  const yanlis = await istek('/api/yonetim/randevular', { yetki: 'Basic ' + Buffer.from('yonetici:yanlis').toString('base64') });
  assert.equal(yanlis.durum, 401);
  const sayfa = await fetch(TABAN + '/yonetim');
  assert.equal(sayfa.status, 401);
  const dogrudan = await fetch(TABAN + '/yonetim.html');
  assert.equal(dogrudan.status, 404);
});

test('yönetim: listeleme, iptal ve iptal sonrası saatin boşalması', async () => {
  const sayfa = await fetch(TABAN + '/yonetim', { headers: { Authorization: YETKI } });
  assert.equal(sayfa.status, 200);
  assert.match(await sayfa.text(), /Randevu Yönetimi/);

  const liste = await istek('/api/yonetim/randevular?tarih=2026-10-22', { yetki: YETKI });
  assert.equal(liste.durum, 200);
  assert.deepEqual(liste.json.randevular.map((r) => r.saat), ['09:00', '10:00']);
  assert.equal(liste.json.randevular[0].vefatEdenTc, tcUret(11111111));

  const tumu = await istek('/api/yonetim/randevular', { yetki: YETKI });
  assert.equal(tumu.json.randevular.length, 2);

  const iptal = await istek(`/api/yonetim/randevular/${olusan.id}/iptal`, { yontem: 'POST', yetki: YETKI });
  assert.equal(iptal.durum, 200);
  const tekrar = await istek(`/api/yonetim/randevular/${olusan.id}/iptal`, { yontem: 'POST', yetki: YETKI });
  assert.equal(tekrar.durum, 404);

  const s = await istek('/api/saatler?tarih=2026-10-22');
  assert.deepEqual(s.json.saatler[0], { saat: '09:00', uygun: true });

  // İptal sonrası aynı vefat eden için yeniden randevu alınabilir.
  const yeni = await istek('/api/randevular', { yontem: 'POST', govde: randevu() });
  assert.equal(yeni.durum, 201);

  const sonra = await istek('/api/yonetim/randevular?tarih=2026-10-22', { yetki: YETKI });
  assert.deepEqual(sonra.json.randevular.map((r) => r.durum), ['iptal', 'aktif', 'aktif']);
});

test('statik dosyalar ve bilinmeyen API yolu', async () => {
  const ana = await fetch(TABAN + '/');
  assert.equal(ana.status, 200);
  assert.match(await ana.text(), /Ankara Defterdarlığı Veraset İşlemleri Randevu/);
  const aciklama = await fetch(TABAN + '/aciklama.html');
  assert.equal(aciklama.status, 200);
  const yok = await istek('/api/yok');
  assert.equal(yok.durum, 404);
});

test('vatandaş: randevu sorgulama ve iptal', async () => {
  const olustur = await istek('/api/randevular', {
    yontem: 'POST',
    govde: randevu({ tarih: '2026-10-23', saat: '10:30', vefatEdenTc: tcUret(77777777), basvuranTc: tcUret(88888888) }),
  });
  assert.equal(olustur.durum, 201);
  const { randevuNo } = olustur.json.randevu;
  const kimlik = { randevuNo, basvuranTc: tcUret(88888888) };

  // Hatalı biçim 400, eşleşmeyen bilgi 404
  assert.equal((await istek('/api/randevu-sorgula', { yontem: 'POST', govde: { randevuNo: 'ABC', basvuranTc: '1' } })).durum, 400);
  assert.equal((await istek('/api/randevu-sorgula', { yontem: 'POST', govde: {} })).durum, 400);
  const yanlisTc = await istek('/api/randevu-sorgula', { yontem: 'POST', govde: { randevuNo, basvuranTc: tcUret(22222222) } });
  assert.equal(yanlisTc.durum, 404);
  const yanlisNo = await istek('/api/randevu-sorgula', { yontem: 'POST', govde: { ...kimlik, randevuNo: 'AAAAAAAA' } });
  assert.equal(yanlisNo.durum, 404);
  assert.equal(yanlisTc.json.hata, yanlisNo.json.hata);

  // Doğru bilgilerle sorgu (randevu numarası küçük harfle de girilebilir)
  const sorgu = await istek('/api/randevu-sorgula', { yontem: 'POST', govde: { ...kimlik, randevuNo: ` ${randevuNo.toLowerCase()} ` } });
  assert.equal(sorgu.durum, 200, JSON.stringify(sorgu.json));
  assert.deepEqual(sorgu.json.randevu, {
    randevuNo,
    vergiDairesi: 'Ankara Veraset ve Harçlar Vergi Dairesi',
    ilce: 'Çankaya',
    tarih: '2026-10-23',
    saat: '10:30',
    vefatEdenAd: 'AHMET',
    vefatEdenSoyad: 'YILMAZ',
    basvuranAd: 'AYŞE FATMA',
    basvuranSoyad: 'ŞİMŞEK',
    durum: 'aktif',
    iptalEdilebilir: true,
  });

  // Yanlış bilgiyle iptal edilemez
  assert.equal((await istek('/api/randevu-iptal', { yontem: 'POST', govde: { randevuNo, basvuranTc: tcUret(22222222) } })).durum, 404);

  const iptal = await istek('/api/randevu-iptal', { yontem: 'POST', govde: kimlik });
  assert.equal(iptal.durum, 200, JSON.stringify(iptal.json));
  assert.equal(iptal.json.randevu.durum, 'iptal');
  assert.equal(iptal.json.randevu.iptalEdilebilir, false);

  const tekrar = await istek('/api/randevu-iptal', { yontem: 'POST', govde: kimlik });
  assert.equal(tekrar.durum, 409);
  assert.ok(tekrar.json.hata);

  const sonra = await istek('/api/randevu-sorgula', { yontem: 'POST', govde: kimlik });
  assert.equal(sonra.json.randevu.durum, 'iptal');

  // Saat boşa çıkar
  const saatler = await istek('/api/saatler?tarih=2026-10-23');
  assert.deepEqual(saatler.json.saatler.find((s) => s.saat === '10:30'), { saat: '10:30', uygun: true });
});

test('sorgulama sayfası sunulur', async () => {
  const y = await fetch(TABAN + '/sorgula.html');
  assert.equal(y.status, 200);
  assert.match(await y.text(), /Randevu Sorgulama/);
});
