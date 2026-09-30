// Sayfalar arasında paylaşılan yardımcılar.
window.Ortak = (function () {
  function tarihGoster(iso) {
    const [y, a, g] = iso.split('-');
    return `${g}.${a}.${y}`;
  }

  // T.C. Kimlik numarası algoritma kontrolü (sunucudaki kontrolle aynıdır).
  function tcGecerli(tc) {
    if (!/^[1-9][0-9]{10}$/.test(tc)) return false;
    const d = tc.split('').map(Number);
    const tek = d[0] + d[2] + d[4] + d[6] + d[8];
    const cift = d[1] + d[3] + d[5] + d[7];
    const onuncu = (((tek * 7 - cift) % 10) + 10) % 10;
    if (onuncu !== d[9]) return false;
    const toplam = d.slice(0, 10).reduce((a, b) => a + b, 0);
    return toplam % 10 === d[10];
  }

  async function api(yol, secenekler = {}) {
    const yanit = await fetch(yol, {
      headers: { 'Content-Type': 'application/json' },
      ...secenekler,
    });
    let veri = null;
    try { veri = await yanit.json(); } catch { /* gövde JSON değil */ }
    if (!yanit.ok) {
      const hata = new Error((veri && veri.hata) || 'Beklenmeyen bir hata oluştu. Lütfen tekrar deneyiniz.');
      hata.durum = yanit.status;
      hata.hatalar = (veri && veri.hatalar) || {};
      throw hata;
    }
    return veri;
  }

  return { tarihGoster, tcGecerli, api };
})();
