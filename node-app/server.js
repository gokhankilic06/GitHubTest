// Riba Veli Anket Sistemi - Node.js sürümü
//
// Ortam değişkenleri:
//   PORT            Sunucu portu (varsayılan 3000)
//   ADMIN_PASSWORD  Yönetici şifresi (varsayılan: admin123 - mutlaka değiştirin)
//   SESSION_SECRET  Oturum çerezlerini imzalamak için gizli anahtar (önerilir)
//   DATABASE_URL    Tanımlıysa PostgreSQL/Supabase kullanılır
//   SQLITE_FILE     SQLite dosya yolu (varsayılan data/anket.db)

const { createDb } = require('./src/db');
const { createApp } = require('./src/app');

async function main() {
  const db = await createDb();
  const adminPassword = process.env.ADMIN_PASSWORD || 'admin123';
  if (!process.env.ADMIN_PASSWORD) console.warn('UYARI: ADMIN_PASSWORD tanımlı değil, varsayılan şifre (admin123) kullanılıyor.');

  const app = createApp({
    db,
    adminPassword,
    sessionSecret: process.env.SESSION_SECRET,
    secureCookies: process.env.NODE_ENV === 'production'
  });

  const port = process.env.PORT || 3000;
  app.listen(port, () => {
    console.log(`Veritabanı: ${db.kind === 'postgres' ? 'PostgreSQL (DATABASE_URL)' : 'SQLite'}`);
    console.log(`Veli anketi:     http://localhost:${port}`);
    console.log(`Yönetici paneli: http://localhost:${port}/admin`);
  });
}

main().catch(err => {
  console.error('Sunucu başlatılamadı:', err.message);
  process.exit(1);
});
