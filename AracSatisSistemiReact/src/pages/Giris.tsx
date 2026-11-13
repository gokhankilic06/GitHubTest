import { useState, type FormEvent } from 'react';
import { useNavigate, useLocation, Navigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

export function Giris() {
  const { kullanici, girisYap } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [kullaniciAdi, setKullaniciAdi] = useState('');
  const [sifre, setSifre] = useState('');
  const [hata, setHata] = useState<string | null>(null);
  const [yukleniyor, setYukleniyor] = useState(false);

  if (kullanici) return <Navigate to="/" replace />;

  async function gonder(e: FormEvent) {
    e.preventDefault();
    setHata(null);
    setYukleniyor(true);
    try {
      await girisYap(kullaniciAdi, sifre);
      const donenYol = (location.state as { from?: string } | null)?.from || '/';
      navigate(donenYol, { replace: true });
    } catch (err: any) {
      if (err?.response?.status === 401) {
        setHata('Kullanıcı adı veya şifre hatalı.');
      } else if (!err?.response) {
        // Sunucuya hiç ulaşılamadı (çalışmıyor, yanlış adres/port, ağ/CORS engeli vb.) -
        // bunu "şifre hatalı" diye göstermek yanıltıcı olurdu.
        setHata(
          `Sunucuya bağlanılamadı (${import.meta.env.VITE_API_BASE_URL}). ` +
            'AracSatisSistemi (backend) çalışıyor mu ve adres doğru mu kontrol edin.',
        );
      } else {
        setHata(`Giriş başarısız (sunucu kodu: ${err.response.status}).`);
      }
    } finally {
      setYukleniyor(false);
    }
  }

  return (
    <div
      className="d-flex align-items-center"
      style={{ minHeight: '100vh', background: '#1b2436' }}
    >
      <div className="container">
        <div className="row justify-content-center">
          <div className="col-md-5 col-lg-4">
            <div className="text-center text-white mb-4">
              <i className="bi bi-car-front-fill" style={{ fontSize: '2.5rem' }}></i>
              <h4 className="mt-2">Araç Satış Sistemi (React)</h4>
            </div>
            <div className="card p-4 shadow">
              <h5 className="mb-3">Giriş Yap</h5>
              {hata && <div className="alert alert-danger py-2">{hata}</div>}
              <form onSubmit={gonder}>
                <div className="mb-3">
                  <label className="form-label">Kullanıcı Adı</label>
                  <input
                    className="form-control"
                    value={kullaniciAdi}
                    onChange={(e) => setKullaniciAdi(e.target.value)}
                    autoFocus
                    required
                  />
                </div>
                <div className="mb-3">
                  <label className="form-label">Şifre</label>
                  <input
                    className="form-control"
                    type="password"
                    value={sifre}
                    onChange={(e) => setSifre(e.target.value)}
                    required
                  />
                </div>
                <button type="submit" className="btn btn-primary w-100" disabled={yukleniyor}>
                  <i className="bi bi-box-arrow-in-right"></i>{' '}
                  {yukleniyor ? 'Giriş yapılıyor...' : 'Giriş Yap'}
                </button>
              </form>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
