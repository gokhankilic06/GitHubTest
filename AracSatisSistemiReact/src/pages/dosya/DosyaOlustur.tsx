import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { kayitFormuVerileriniGetir, yeniDosyaOnerileriniGetir } from '../../api/referans';
import { dosyaOlustur } from '../../api/dosya';
import type { DosyaFormVerisi, KayitFormuVerisi } from '../../types';

const PLAKA_DESENI = /^(0[1-9]|[1-7][0-9]|8[01])([A-PR-VYZ]\d{4}|[A-PR-VYZ]{2}\d{3,4}|[A-PR-VYZ]{3}\d{2})$/;
const DIGER = '__DIGER__';

const bosForm: DosyaFormVerisi = {
  vergiDairesiAdi: '',
  vergiDairesiYaziTarihi: '',
  vergiDairesiYaziSayisi: '',
  alacakliVergiDairesi: '',
  vergiKimlikNumarasi: '',
  tcKimlikNumarasi: '',
  adiSoyadiUnvani: '',
  kanuniTemsilci: '',
  plaka: '',
  aracCinsi: '',
  aracMarkasi: '',
  aracTipi: '',
  modelYili: '',
  renk: '',
  ruhsatVar: false,
  anahtarVar: false,
  cekiciVar: false,
  otoparkAdi: '',
  hasarDurumu: '',
  muhammenBedel: '',
  kdvOrani: 1,
  damgaVergisiOrani: 5.69,
  otoparkaGirisTarihi: '',
  komisyonBaskani: '',
  komisyonUye1: '',
  komisyonUye2: '',
  komisyonUye3: '',
  satis1Tarihi: '',
  satis2Tarihi: '',
};

export function DosyaOlustur() {
  const navigate = useNavigate();
  const [veri, setVeri] = useState<KayitFormuVerisi | null>(null);
  const [form, setForm] = useState<DosyaFormVerisi>(bosForm);
  const [sehir, setSehir] = useState('');
  const [aracTipiManuelMi, setAracTipiManuelMi] = useState(false);
  const [renkManuelMi, setRenkManuelMi] = useState(false);
  const [bedelGosterim, setBedelGosterim] = useState('');
  const [plakaUyari, setPlakaUyari] = useState(false);
  const [hata, setHata] = useState<string | null>(null);
  const [kaydediliyor, setKaydediliyor] = useState(false);

  useEffect(() => {
    (async () => {
      const [kayitVerisi, onerilerVerisi] = await Promise.all([
        kayitFormuVerileriniGetir(),
        yeniDosyaOnerileriniGetir(),
      ]);
      setVeri(kayitVerisi);
      setForm((f) => ({
        ...f,
        satis1Tarihi: onerilerVerisi.satis1Onerisi.slice(0, 10),
        satis2Tarihi: onerilerVerisi.satis2Onerisi.slice(0, 10),
        komisyonBaskani: onerilerVerisi.komisyonBaskani || '',
        komisyonUye1: onerilerVerisi.komisyonUye1 || '',
        komisyonUye2: onerilerVerisi.komisyonUye2 || '',
        komisyonUye3: onerilerVerisi.komisyonUye3 || '',
      }));
    })();
  }, []);

  const vergiDaireleriIlde = useMemo(
    () => (veri ? veri.vergiDaireleri.filter((v) => v.sehir === sehir) : []),
    [veri, sehir],
  );

  const modellerMarkada = useMemo(
    () => (veri && form.aracMarkasi ? veri.modelSozlugu[form.aracMarkasi] || [] : []),
    [veri, form.aracMarkasi],
  );

  const teminat = useMemo(() => {
    const bedel = Number(form.muhammenBedel) || 0;
    return bedel > 10000 ? Math.round(bedel * 0.05 * 100) / 100 : 0;
  }, [form.muhammenBedel]);

  const damgaVergisiTutari = useMemo(() => {
    const bedel = Number(form.muhammenBedel) || 0;
    const oran = Number(form.damgaVergisiOrani) || 0;
    return Math.round(((bedel * oran) / 1000) * 100) / 100;
  }, [form.muhammenBedel, form.damgaVergisiOrani]);

  function paraFormatla(deger: number) {
    return deger.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  function bedelDegisti(ham: string) {
    const ilkVirgul = ham.indexOf(',');
    const tamKisim = (ilkVirgul === -1 ? ham : ham.slice(0, ilkVirgul)).replace(/\D/g, '');
    const ondalikKisim = (ilkVirgul === -1 ? '' : ham.slice(ilkVirgul + 1)).replace(/\D/g, '').slice(0, 2);
    const tamSayi = tamKisim === '' ? 0 : parseInt(tamKisim, 10);
    setBedelGosterim(tamSayi.toLocaleString('tr-TR') + (ilkVirgul === -1 ? '' : ',' + ondalikKisim));
    const yeniDeger = ondalikKisim ? `${tamSayi}.${ondalikKisim}` : `${tamSayi}`;
    setForm((f) => ({ ...f, muhammenBedel: Number(yeniDeger) }));
  }

  function plakaDegisti(deger: string) {
    const temiz = deger.toUpperCase().replace(/[^0-9A-PR-VYZ]/g, '');
    setForm((f) => ({ ...f, plaka: temiz }));
    setPlakaUyari(temiz !== '' && !PLAKA_DESENI.test(temiz));
  }

  async function gonder(e: React.FormEvent) {
    e.preventDefault();
    setHata(null);

    if (!PLAKA_DESENI.test(form.plaka)) {
      setPlakaUyari(true);
      setHata('Geçerli bir plaka giriniz.');
      return;
    }

    setKaydediliyor(true);
    try {
      // Boş metin alanları "" yerine null gönderilir: StringLength(MinimumLength) gibi
      // doğrulamalar null'u yok sayar ama boş string'i (uzunluk 0) geçersiz sayar -
      // Razor formundaki ConvertEmptyStringToNull davranışıyla aynı sonucu verir.
      const bosIseNull = (deger: string) => (deger === '' ? null : deger);
      const gonderilecek = {
        ...form,
        vergiDairesiYaziTarihi: form.vergiDairesiYaziTarihi || null,
        vergiDairesiYaziSayisi: bosIseNull(form.vergiDairesiYaziSayisi),
        vergiKimlikNumarasi: bosIseNull(form.vergiKimlikNumarasi),
        tcKimlikNumarasi: bosIseNull(form.tcKimlikNumarasi),
        kanuniTemsilci: bosIseNull(form.kanuniTemsilci),
        aracTipi: bosIseNull(form.aracTipi),
        renk: bosIseNull(form.renk),
        otoparkAdi: bosIseNull(form.otoparkAdi),
        hasarDurumu: bosIseNull(form.hasarDurumu),
        otoparkaGirisTarihi: form.otoparkaGirisTarihi || null,
      };
      const yeniDosya = await dosyaOlustur(gonderilecek as unknown as DosyaFormVerisi);
      navigate(`/dosya/${yeniDosya.id}`);
    } catch (err: any) {
      const mesajlar = err?.response?.data?.errors
        ? Object.values(err.response.data.errors).flat().join(' ')
        : 'Dosya kaydedilemedi, lütfen alanları kontrol edin.';
      setHata(mesajlar as string);
    } finally {
      setKaydediliyor(false);
    }
  }

  if (!veri) return <div className="text-center py-5">Yükleniyor...</div>;

  const yillar: number[] = [];
  for (let yil = veri.maksimumModelYili; yil >= 1940; yil--) yillar.push(yil);

  return (
    <div>
      <div className="d-flex justify-content-between align-items-center flex-wrap gap-2">
        <div>
          <h3>Kayıt İşlemleri</h3>
          <p className="text-muted mb-0">Dosya numarası kayıt tamamlandıktan sonra sistem tarafından otomatik üretilir.</p>
        </div>
      </div>

      {hata && <div className="alert alert-danger mt-3">{hata}</div>}

      <form onSubmit={gonder}>
        <div className="form-section-title">1. İlgili Vergi Dairesi</div>
        <div className="row g-3">
          <div className="col-md-3">
            <label className="form-label">Vergi Dairesi Adı</label>
            <select
              className="form-select"
              value={form.vergiDairesiAdi}
              onChange={(e) => setForm((f) => ({ ...f, vergiDairesiAdi: e.target.value }))}
              required
            >
              <option value="">-- Seçiniz --</option>
              {veri.ankaraVergiDaireleri.map((v) => (
                <option key={v.id} value={v.ad}>{v.ad}</option>
              ))}
            </select>
          </div>
          <div className="col-md-2">
            <label className="form-label">Vergi Dairesi Yazı Tarihi</label>
            <input
              className="form-control"
              type="date"
              value={form.vergiDairesiYaziTarihi}
              onChange={(e) => setForm((f) => ({ ...f, vergiDairesiYaziTarihi: e.target.value }))}
            />
          </div>
          <div className="col-md-2">
            <label className="form-label">Vergi Dairesi Yazı Sayısı</label>
            <input
              className="form-control"
              value={form.vergiDairesiYaziSayisi}
              onChange={(e) => setForm((f) => ({ ...f, vergiDairesiYaziSayisi: e.target.value }))}
            />
          </div>
          <div className="col-md-2">
            <label className="form-label">Alacaklı Vergi Dairesi - İl</label>
            <select className="form-select" value={sehir} onChange={(e) => { setSehir(e.target.value); setForm((f) => ({ ...f, alacakliVergiDairesi: '' })); }}>
              <option value="">-- İl Seçiniz --</option>
              {veri.sehirler.map((s) => (
                <option key={s} value={s}>{s}</option>
              ))}
            </select>
          </div>
          <div className="col-md-3">
            <label className="form-label">Alacaklı Vergi Dairesi</label>
            <select
              className="form-select"
              value={form.alacakliVergiDairesi}
              onChange={(e) => setForm((f) => ({ ...f, alacakliVergiDairesi: e.target.value }))}
              disabled={!sehir}
              required
            >
              <option value="">{sehir ? '-- Vergi Dairesi Seçiniz --' : '-- Önce il seçiniz --'}</option>
              {vergiDaireleriIlde.map((v) => (
                <option key={v.id} value={v.ad}>{v.ad}</option>
              ))}
            </select>
          </div>
        </div>

        <div className="form-section-title">2. Borçlu Mükellef Bilgileri</div>
        <div className="row g-3">
          <div className="col-md-3">
            <label className="form-label">Vergi Kimlik Numarası</label>
            <input className="form-control" maxLength={10} placeholder="10 haneli" value={form.vergiKimlikNumarasi} onChange={(e) => setForm((f) => ({ ...f, vergiKimlikNumarasi: e.target.value.replace(/\D/g, '') }))} />
          </div>
          <div className="col-md-3">
            <label className="form-label">T.C. Kimlik Numarası</label>
            <input className="form-control" maxLength={11} placeholder="11 haneli" value={form.tcKimlikNumarasi} onChange={(e) => setForm((f) => ({ ...f, tcKimlikNumarasi: e.target.value.replace(/\D/g, '') }))} />
          </div>
          <div className="col-md-3">
            <label className="form-label">Adı Soyadı / Unvanı</label>
            <input className="form-control" value={form.adiSoyadiUnvani} onChange={(e) => setForm((f) => ({ ...f, adiSoyadiUnvani: e.target.value }))} required />
          </div>
          <div className="col-md-3">
            <label className="form-label">Kanuni Temsilci / Ortak vb.</label>
            <input className="form-control" value={form.kanuniTemsilci} onChange={(e) => setForm((f) => ({ ...f, kanuniTemsilci: e.target.value }))} />
          </div>
        </div>

        <div className="form-section-title">3. Araç Bilgi Girişi</div>
        <div className="row g-3">
          <div className="col-md-3">
            <label className="form-label">Plaka</label>
            <input
              className={`form-control ${plakaUyari ? 'is-invalid' : ''}`}
              placeholder="ör. 34ABC123"
              autoComplete="off"
              value={form.plaka}
              onChange={(e) => plakaDegisti(e.target.value)}
              onBlur={(e) => plakaDegisti(e.target.value)}
              required
            />
            {plakaUyari && (
              <div className="form-text text-danger">
                Geçerli bir plaka giriniz (ör. 34ABC123; il kodu 01-81, Q/W/X kullanılmaz).
              </div>
            )}
          </div>
          <div className="col-md-3">
            <label className="form-label">Cinsi</label>
            <select className="form-select" value={form.aracCinsi} onChange={(e) => setForm((f) => ({ ...f, aracCinsi: e.target.value }))} required>
              <option value="">-- Seçiniz --</option>
              {veri.cinsler.map((c) => (
                <option key={c.id} value={c.ad}>{c.ad}</option>
              ))}
            </select>
          </div>
          <div className="col-md-3">
            <label className="form-label">Markası</label>
            <select
              className="form-select"
              value={form.aracMarkasi}
              onChange={(e) => { setForm((f) => ({ ...f, aracMarkasi: e.target.value, aracTipi: '' })); setAracTipiManuelMi(false); }}
              required
            >
              <option value="">-- Seçiniz --</option>
              {veri.markalar.map((m) => (
                <option key={m.id} value={m.ad}>{m.ad}</option>
              ))}
            </select>
          </div>
          <div className="col-md-3">
            <label className="form-label">Tipi</label>
            {!aracTipiManuelMi ? (
              <select
                className="form-select"
                value={form.aracTipi}
                disabled={!form.aracMarkasi}
                onChange={(e) => {
                  if (e.target.value === DIGER) { setAracTipiManuelMi(true); setForm((f) => ({ ...f, aracTipi: '' })); }
                  else setForm((f) => ({ ...f, aracTipi: e.target.value }));
                }}
              >
                <option value="">{form.aracMarkasi ? (modellerMarkada.length ? '-- Seçiniz --' : '-- Bu markada kayıtlı model yok --') : '-- Önce marka seçiniz --'}</option>
                {modellerMarkada.map((m) => (
                  <option key={m} value={m}>{m}</option>
                ))}
                {form.aracMarkasi && <option value={DIGER}>Diğer (elle yaz)</option>}
              </select>
            ) : (
              <input className="form-control" placeholder="Model adı (elle yazın)" value={form.aracTipi} onChange={(e) => setForm((f) => ({ ...f, aracTipi: e.target.value }))} />
            )}
          </div>
          <div className="col-md-3">
            <label className="form-label">Model Yılı</label>
            <select className="form-select" value={form.modelYili} onChange={(e) => setForm((f) => ({ ...f, modelYili: Number(e.target.value) }))} required>
              <option value="">-- Seçiniz --</option>
              {yillar.map((y) => (
                <option key={y} value={y}>{y}</option>
              ))}
            </select>
          </div>
          <div className="col-md-3">
            <label className="form-label">Renk</label>
            {!renkManuelMi ? (
              <select
                className="form-select"
                value={form.renk}
                onChange={(e) => {
                  if (e.target.value === DIGER) { setRenkManuelMi(true); setForm((f) => ({ ...f, renk: '' })); }
                  else setForm((f) => ({ ...f, renk: e.target.value }));
                }}
              >
                <option value="">-- Seçiniz --</option>
                {veri.renkler.map((r) => (
                  <option key={r.id} value={r.ad}>{r.ad}</option>
                ))}
                <option value={DIGER}>Diğer (elle yaz)</option>
              </select>
            ) : (
              <input className="form-control" placeholder="Renk (elle yazın)" value={form.renk} onChange={(e) => setForm((f) => ({ ...f, renk: e.target.value }))} />
            )}
          </div>
          <div className="col-md-3">
            <label className="form-label">Ruhsatın Olup Olmadığı</label>
            <select className="form-select" value={form.ruhsatVar ? 'true' : 'false'} onChange={(e) => setForm((f) => ({ ...f, ruhsatVar: e.target.value === 'true' }))}>
              <option value="true">VAR</option>
              <option value="false">YOK</option>
            </select>
          </div>
          <div className="col-md-3">
            <label className="form-label">Anahtarın Bulunup Bulunmadığı</label>
            <select className="form-select" value={form.anahtarVar ? 'true' : 'false'} onChange={(e) => setForm((f) => ({ ...f, anahtarVar: e.target.value === 'true' }))}>
              <option value="true">VAR</option>
              <option value="false">YOK</option>
            </select>
          </div>
          <div className="col-md-3">
            <label className="form-label">Aracın Bulunduğu Otopark</label>
            <select className="form-select" value={form.otoparkAdi} onChange={(e) => setForm((f) => ({ ...f, otoparkAdi: e.target.value }))}>
              <option value="">-- Seçiniz --</option>
              {veri.otoparklar.map((o) => (
                <option key={o.id} value={o.ad}>{o.ad}</option>
              ))}
            </select>
          </div>
          <div className="col-md-3">
            <label className="form-label">Otoparka Giriş Tarihi</label>
            <input className="form-control" type="date" value={form.otoparkaGirisTarihi} onChange={(e) => setForm((f) => ({ ...f, otoparkaGirisTarihi: e.target.value }))} />
          </div>

          <div className="col-md-3">
            <label className="form-label">Muhammen Bedeli (TL)</label>
            <div className="input-group">
              <input className="form-control" type="text" inputMode="decimal" placeholder="0,00" autoComplete="off" value={bedelGosterim} onChange={(e) => bedelDegisti(e.target.value)} />
              <span className="input-group-text">TL</span>
            </div>
          </div>
          <div className="col-md-3">
            <label className="form-label">Teminat (%5, 10.000 TL üzeri)</label>
            <div className="input-group">
              <input className="form-control" readOnly value={paraFormatla(teminat)} />
              <span className="input-group-text">TL</span>
            </div>
          </div>
          <div className="col-md-3">
            <label className="form-label">KDV Oranı (%)</label>
            <select className="form-select" value={form.kdvOrani} onChange={(e) => setForm((f) => ({ ...f, kdvOrani: Number(e.target.value) }))}>
              <option value={1}>%1</option>
              <option value={10}>%10</option>
              <option value={20}>%20</option>
            </select>
          </div>
          <div className="col-md-3">
            <label className="form-label">Damga Vergisi Oranı (Binde)</label>
            <input className="form-control" type="number" step="0.01" min={0} value={form.damgaVergisiOrani} onChange={(e) => setForm((f) => ({ ...f, damgaVergisiOrani: e.target.value === '' ? '' : Number(e.target.value) }))} />
          </div>
          <div className="col-md-3">
            <label className="form-label">Damga Vergisi Tutarı (Tahmini)</label>
            <div className="input-group">
              <input className="form-control" readOnly value={paraFormatla(damgaVergisiTutari)} />
              <span className="input-group-text">TL</span>
            </div>
          </div>
          <div className="col-md-6">
            <label className="form-label">Hasar Durumu</label>
            <textarea className="form-control" rows={2} placeholder="Haciz tutanağında yapılan tespitler kısa açıklama olarak yazılabilir" value={form.hasarDurumu} onChange={(e) => setForm((f) => ({ ...f, hasarDurumu: e.target.value }))} />
          </div>
        </div>

        <div className="form-section-title">4. Satış Bilgileri</div>
        <div className="table-responsive">
          <table className="table table-bordered align-middle" style={{ width: 'auto' }}>
            <thead className="table-light">
              <tr>
                <th>Satış Türü</th>
                <th>Satış Tarihi</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td className="fw-semibold">1. Satış</td>
                <td><input className="form-control" type="date" style={{ width: 170 }} value={form.satis1Tarihi} onChange={(e) => setForm((f) => ({ ...f, satis1Tarihi: e.target.value }))} /></td>
              </tr>
              <tr>
                <td className="fw-semibold">2. Satış</td>
                <td><input className="form-control" type="date" style={{ width: 170 }} value={form.satis2Tarihi} onChange={(e) => setForm((f) => ({ ...f, satis2Tarihi: e.target.value }))} /></td>
              </tr>
            </tbody>
          </table>
        </div>

        <div className="mt-4">
          <button type="submit" className="btn btn-primary" disabled={kaydediliyor}>
            {kaydediliyor ? 'Kaydediliyor...' : 'Dosyayı Kaydet'}
          </button>
          <button type="button" className="btn btn-outline-secondary ms-2" onClick={() => navigate('/')}>Vazgeç</button>
        </div>
      </form>
    </div>
  );
}
