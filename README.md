# Veraset Randevu Sistemi

Ankara Veraset ve Harçlar Vergi Dairesi için online randevu sistemi. Aynı arayüz ve aynı API üç ayrı backend ile yazıldı: **Node.js**, **Python** ve **C#**. Hangisi kullanılırsa kullanılsın davranış aynıdır. Üçü de aynı ortak test paketinden geçer.

## Randevu akışı

1. **Açıklama ve tarih:** Vatandaş açıklama metnini okur ve veraset belgesini indirir. "Okudum, taahhüt ederim" kutusunu işaretler. Vefat edenin ikamet ettiği ilçeyi seçer (Ankara'nın 25 ilçesi). Son olarak takvimden gün seçer.
   - Yeşil günlerde randevu alınabilir. Gri günler hafta sonu, resmi tatil, tamamen dolu ya da randevu penceresinin dışındadır.
   - Aynı gün için randevu verilmez. Her gün saat 17:00'de pencerenin sonuna yeni bir gün eklenir.
2. **Saat seçimi:** Seçilen günün boş saatleri gösterilir. Dolu saatler seçilemez.
3. **Bilgiler:** Vergi dairesi, tarih ve saat salt okunur olarak gösterilir. Vatandaş şu bilgileri girer:
   - Vefat edenin adı, soyadı ve T.C. numarası
   - Kendi adı, soyadı, doğum yılı, T.C. numarası ve cep numarası
4. **Onay:** Randevu numarası ve özet gösterilir. Sayfa yazdırılabilir.

Ankara'da tek vergi dairesi olduğu için vergi dairesi seçimi yoktur. Vergi dairesinin adı `config.json` dosyasından okunur.

### Sunucu tarafı kurallar

- T.C. kimlik numaraları algoritmayla doğrulanır. Randevuyu alan kişi ile vefat eden kişinin numarası aynı olamaz.
- Cep numarası `5xxxxxxxxx` biçiminde olmalı. Başında 0 ile girilirse 0 atılır.
- Ad ve soyadlar Türkçe kurallarına göre büyük harfe çevrilerek kaydedilir (i → İ).
- Her saate `slotKapasitesi` kadar randevu verilir (varsayılan 1). Aynı anda gelen iki istek aynı saati alamaz.
- Aynı vefat eden kişi için birden fazla aktif randevu alınamaz.

## Personel paneli

`/yonetim` adresinden açılır ve HTTP Basic kimlik doğrulaması ister:
- Kullanıcı adı: `ADMIN_USER` ortam değişkeni (varsayılan `yonetici`)
- Şifre: `ADMIN_PASSWORD` ortam değişkeni. Tanımlanmazsa panel kapalı kalır.

Panelde randevular tarihe göre listelenir, yazdırılır ve iptal edilebilir. İptal edilen saat yeniden boşa çıkar.

## Klasör yapısı

```
config.json      Ortak ayarlar: vergi dairesi, ilçeler, mesai, tatiller, belge
schema.sql       Ortak SQLite şeması
web/public/      Ortak arayüz (HTML/CSS/JS)
  aciklama.html  İlk sayfadaki açıklama metni
  belgeler/      İndirilecek veraset belgesi buraya konur
web/yonetim.html Personel paneli (yalnızca giriş yapınca sunulur)
node/            Node.js + Express backend
python/          Python + Flask backend
csharp/          C# ASP.NET Core (.NET 8) backend
test/            Üç backend için ortak API testleri
```

## Çalıştırma

Üç backend'in hepsi varsayılan olarak `data/randevu.db` SQLite dosyasını kullanır.

| Ortam değişkeni  | Açıklama                                               |
|------------------|--------------------------------------------------------|
| `PORT`           | Dinlenecek port                                        |
| `DB_PATH`        | SQLite dosya yolu                                      |
| `ADMIN_USER`     | Panel kullanıcı adı (varsayılan `yonetici`)            |
| `ADMIN_PASSWORD` | Panel şifresi. Tanımlanmazsa panel kapalıdır.           |

### Node.js (22.13 veya üstü)

```bash
cd node
npm install
ADMIN_PASSWORD=gizli npm start        # http://localhost:3000
```

### Python (3.10 veya üstü)

```bash
cd python
python3 -m venv .venv
.venv/bin/pip install -r requirements.txt
ADMIN_PASSWORD=gizli .venv/bin/python app.py   # http://localhost:5000
```

### C# (.NET 8 SDK)

```bash
ADMIN_PASSWORD=gizli dotnet run --project csharp   # http://localhost:5100
```

Windows PowerShell'de ortam değişkenini şöyle tanımlayın: `$env:ADMIN_PASSWORD="gizli"`.

## Testler

Ortak test paketi seçilen backend'i geçici bir veritabanıyla ve sabit bir tarihle (`SIMDI`) başlatır, sonra tüm API'yi test eder:

```bash
npm run test:node
npm run test:python    # python/.venv kurulu olmalı
npm run test:csharp
npm test               # üçünü sırayla çalıştırır
```

## Yapılacaklar / içerik

- **Açıklama metni:** `web/public/aciklama.html` dosyasına yazılacak.
- **Veraset belgesi:** `web/public/belgeler/veraset-beyannamesi.pdf` olarak eklenecek. Dosya yoksa indirme butonu gizlenir.
- **Resmi tatiller:** `config.json` içindeki `tatiller` ve `yarimGunler` listeleri her yıl güncellenmeli. 2027 dini bayramları henüz eklenmedi.
- **Diğer ayarlar:** Mesai saatleri, randevu aralığı (15 dk), saat başına kapasite ve kaç gün ilerisine randevu verileceği `config.json` dosyasından ayarlanır.
