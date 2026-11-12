import { Link } from 'react-router-dom';

interface MenuKarti {
  yol: string;
  ikon: string;
  baslik: string;
  aciklama: string;
  hazir: boolean;
}

const kartlar: MenuKarti[] = [
  { yol: '/dosya/yeni', ikon: 'bi-file-earmark-plus', baslik: 'KAYIT İŞLEMLERİ', aciklama: 'Yeni dosya, vergi dairesi, borçlu ve araç bilgisi girişi', hazir: true },
  { yol: '/dosya', ikon: 'bi-search', baslik: 'DOSYA ARA / LİSTELE', aciklama: 'Kayıtlı dosyaları arayın, filtreleyin ve detaylarını görüntüleyin', hazir: true },
  { yol: '/yakinda', ikon: 'bi-file-earmark-text', baslik: 'İHALE TUTANAĞI', aciklama: 'Dosya no ile ihale öncesi yazdırılacak İhale Tutanağı\'na ulaşım', hazir: false },
  { yol: '/yakinda', ikon: 'bi-cash-coin', baslik: 'SATIŞ SONUÇLARINI GİR', aciklama: 'İhale sonrası satış türü/tarihine göre araçları bulup sonuç ve alıcı bilgisi girişi', hazir: false },
  { yol: '/yakinda', ikon: 'bi-person-check', baslik: 'ALICI ÖDEME BİLGİLERİ', aciklama: 'Satılan araçlar için alıcı kimlik, adres, telefon ve ödeme vadesi girişi', hazir: false },
  { yol: '/yakinda', ikon: 'bi-people-fill', baslik: 'KOMİSYON İŞLEMLERİ', aciklama: 'Satış türü/tarihi/saatine göre komisyon başkanı ve üyelerinin atanması', hazir: false },
  { yol: '/yakinda', ikon: 'bi-arrow-return-left', baslik: 'İADE KAPAMA İŞLEMLERİ', aciklama: 'Satış iptali, iadesi ve dosya kapatma', hazir: false },
  { yol: '/yakinda', ikon: 'bi-list-check', baslik: 'YAZIŞMALAR VE LİSTELER', aciklama: 'Araç durum listesi, satış sonuç listesi, yazdır / PDF / Excel', hazir: false },
  { yol: '/yakinda', ikon: 'bi-clipboard-data', baslik: 'TUTANAK VE RAPORLAR', aciklama: 'Çok kriterli filtreleme, İhale Tutanağı toplu yazdırma/PDF/Excel', hazir: false },
  { yol: '/yakinda', ikon: 'bi-envelope-paper', baslik: 'YAZIŞMA', aciklama: 'Dosya no ile borçlu/alıcı/araç ve mali özet bilgilerinin görüntülenmesi', hazir: false },
  { yol: '/yakinda', ikon: 'bi-calendar-week', baslik: 'SATIŞ TAKVİMİ', aciklama: 'Yıllık 1. ve 2. Satış (ihale) tarihlerinin belirlenmesi', hazir: false },
  { yol: '/yakinda', ikon: 'bi-bar-chart-line', baslik: 'İSTATİSTİK', aciklama: 'Tüm kayıt ve satış sonuçlarının yıl/ay/tarih/tür filtreleriyle analizi', hazir: false },
  { yol: '/yakinda', ikon: 'bi-gear', baslik: 'GÜNCELLEMELER', aciklama: 'Komisyon, tarife, referans listeleri ve veri yedekleme', hazir: false },
];

export function AnaSayfa() {
  return (
    <div>
      <div className="text-center mb-4">
        <h2>Araç Satış Programı</h2>
        <p className="text-muted">Haczedilen araçların kayıt, satış ve takip süreçlerini yönetin</p>
        <div className="alert alert-info d-inline-block">
          Bu, mevcut programın React ile yapılan yeni arayüzü - aşamalı olarak geliştiriliyor.
          Şu an <strong>Kayıt İşlemleri</strong> ve <strong>Dosya Ara/Listele</strong> hazır;
          diğer ekranlar sırayla ekleniyor.
        </div>
      </div>

      <div className="row g-4 justify-content-center">
        {kartlar.map((k) => (
          <div className="col-md-4" key={k.baslik}>
            <Link to={k.yol} className="text-decoration-none">
              <div className={`card menu-card text-center p-4 h-100 ${k.hazir ? '' : 'opacity-50'}`}>
                <div className="menu-icon">
                  <i className={`bi ${k.ikon}`}></i>
                </div>
                <h5>{k.baslik}</h5>
                <p className="text-muted mb-0">{k.aciklama}</p>
                {!k.hazir && <span className="badge bg-secondary mt-2 align-self-center">Yakında</span>}
              </div>
            </Link>
          </div>
        ))}
      </div>
    </div>
  );
}
