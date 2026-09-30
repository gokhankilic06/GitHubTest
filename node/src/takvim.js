// Tarih/saat kuralları. Tüm tarihler "YYYY-MM-DD", saatler "HH:MM" metinleridir.

const iki = (n) => String(n).padStart(2, '0');

function dakikaya(saat) {
  const [s, d] = saat.split(':').map(Number);
  return s * 60 + d;
}

function saate(dakika) {
  return `${iki(Math.floor(dakika / 60))}:${iki(dakika % 60)}`;
}

// Verilen anın, yapılandırılmış saat dilimindeki tarihini ve günün kaçıncı dakikası olduğunu döner.
function yerelZaman(an, saatDilimi) {
  const parcalar = Object.fromEntries(
    new Intl.DateTimeFormat('en-CA', {
      timeZone: saatDilimi, year: 'numeric', month: '2-digit', day: '2-digit',
      hour: '2-digit', minute: '2-digit', hourCycle: 'h23',
    }).formatToParts(an).map((p) => [p.type, p.value]),
  );
  return {
    tarih: `${parcalar.year}-${parcalar.month}-${parcalar.day}`,
    dakika: Number(parcalar.hour) * 60 + Number(parcalar.minute),
  };
}

function gunEkle(tarih, gun) {
  const [y, a, g] = tarih.split('-').map(Number);
  const d = new Date(Date.UTC(y, a - 1, g + gun));
  return `${d.getUTCFullYear()}-${iki(d.getUTCMonth() + 1)}-${iki(d.getUTCDate())}`;
}

function gecerliTarihMi(tarih) {
  if (typeof tarih !== 'string' || !/^\d{4}-\d{2}-\d{2}$/.test(tarih)) return false;
  return gunEkle(tarih, 0) === tarih;
}

// Randevu alınabilecek ilk ve son gün. Aynı gün için randevu verilmez;
// her gün yeniGunAcilisSaati'nde pencerenin sonuna yeni bir gün eklenir.
function pencere(config, an) {
  const { tarih, dakika } = yerelZaman(an, config.saatDilimi);
  const acildi = dakika >= dakikaya(config.yeniGunAcilisSaati);
  return {
    bugun: tarih,
    ilk: gunEkle(tarih, 1),
    son: gunEkle(tarih, config.ileriGunSayisi - (acildi ? 0 : 1)),
  };
}

// Kurum çalışıyor mu? (hafta sonu ve resmi tatiller hariç)
function isGunuMu(config, tarih) {
  const [y, a, g] = tarih.split('-').map(Number);
  const haftaGunu = new Date(Date.UTC(y, a - 1, g)).getUTCDay();
  return haftaGunu !== 0 && haftaGunu !== 6 && !config.tatiller.includes(tarih);
}

// Verilen iş gününün randevu saatleri. Yarım günlerde yalnızca ilk mesai bloğu kullanılır.
function gunSaatleri(config, tarih) {
  if (!isGunuMu(config, tarih)) return [];
  const bloklar = config.yarimGunler.includes(tarih)
    ? config.mesaiBloklari.slice(0, 1)
    : config.mesaiBloklari;
  const saatler = [];
  for (const b of bloklar) {
    for (let d = dakikaya(b.baslangic); d < dakikaya(b.bitis); d += config.randevuAraligiDakika) {
      saatler.push(saate(d));
    }
  }
  return saatler;
}

module.exports = { yerelZaman, gunEkle, gecerliTarihMi, pencere, isGunuMu, gunSaatleri };
