# Riba Veli Anket Sistemi — Node.js sürümü

Yavuz Sultan Selim İlkokulu için iki seçenekli (A/B) veli anketi ve yönetici paneli.

## Özellikler

**Veli sayfası**
- Veli adını soyadını yazar ve sınıfını listeden seçer.
- 13 adet A/B sorusu vardır. Eksik bırakılan sorular kırmızı ile işaretlenir.

**Yönetici paneli (`/admin`)**
- **Sınıf filtresi:** Tüm tablolar, grafikler ve Excel çıktısı seçilen sınıfa göre değişir.
- **Grafikler:** soru bazlı A/B oranı ve sınıflara göre katılım.
- **Tablolar:** katılımcı listesi, soru bazlı dağılım, bireysel yanıt tablosu (M1–M13), şık bazlı sonuçlar ve sınıf özeti.
- **Excel'e Aktar (.xlsx):** 5 sayfalık rapor. Sayfalar: Katılımcılar, Soru Bazlı Dağılım, Bireysel Yanıtlar, Şık Bazlı Sonuçlar, Sınıf Özeti.
- Tek bir yanıtı silme ve tüm verileri sıfırlama.

| Veli formu | Sonuçlar (sınıf filtresi + grafikler) |
|---|---|
| ![Anket](docs/anket.png) | ![Panel](docs/panel.png) |

## Kurulum

### Windows'ta en kolay yol
1. https://nodejs.org adresinden **LTS** sürümünü kurun (Node.js 22.13 veya üzeri).
2. Zip'i çıkarın ve **`baslat.bat`** dosyasına çift tıklayın.

İlk açılışta kurulum otomatik yapılır ve yönetici paneli tarayıcıda açılır (şifre: `admin123`). Siyah pencere açık kaldığı sürece site çalışır.

> PowerShell'de `npm` komutu "running scripts is disabled" hatası verirse `npm` yerine `npm.cmd` yazın veya `baslat.bat` dosyasını kullanın.

### Komut satırından
Node.js 22.13 veya üzeri gerekir. Derleme gerektiren bir paket yoktur, SQLite için Node.js'in kendi modülü kullanılır.

```bash
cd node-app
npm install
ADMIN_PASSWORD=gucluBirSifre SESSION_SECRET=uzunRastgeleMetin npm start
```

- Veli sayfası: http://localhost:3000
- Yönetici paneli: http://localhost:3000/admin

### Ortam değişkenleri

| Değişken | Açıklama |
|---|---|
| `ADMIN_PASSWORD` | Yönetici şifresi. **Mutlaka değiştirin.** Varsayılan: `admin123` |
| `SESSION_SECRET` | Oturum çerezini imzalayan gizli anahtar (uzun, rastgele bir metin) |
| `DATABASE_URL` | Tanımlıysa **PostgreSQL / Supabase** kullanılır |
| `SQLITE_FILE` | SQLite dosyasının yolu. Varsayılan: `data/anket.db` |
| `PORT` | Sunucu portu. Varsayılan: `3000` |
| `NODE_ENV=production` | Çerezleri yalnızca HTTPS üzerinden gönderir |

## Veritabanı seçimi

### 1) Kendi sunucunuzda: SQLite (varsayılan)
Hiçbir ayar gerekmez. Veriler `data/anket.db` dosyasında tutulur. Yedek almak için bu dosyayı kopyalamanız yeterlidir.

### 2) Ücretsiz barındırmada: Supabase (PostgreSQL)
Render, Railway gibi ücretsiz servislerde disk kalıcı değildir ve SQLite dosyası silinir. Bu durumda Supabase kullanın:

1. https://supabase.com adresinde ücretsiz bir proje oluşturun.
2. **Project Settings → Database → Connection string** bölümüne gidin ve **Session pooler** adresini kopyalayın. Adres şu şekildedir:
   `postgresql://postgres.xxxx:SIFRE@aws-0-eu-central-1.pooler.supabase.com:5432/postgres`
3. Barındırma servisinde bu adresi `DATABASE_URL` ortam değişkeni olarak girin.

Tablolar ilk çalıştırmada otomatik oluşturulur.

**Örnek: Render.com**
- Build Command: `cd node-app && npm install`
- Start Command: `cd node-app && npm start`
- Environment: `DATABASE_URL`, `ADMIN_PASSWORD`, `SESSION_SECRET`, `NODE_ENV=production`

## Soruları / sınıfları düzenleme

Okul adı, sorular, şıklar ve sınıf listesi `questions.js` dosyasındadır.
Soru sırası değiştirilirse mevcut yanıtlar yanlış soruya eşleşir. Bu durumda önce panelden verileri sıfırlayın.

## Testler

```bash
npm test                                                   # SQLite üzerinde
TEST_DATABASE_URL=postgres://localhost/anket_test npm test # ayrıca PostgreSQL üzerinde
```

> Uyarı: `TEST_DATABASE_URL` veritabanındaki anket verileri test sırasında silinir. Canlı veritabanını vermeyin.

## Güvenlik

- Yönetici oturumu imzalı ve `HttpOnly` bir çerezle tutulur. Sunucu yeniden başlasa da oturum korunur; şifre değişince eski oturumlar geçersiz olur.
- Hatalı şifre denemeleri sınırlıdır: IP başına 15 dakikada 10 deneme.
