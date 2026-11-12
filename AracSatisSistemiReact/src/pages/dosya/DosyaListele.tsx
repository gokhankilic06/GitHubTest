import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { dosyalariListele } from '../../api/dosya';
import type { DosyaOzeti } from '../../types';

export function DosyaListele() {
  const [liste, setListe] = useState<DosyaOzeti[]>([]);
  const [arama, setArama] = useState('');
  const [durumFiltre, setDurumFiltre] = useState('');
  const [yukleniyor, setYukleniyor] = useState(true);

  async function ara(e?: React.FormEvent) {
    e?.preventDefault();
    setYukleniyor(true);
    try {
      const sonuc = await dosyalariListele(arama, durumFiltre);
      setListe(sonuc);
    } finally {
      setYukleniyor(false);
    }
  }

  useEffect(() => {
    ara();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <div>
      <h3>Dosya Ara / Listele</h3>

      <form className="row g-2 mb-3" onSubmit={ara}>
        <div className="col-md-4">
          <input
            className="form-control"
            placeholder="Dosya No / Plaka / Adı Soyadı"
            value={arama}
            onChange={(e) => setArama(e.target.value)}
          />
        </div>
        <div className="col-md-3">
          <select className="form-select" value={durumFiltre} onChange={(e) => setDurumFiltre(e.target.value)}>
            <option value="">Tümü (Aktif + Pasif)</option>
            <option value="aktif">Sadece Aktif</option>
            <option value="pasif">Sadece Pasif</option>
          </select>
        </div>
        <div className="col-auto">
          <button className="btn btn-primary" type="submit">Ara</button>
          <Link to="/dosya/yeni" className="btn btn-success ms-2">+ Yeni Dosya</Link>
        </div>
      </form>

      <table className="table table-striped table-bordered bg-white">
        <thead>
          <tr>
            <th>Dosya No</th>
            <th>Kayıt Tarihi</th>
            <th>Plaka</th>
            <th>Adı Soyadı / Unvanı</th>
            <th>Vergi Dairesi</th>
            <th>Genel Durum</th>
            <th>Aşama</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {yukleniyor && (
            <tr><td colSpan={8} className="text-center text-muted">Yükleniyor...</td></tr>
          )}
          {!yukleniyor && liste.map((d) => (
            <tr key={d.id}>
              <td>{d.dosyaNo}</td>
              <td>{new Date(d.kayitTarihi).toLocaleString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })}</td>
              <td>{d.plaka}</td>
              <td>{d.adiSoyadiUnvani}</td>
              <td>{d.vergiDairesiAdi}</td>
              <td><span className={`badge ${d.aktif ? 'bg-success' : 'bg-secondary'}`}>{d.genelDurumGorunen}</span></td>
              <td><span className={`badge ${d.asamaRozetSinifi}`}>{d.asamaGorunen}</span></td>
              <td><Link className="btn btn-sm btn-outline-primary" to={`/dosya/${d.id}`}>Görüntüle</Link></td>
            </tr>
          ))}
          {!yukleniyor && liste.length === 0 && (
            <tr><td colSpan={8} className="text-center text-muted">Kayıt bulunamadı.</td></tr>
          )}
        </tbody>
      </table>
    </div>
  );
}
