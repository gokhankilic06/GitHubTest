const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const express = require('express');
const { Depo } = require('./depo');
const takvim = require('./takvim');
const { randevuDogrula } = require('./dogrulama');

const KOK = path.resolve(__dirname, '..', '..');
const WEB = path.join(KOK, 'web');
const config = JSON.parse(fs.readFileSync(path.join(KOK, 'config.json'), 'utf8'));

// SIMDI ortam değişkeni (ISO tarih) test amaçlı sabit bir "şu an" vermeyi sağlar.
function simdi() {
  return process.env.SIMDI ? new Date(process.env.SIMDI) : new Date();
}

function esitMi(a, b) {
  const x = Buffer.from(a);
  const y = Buffer.from(b);
  return x.length === y.length && crypto.timingSafeEqual(x, y);
}

// Personel paneli için HTTP Basic kimlik doğrulaması.
function yoneticiDogrula(req, res, next) {
  const sifre = process.env.ADMIN_PASSWORD;
  if (!sifre) {
    return res.status(503).json({ hata: 'Yönetim paneli kapalı: ADMIN_PASSWORD ortam değişkeni tanımlanmamış.' });
  }
  const kullanici = process.env.ADMIN_USER || 'yonetici';
  const [tip, deger] = (req.get('authorization') || '').split(' ');
  if (tip === 'Basic' && deger) {
    const cozulen = Buffer.from(deger, 'base64').toString('utf8');
    const ayrac = cozulen.indexOf(':');
    if (ayrac >= 0 && esitMi(cozulen.slice(0, ayrac), kullanici) && esitMi(cozulen.slice(ayrac + 1), sifre)) {
      return next();
    }
  }
  res.set('WWW-Authenticate', 'Basic realm="Randevu Yonetimi", charset="UTF-8"');
  return res.status(401).json({ hata: 'Yetkisiz erişim.' });
}

function uygulamaOlustur(depo) {
  const app = express();
  app.disable('x-powered-by');
  app.use(express.json({ limit: '20kb' }));
  app.use((req, res, next) => {
    res.set('X-Content-Type-Options', 'nosniff');
    next();
  });

  app.get('/api/ayarlar', (req, res) => {
    const belgeYolu = path.join(WEB, 'public', 'belgeler', config.belge.dosya);
    res.json({
      vergiDairesi: config.vergiDairesi,
      ilceler: config.ilceler,
      yeniGunAcilisSaati: config.yeniGunAcilisSaati,
      belge: {
        ad: config.belge.ad,
        url: `/belgeler/${encodeURIComponent(config.belge.dosya)}`,
        mevcut: fs.existsSync(belgeYolu),
      },
    });
  });

  app.get('/api/gunler', (req, res) => {
    const { bugun, ilk, son } = takvim.pencere(config, simdi());
    const doluluk = depo.gunlukDoluluk(ilk, son);
    const gunler = {};
    for (let t = ilk; t <= son; t = takvim.gunEkle(t, 1)) {
      const kapasite = takvim.gunSaatleri(config, t).length * config.slotKapasitesi;
      if (kapasite === 0) gunler[t] = 'kapali';
      else gunler[t] = (doluluk[t] || 0) >= kapasite ? 'dolu' : 'uygun';
    }
    res.json({ bugun, ilk, son, gunler });
  });

  app.get('/api/saatler', (req, res) => {
    const tarih = String(req.query.tarih || '');
    const { ilk, son } = takvim.pencere(config, simdi());
    if (!takvim.gecerliTarihMi(tarih) || tarih < ilk || tarih > son || !takvim.isGunuMu(config, tarih)) {
      return res.status(400).json({ hata: 'Seçilen gün için randevu alınamaz.' });
    }
    const doluluk = depo.saatlikDoluluk(tarih);
    const saatler = takvim.gunSaatleri(config, tarih).map((saat) => ({
      saat,
      uygun: (doluluk[saat] || 0) < config.slotKapasitesi,
    }));
    res.json({ tarih, saatler });
  });

  app.post('/api/randevular', (req, res) => {
    const an = simdi();
    const { bugun, ilk, son } = takvim.pencere(config, an);
    const { veri, hatalar } = randevuDogrula(req.body, { ilceler: config.ilceler, buYil: Number(bugun.slice(0, 4)) });

    if (!takvim.gecerliTarihMi(veri.tarih) || veri.tarih < ilk || veri.tarih > son || !takvim.isGunuMu(config, veri.tarih)) {
      hatalar.tarih = 'Seçilen gün için randevu alınamaz.';
    } else if (!takvim.gunSaatleri(config, veri.tarih).includes(veri.saat)) {
      hatalar.saat = 'Geçerli bir randevu saati seçiniz.';
    }

    if (Object.keys(hatalar).length) {
      return res.status(400).json({ hata: 'Lütfen hatalı alanları düzeltiniz.', hatalar });
    }

    const sonuc = depo.ekle(veri, { bugun, kapasite: config.slotKapasitesi });
    if (sonuc.cakisma) {
      return res.status(409).json({ hata: sonuc.cakisma.mesaj, hatalar: { [sonuc.cakisma.alan]: sonuc.cakisma.mesaj } });
    }
    return res.status(201).json({ randevu: sonuc.randevu });
  });

  app.get('/yonetim', yoneticiDogrula, (req, res) => {
    res.sendFile(path.join(WEB, 'yonetim.html'));
  });

  app.get('/api/yonetim/randevular', yoneticiDogrula, (req, res) => {
    const tarih = req.query.tarih ? String(req.query.tarih) : '';
    if (tarih && !takvim.gecerliTarihMi(tarih)) return res.status(400).json({ hata: 'Geçersiz tarih.' });
    const { bugun } = takvim.pencere(config, simdi());
    res.json({ randevular: depo.listele({ tarih, bugun }) });
  });

  app.post('/api/yonetim/randevular/:id/iptal', yoneticiDogrula, (req, res) => {
    const id = Number(req.params.id);
    if (!Number.isInteger(id) || !depo.iptalEt(id)) {
      return res.status(404).json({ hata: 'Aktif randevu bulunamadı.' });
    }
    res.json({ tamam: true });
  });

  app.use('/api', (req, res) => res.status(404).json({ hata: 'Bulunamadı.' }));
  app.use(express.static(path.join(WEB, 'public')));

  // Beklenmeyen hatalarda iç ayrıntıları kullanıcıya göstermeden JSON döner.
  app.use((err, req, res, next) => { // eslint-disable-line no-unused-vars
    if (err.type === 'entity.parse.failed') return res.status(400).json({ hata: 'Geçersiz istek.' });
    console.error(err);
    res.status(500).json({ hata: 'Sunucu hatası. Lütfen daha sonra tekrar deneyiniz.' });
  });

  return app;
}

if (require.main === module) {
  const depo = new Depo(process.env.DB_PATH || path.join(KOK, 'data', 'randevu.db'), path.join(KOK, 'schema.sql'));
  const port = Number(process.env.PORT) || 3000;
  uygulamaOlustur(depo).listen(port, () => {
    console.log(`Veraset randevu sistemi (Node.js) http://localhost:${port} adresinde çalışıyor.`);
  });
}

module.exports = { uygulamaOlustur };
