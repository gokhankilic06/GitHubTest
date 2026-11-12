import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { dosyaDetayiniGetir } from '../../api/dosya';
import type { Dosya } from '../../types';

function tarihFormatla(deger: string | null, saatVar = false) {
  if (!deger) return '';
  const d = new Date(deger);
  const tarih = d.toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric' });
  if (!saatVar) return tarih;
  return tarih + ' ' + d.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
}

function paraFormatla(deger: number) {
  return deger.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' TL';
}

export function DosyaDetay() {
  const { id } = useParams();
  const [dosya, setDosya] = useState<Dosya | null>(null);
  const [yukleniyor, setYukleniyor] = useState(true);

  useEffect(() => {
    (async () => {
      setYukleniyor(true);
      const veri = await dosyaDetayiniGetir(Number(id));
      setDosya(veri);
      setYukleniyor(false);
    })();
  }, [id]);

  if (yukleniyor) return <div className="text-center py-5">Yükleniyor...</div>;
  if (!dosya) return <div className="alert alert-danger">Dosya bulunamadı.</div>;

  return (
    <div>
      <div className="d-flex justify-content-between align-items-center mb-3">
        <h3>Dosya No: {dosya.dosyaNo} - {dosya.durumGorunenAd}</h3>
        <Link to="/dosya" className="btn btn-outline-secondary">
          <i className="bi bi-arrow-left"></i> Listeye Dön
        </Link>
      </div>

      <div className="a4-sayfa">
        <div className="letterhead mb-3 pb-2 d-flex align-items-start">
          <div style={{ width: 90 }}></div>
          <div className="text-center flex-grow-1">
            <h4 className="mb-0 fw-bold text-nowrap">ANKARA DEFTERDARLIĞI</h4>
            <div className="text-muted">Araç Kayıt Belgesi</div>
            <div>{tarihFormatla(dosya.kayitTarihi)}</div>
          </div>
          <div style={{ width: 90 }}></div>
        </div>

        <div className="belge-bolumu">
          <div className="d-flex flex-wrap gap-3">
            <div className="kimlik-kutu">
              <span className="kimlik-etiket">Dosya No:</span>
              <span className="kimlik-deger">{dosya.dosyaNo}</span>
            </div>
            <div className="kimlik-kutu">
              <span className="kimlik-etiket">Durum:</span>
              <span className={`durum-rozet durum-${dosya.durum}`}>{dosya.durumGorunenAd}</span>
            </div>
            <div className="kimlik-kutu">
              <span className="kimlik-etiket">Aşama:</span>
              <span className={`badge ${dosya.asamaRozetSinifi}`}>{dosya.asamaGorunen}</span>
            </div>
          </div>
        </div>

        <div className="belge-bolumu">
          <div className="form-section-title">İlgili Vergi Dairesi</div>
          <table className="detay-tablo">
            <tbody>
              <tr><td className="etiket">Vergi Dairesi Adı</td><td colSpan={3}>{dosya.vergiDairesiAdi}</td></tr>
              <tr>
                <td className="etiket">Vergi Dairesi Yazısı Tarihi</td><td>{tarihFormatla(dosya.vergiDairesiYaziTarihi)}</td>
                <td className="etiket">Vergi Dairesi Yazısı Sayısı</td><td>{dosya.vergiDairesiYaziSayisi}</td>
              </tr>
              <tr><td className="etiket">Alacaklı Vergi Dairesi</td><td colSpan={3}>{dosya.alacakliVergiDairesi}</td></tr>
            </tbody>
          </table>
        </div>

        <div className="belge-bolumu">
          <div className="form-section-title">Borçlu Mükellef</div>
          <table className="detay-tablo">
            <tbody>
              <tr><td className="etiket">Adı Soyadı / Unvanı</td><td colSpan={3}>{dosya.adiSoyadiUnvani}</td></tr>
              <tr>
                <td className="etiket">T.C. Kimlik No</td><td>{dosya.tcKimlikNumarasi}</td>
                <td className="etiket">Vergi Kimlik No</td><td>{dosya.vergiKimlikNumarasi}</td>
              </tr>
              <tr><td className="etiket">Kanuni Temsilci</td><td colSpan={3}>{dosya.kanuniTemsilci}</td></tr>
            </tbody>
          </table>
        </div>

        <div className="belge-bolumu">
          <div className="form-section-title">Araç Bilgileri</div>
          <table className="detay-tablo">
            <tbody>
              <tr>
                <td className="etiket">Plaka</td><td>{dosya.plaka}</td>
                <td className="etiket">Markası</td><td>{dosya.aracMarkasi}</td>
              </tr>
              <tr>
                <td className="etiket">Cinsi</td><td>{dosya.aracCinsi}</td>
                <td className="etiket">Tipi</td><td>{dosya.aracTipi}</td>
              </tr>
              <tr>
                <td className="etiket">Model Yılı</td><td>{dosya.modelYili}</td>
                <td className="etiket">Renk</td><td>{dosya.renk}</td>
              </tr>
              <tr>
                <td className="etiket">Ruhsat</td><td>{dosya.ruhsatVar ? 'VAR' : 'YOK'}</td>
                <td className="etiket">Anahtar</td><td>{dosya.anahtarVar ? 'VAR' : 'YOK'}</td>
              </tr>
              <tr>
                <td className="etiket">Çekici</td><td>{dosya.cekiciVar ? 'VAR' : 'YOK'}</td>
                <td className="etiket">Otopark</td><td>{dosya.otoparkAdi}</td>
              </tr>
              <tr>
                <td className="etiket">Otoparka Giriş Tarihi</td><td>{tarihFormatla(dosya.otoparkaGirisTarihi)}</td>
                <td className="etiket">Hasar Durumu</td><td>{dosya.hasarDurumu}</td>
              </tr>
              <tr>
                <td className="etiket">Muhammen Bedel</td><td>{paraFormatla(dosya.muhammenBedel)}</td>
                <td className="etiket">Teminat (%5)</td><td>{paraFormatla(dosya.teminat)}</td>
              </tr>
              <tr>
                <td className="etiket">KDV Oranı</td><td>%{dosya.kdvOrani}</td>
                <td className="etiket">Damga Vergisi (Binde {dosya.damgaVergisiOrani.toFixed(2)})</td><td>{paraFormatla(dosya.damgaVergisiTutari)}</td>
              </tr>
            </tbody>
          </table>
        </div>

        {dosya.satislar.length === 0 ? (
          <div className="belge-bolumu">
            <div className="form-section-title">Planlanan İhale Tarihleri</div>
            <table className="detay-tablo">
              <tbody>
                <tr>
                  <td className="etiket">1. Satış Tarihi</td><td>{tarihFormatla(dosya.satis1Tarihi)}</td>
                  <td className="etiket">2. Satış Tarihi</td><td>{tarihFormatla(dosya.satis2Tarihi)}</td>
                </tr>
              </tbody>
            </table>
          </div>
        ) : (
          <div className="belge-bolumu">
            <div className="form-section-title">Satış Bilgileri</div>
            <table className="satis-tablo">
              <thead>
                <tr><th>Tür</th><th>Tarih</th><th>Sonuç</th><th>Bedel (TL)</th></tr>
              </thead>
              <tbody>
                {dosya.satislar.map((s) => (
                  <tr key={s.id}>
                    <td>{s.satisTuruGorunen}</td>
                    <td>{tarihFormatla(s.satisTarihi)}</td>
                    <td>{s.sonucGorunen}</td>
                    <td>{s.satisBedeli?.toLocaleString('tr-TR', { minimumFractionDigits: 2 }) ?? '-'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
