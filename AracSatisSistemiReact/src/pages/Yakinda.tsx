import { Link } from 'react-router-dom';

export function Yakinda() {
  return (
    <div className="text-center py-5">
      <i className="bi bi-hourglass-split" style={{ fontSize: '2.5rem' }}></i>
      <h4 className="mt-3">Bu ekran yakında eklenecek</h4>
      <p className="text-muted">React sürümü aşamalı olarak geliştiriliyor.</p>
      <Link to="/" className="btn btn-outline-primary">
        <i className="bi bi-house"></i> Ana Sayfaya Dön
      </Link>
    </div>
  );
}
