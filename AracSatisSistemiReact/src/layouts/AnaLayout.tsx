import { Link, Outlet, useNavigate } from 'react-router-dom';
import { useEffect, useState } from 'react';
import { useAuth } from '../auth/AuthContext';

const menuOgeleri = [
  { yol: '/dosya/yeni', etiket: 'Kayıt İşlemleri', ikon: 'bi-file-earmark-plus' },
  { yol: '/dosya', etiket: 'Dosya Ara / Listele', ikon: 'bi-search' },
  { yol: '/', etiket: 'Ana Sayfa', ikon: 'bi-house' },
];

export function AnaLayout() {
  const { kullanici, cikisYap } = useAuth();
  const navigate = useNavigate();
  const [saat, setSaat] = useState('');

  useEffect(() => {
    function guncelle() {
      const simdi = new Date();
      setSaat(
        simdi.toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric' }) +
          ' - ' +
          simdi.toLocaleTimeString('tr-TR'),
      );
    }
    guncelle();
    const id = setInterval(guncelle, 1000);
    return () => clearInterval(id);
  }, []);

  function cikis() {
    cikisYap();
    navigate('/giris');
  }

  return (
    <div>
      <nav className="navbar navbar-expand-lg navbar-dark app-navbar mb-4">
        <div className="container-fluid">
          <Link className="navbar-brand d-flex align-items-center gap-2" to="/">
            <i className="bi bi-car-front-fill"></i> Araç Satış Sistemi (React)
          </Link>
          <button
            className="navbar-toggler"
            type="button"
            data-bs-toggle="collapse"
            data-bs-target="#navMenu"
          >
            <span className="navbar-toggler-icon"></span>
          </button>
          <div className="collapse navbar-collapse" id="navMenu">
            <ul className="navbar-nav me-auto">
              {menuOgeleri.map((m) => (
                <li className="nav-item" key={m.yol}>
                  <Link className="nav-link" to={m.yol}>
                    <i className={`bi ${m.ikon}`}></i> {m.etiket}
                  </Link>
                </li>
              ))}
            </ul>
            <span className="navbar-text text-white-50 small d-flex align-items-center gap-1">
              <i className="bi bi-clock"></i> {saat}
            </span>
            {kullanici && (
              <span className="navbar-text text-white-50 small ms-3 d-flex align-items-center gap-2">
                <i className="bi bi-person-circle"></i> {kullanici.adSoyad}
                <button className="btn btn-sm btn-outline-light" onClick={cikis}>
                  <i className="bi bi-box-arrow-right"></i> Çıkış
                </button>
              </span>
            )}
          </div>
        </div>
      </nav>

      <div className="container">
        <Outlet />
      </div>

      <footer className="text-center text-muted small py-4 mt-4">
        Araç Satış Sistemi (React) &middot; {saat}
      </footer>
    </div>
  );
}
