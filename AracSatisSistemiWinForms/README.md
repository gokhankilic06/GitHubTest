# Araç Satış Sistemi - Masaüstü (WinForms) Uygulaması

Bu, aynı sunucuya (AracSatisSistemi projesindeki Web API'ye) bağlanan bir
Windows masaüstü uygulamasıdır. Web/React arayüzüyle **aynı veritabanını**
kullanır - hangi bilgisayardan kayıt girilirse girilsin, Dosya No'lar
sırayla ve çakışmasız verilir.

## Önemli: Bu proje sadece Windows'ta derlenip çalışabilir

WinForms, Windows'a özgü bir arayüz teknolojisidir. Bu kod Linux
üzerinde (bu bulut ortamında) yazıldı ama derlenemedi/test edilemedi -
Windows'ta Visual Studio veya `dotnet build` ile derlemeniz gerekir.

## Gereksinimler

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (Visual
  Studio 2022 kuruluysa zaten gelir; "ASP.NET ve web geliştirme" ve
  ".NET masaüstü geliştirme" iş yüklerinin kurulu olduğundan emin olun)

## Çalıştırma

1. `AracSatisSistemi` (sunucu/API) projesini her zamanki gibi çalıştırın
   (`dotnet watch` veya `dotnet run`), ya da ortak sunucuya kurulmuşsa onun
   adresini bilin (ör. `http://192.168.1.50:5080`).
2. Bu klasörde (`AracSatisSistemiWinForms`):
   ```
   dotnet run
   ```
   veya Visual Studio'da `AracSatisSistemiWinForms.csproj` dosyasını açıp
   F5 ile çalıştırın.
3. Giriş ekranında "Sunucu Adresi" alanına API'nin adresini yazın (aynı
   bilgisayarda test ediyorsanız varsayılan `http://localhost:5080` zaten
   doğru olabilir; ağdaki ortak sunucuysa gerçek IP adresini yazın, ör.
   `http://192.168.1.50:5080`). Bu adres `api-ayarlari.json` dosyasına
   kaydedilir, bir daha yazmanıza gerek kalmaz.
4. Kullanıcı adı/şifre ile giriş yapın (web uygulamasındaki ile AYNI
   kullanıcı/şifre - örn. `admin` / `Admin123!`).

## Şu anki kapsam (1. aşama)

- Giriş / Çıkış
- Kayıt İşlemleri (yeni dosya oluşturma) - web formuyla birebir aynı
  alanlar, kademeli seçimler, plaka/VKN/TCKN doğrulama, teminat/damga
  vergisi anlık önizleme
- Dosya Ara / Listele
- Dosya Detayı (görüntüleme)

Diğer ekranlar (Satış Sonucu Gir, Alıcı Ödeme, Komisyon, Tutanak ve
Raporlar vb.) React arayüzüyle birlikte aşamalı olarak eklenecek.

## Derleme hatası/çalışma zamanı hatası alırsanız

Bu proje test edilememiş kodla teslim edildi - ilk denemede küçük bir
hata çıkması olası. Hata mesajının tamamını (veya ekran görüntüsünü)
paylaşırsanız hemen düzeltilir.
