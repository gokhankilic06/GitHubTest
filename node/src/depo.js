const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const { DatabaseSync } = require('node:sqlite');

const NO_KARAKTERLERI = 'ABCDEFGHJKLMNPRSTUVYZ23456789';

function randevuNoUret() {
  let no = '';
  for (let i = 0; i < 8; i++) no += NO_KARAKTERLERI[crypto.randomInt(NO_KARAKTERLERI.length)];
  return no;
}

function satirdanRandevu(s) {
  return {
    id: s.id,
    randevuNo: s.randevu_no,
    ilce: s.ilce,
    tarih: s.tarih,
    saat: s.saat,
    vefatEdenAd: s.vefat_eden_ad,
    vefatEdenSoyad: s.vefat_eden_soyad,
    vefatEdenTc: s.vefat_eden_tc,
    basvuranAd: s.basvuran_ad,
    basvuranSoyad: s.basvuran_soyad,
    basvuranDogumYili: s.basvuran_dogum_yili,
    basvuranTc: s.basvuran_tc,
    basvuranCep: s.basvuran_cep,
    durum: s.durum,
    olusturma: s.olusturma,
  };
}

class Depo {
  constructor(dbYolu, semaYolu) {
    if (dbYolu !== ':memory:') fs.mkdirSync(path.dirname(dbYolu), { recursive: true });
    this.db = new DatabaseSync(dbYolu);
    this.db.exec('PRAGMA journal_mode = WAL; PRAGMA busy_timeout = 5000;');
    this.db.exec(fs.readFileSync(semaYolu, 'utf8'));
  }

  // tarih -> aktif randevu sayısı
  gunlukDoluluk(ilk, son) {
    const satirlar = this.db.prepare(
      "SELECT tarih, COUNT(*) AS adet FROM randevular WHERE durum = 'aktif' AND tarih BETWEEN ? AND ? GROUP BY tarih",
    ).all(ilk, son);
    return Object.fromEntries(satirlar.map((s) => [s.tarih, s.adet]));
  }

  // saat -> aktif randevu sayısı
  saatlikDoluluk(tarih) {
    const satirlar = this.db.prepare(
      "SELECT saat, COUNT(*) AS adet FROM randevular WHERE durum = 'aktif' AND tarih = ? GROUP BY saat",
    ).all(tarih);
    return Object.fromEntries(satirlar.map((s) => [s.saat, s.adet]));
  }

  // Kapasite ve mükerrer kontrolünü tek bir yazma işlemi içinde yapıp randevuyu ekler.
  // Dönüş: { randevu } veya { cakisma: { alan, mesaj } }
  ekle(veri, { bugun, kapasite }) {
    this.db.exec('BEGIN IMMEDIATE');
    try {
      const mevcut = this.db.prepare(
        "SELECT tarih, saat FROM randevular WHERE durum = 'aktif' AND vefat_eden_tc = ? AND tarih >= ?",
      ).get(veri.vefatEdenTc, bugun);
      if (mevcut) {
        this.db.exec('ROLLBACK');
        const [y, a, g] = mevcut.tarih.split('-');
        return {
          cakisma: {
            alan: 'vefatEdenTc',
            mesaj: `Bu vefat eden kişi için ${g}.${a}.${y} ${mevcut.saat} tarihli aktif bir randevu zaten bulunmaktadır.`,
          },
        };
      }

      const { adet } = this.db.prepare(
        "SELECT COUNT(*) AS adet FROM randevular WHERE durum = 'aktif' AND tarih = ? AND saat = ?",
      ).get(veri.tarih, veri.saat);
      if (adet >= kapasite) {
        this.db.exec('ROLLBACK');
        return { cakisma: { alan: 'saat', mesaj: 'Seçtiğiniz saat başka bir kullanıcı tarafından alındı. Lütfen başka bir saat seçiniz.' } };
      }

      const ekle = this.db.prepare(`
        INSERT INTO randevular (randevu_no, ilce, tarih, saat, vefat_eden_ad, vefat_eden_soyad, vefat_eden_tc,
          basvuran_ad, basvuran_soyad, basvuran_dogum_yili, basvuran_tc, basvuran_cep)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`);
      let id;
      for (let deneme = 0; ; deneme++) {
        try {
          id = ekle.run(randevuNoUret(), veri.ilce, veri.tarih, veri.saat, veri.vefatEdenAd, veri.vefatEdenSoyad,
            veri.vefatEdenTc, veri.basvuranAd, veri.basvuranSoyad, veri.basvuranDogumYili, veri.basvuranTc,
            veri.basvuranCep).lastInsertRowid;
          break;
        } catch (e) {
          // Randevu numarası çakışması (çok düşük ihtimal) — yeni numarayla tekrar dene.
          if (deneme >= 5 || !/UNIQUE/.test(e.message)) throw e;
        }
      }
      this.db.exec('COMMIT');
      return { randevu: this.getir(Number(id)) };
    } catch (e) {
      if (this.db.isTransaction) this.db.exec('ROLLBACK');
      throw e;
    }
  }

  getir(id) {
    const s = this.db.prepare('SELECT * FROM randevular WHERE id = ?').get(id);
    return s ? satirdanRandevu(s) : null;
  }

  numaraIleGetir(randevuNo) {
    const s = this.db.prepare('SELECT * FROM randevular WHERE randevu_no = ?').get(randevuNo);
    return s ? satirdanRandevu(s) : null;
  }

  // tarih verilirse o günün tüm kayıtları, verilmezse bugünden itibaren tüm kayıtlar
  listele({ tarih, bugun }) {
    const satirlar = tarih
      ? this.db.prepare('SELECT * FROM randevular WHERE tarih = ? ORDER BY saat, id').all(tarih)
      : this.db.prepare('SELECT * FROM randevular WHERE tarih >= ? ORDER BY tarih, saat, id').all(bugun);
    return satirlar.map(satirdanRandevu);
  }

  iptalEt(id) {
    const sonuc = this.db.prepare("UPDATE randevular SET durum = 'iptal' WHERE id = ? AND durum = 'aktif'").run(id);
    return sonuc.changes > 0;
  }

  kapat() {
    this.db.close();
  }
}

module.exports = { Depo };
