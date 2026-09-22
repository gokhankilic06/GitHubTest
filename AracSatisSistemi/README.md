# Araç Satış Sistemi (ASP.NET Core MVC)

Bu proje, ekteki Excel çalışmasında (Araç Satış Programına İlişkin Çalışma) tarif edilen
haciz/satış sürecini web tabanlı bir uygulamaya dönüştürür:

- **Kayıt İşlemleri**: Vergi dairesi, borçlu mükellef, araç bilgi girişi, komisyon bilgileri.
  Dosya no otomatik üretilir (YIL/000001 formatında). Muhammen bedel 10.000 TL üzeri ise
  teminat otomatik %5 hesaplanır; damga vergisi binde 5,69 üzerinden otomatik hesaplanır.
- **Satışa İlişkin Bilgiler**: Dosya no ile arama, satış türü/tarihi/sonucu girişi.
  "Alıcı çıkmadı" seçilirse satış bedeli alanı pasif olur. Vade, satış tarihinden
  itibaren otomatik 3 gün olarak hesaplanır. Otopark ücreti, giriş tarihi ile satış
  tarihi arasındaki gün sayısı x günlük ücret olarak hesaplanır.
- **İade Kapama İşlemleri**: Satış iptali / iadesi (tarih, sayı, neden) ve dosya kapatma.
- **Yazışmalar ve Listeler**: Satış türü ve/veya tarih aralığına göre filtrelenebilen
  Araç Durum Listesi ve Satış Sonuç Listesi (yazdırma desteği ile).
- **Güncellemeler**: Komisyon üyeleri, otopark günlük ücret tarifesi, araç markası/cinsi,
  vergi dairesi ve otopark listelerinin yönetimi.

## Teknoloji

- .NET 8 / ASP.NET Core MVC
- Entity Framework Core + SQLite (dosya tabanlı, kurulum gerektirmez)
- Bootstrap 5 (CDN üzerinden)

> Not: Bu kod bu ortamda derlenip çalıştırılamadı çünkü konteynerde .NET SDK'sı ve
> internet erişimi bulunmuyor. Kodlar elle, ASP.NET Core MVC standartlarına uygun
> şekilde yazılmıştır; kendi bilgisayarınızda aşağıdaki adımlarla çalıştırabilirsiniz.

## Çalıştırma

1. [.NET 8 SDK](https://dotnet.microsoft.com/download) kurulu olmalı.
2. Proje klasöründe:
   ```bash
   dotnet restore
   dotnet run
   ```
3. Konsolda görünen adresi (ör. `https://localhost:5001`) tarayıcınızda açın.
   İlk çalıştırmada `AracSatis.db` adlı SQLite veritabanı otomatik oluşturulur ve
   başlangıç (seed) verileri (araç cinsleri, örnek marka/vergi dairesi/otopark listeleri) yüklenir.

Visual Studio kullanıyorsanız `.csproj` dosyasını açmanız yeterlidir.

## Bilinen sınırlamalar / genişletilebilecek noktalar

- Araç markası listesi https://www.tsb.org.tr/tr/kasko-deger-listesi adresinden otomatik
  çekilmemiştir (bu ortamda internet erişimi yok); başlangıç için yaygın markalardan oluşan
  bir liste eklendi. "Güncellemeler" ekranından manuel ekleme/silme yapılabilir; isterseniz
  ben bu listeyi web'den alıp genişletebilirim.
- "İhale Satış Tutanağı", "Araç Satış Kararı", "Vergi Dairesi Yazısı" gibi resmi belgeler için
  şu an sadece dosya detay sayfasının yazdırılması var; istenirse Word/PDF şablonlarına
  otomatik doldurma (mail-merge) eklenebilir.
- Kullanıcı girişi / yetkilendirme (kim hangi ekranı görebilir, komisyon güncelleme yetkisi vb.)
  henüz eklenmedi; kurumsal kullanım için ASP.NET Core Identity eklenmesi önerilir.
- Kimlik doğrulama olmadığından şu an herkes her ekrana erişebilir; canlıya almadan önce
  giriş ekranı eklenmelidir.

## Yeni Eklenenler (2. Sürüm)

- **Tarih/Saat**: Menüde canlı saat, tüm kayıtlarda tarih+saat (ör. dosya kayıt anı).
- **Yazdır**: Dosya Detayı ve Listeler ekranında "Yazdır" butonu (tarayıcı yazdırma, menü/butonlar gizlenir).
- **PDF olarak kaydet**: Dosya Detayı ekranında "PDF Olarak Kaydet" butonu (jsPDF + html2canvas ile
  gerçek .pdf dosyası indirilir - internet bağlantısı gerektirir çünkü kütüphaneler CDN'den yüklenir).
- **Excel'e aktar**: Dosya Detayı, Listeler ekranı ve Ayarlar > Yedekleme bölümünde Excel (.xlsx)
  indirme seçenekleri (ClosedXML kütüphanesi ile, sunucu tarafında, internet gerektirmez).
- **Yedekleme**: Uygulama her açıldığında `Backups` klasörüne otomatik `.db` yedeği alınır (son 20
  yedek saklanır). Ayrıca Ayarlar ekranından istenildiği an "Excel Yedek İndir" (tüm tablolar) veya
  "Veritabanı Dosyasını İndir (.db)" ile manuel yedek alınabilir.
- **Vergi Daireleri**: Artık İl + Vergi Dairesi Adı olarak veritabanında tutuluyor; Kayıt İşlemleri
  ekranında önce İl, sonra o ile ait vergi daireleri kademeli (cascading) combobox olarak geliyor.
  Türkiye genelinde ~300 vergi dairesi başlangıç listesi olarak yüklendi; Ayarlar ekranından
  "Türkiye Geneli Hazır Listeyi Yükle" butonuyla eksik olanlar her zaman tamamlanabilir, manuel
  ekleme/silme de yapılabilir.
- **Görsel**: Bootstrap Icons, Inter yazı tipi, gradyan menü çubuğu, ikonlu ana ekran kartları,
  kart gölgeleri/hover efektleri ile daha modern bir görünüm.

**Not:** PDF ve görsel kütüphaneler (Bootstrap Icons, Google Fonts, jsPDF, html2canvas) CDN
üzerinden yüklenir; uygulamayı çalıştırdığınız bilgisayarda internet bağlantısı olması gerekir.
Excel dışa aktarma ve otomatik yedekleme tamamen sunucu tarafında (ClosedXML ile) çalışır,
internet gerektirmez.

## Yeni Eklenenler (3. Sürüm)

- **Dosya No formatı**: Artık 4 haneli üretiliyor (ör. `2026/0001`).
- **Yazdırma başlığı**: Dosya Detayı yazdırma/PDF çıktısının en üstünde "ANKARA DEFTERDARLIĞI"
  başlığı, altında "Araç Kayıt Detayları" alt başlığı yer alıyor.
- **İhale Tutanağı**: Dosya Detayı ekranından "İhale Tutanağı" butonuyla açılan, başlığı
  "ANKARA DEFTERDARLIĞI" / "İhale Tutanağı" olan, komisyon imza alanları içeren ayrı bir
  yazdırılabilir/PDF'e aktarılabilir resmi belge sayfası eklendi.
- **Access (.accdb) Yedek**: Ayarlar > Veri Güvenliği bölümüne "Access (.accdb) Yedek Oluştur"
  butonu eklendi. **Önemli:** Bu özellik yalnızca Windows'ta ve bilgisayarda "Microsoft Access
  Database Engine" (ücretsiz Microsoft dağıtımı, Access kuruluysa zaten mevcuttur) kurulu
  olduğunda çalışır; çünkü `.accdb` formatı Microsoft'un ACE OLEDB sürücüsünü gerektirir - bu
  Claude'un veya .NET'in atlayabileceği bir kısıtlama değil, dosya formatının kendisinden
  kaynaklanıyor. Kurulu değilse anlaşılır bir hata mesajı gösterilir; alternatif olarak "Excel
  Yedek İndir" ile alınan dosya Access'te *Dış Veri Al > Excel* yoluyla kolayca içe aktarılabilir.
  Ayrı bir buton olan "SQLite Veritabanı Dosyasını İndir (.db)" ise uygulamanın kendi kullandığı
  ham veritabanı dosyasıdır (Access ile açılmaz, DB Browser for SQLite gibi araçlarla açılır).
