# Riba Veli Anket Sistemi

Yavuz Sultan Selim İlkokulu için iki seçenekli (A/B) veli anketi ve yönetici paneli.
Aynı sistemin iki bağımsız sürümü vardır. İkisinden birini seçip kullanmanız yeterlidir.

| | [Node.js sürümü](node-app/) | [Python / Django sürümü](django-app/) |
|---|---|---|
| Çalıştırma | `npm install && npm start` | `pip install -r requirements.txt && python manage.py runserver` |
| Veritabanı | SQLite veya Supabase (`DATABASE_URL`) | SQLite veya Supabase (`DATABASE_URL`) |
| Yönetici girişi | Tek şifre (`ADMIN_PASSWORD`) | Kullanıcı adı + şifre, birden fazla yönetici |
| Soruları düzenleme | `questions.js` dosyasından | Tarayıcıdan (Django admin) |
| Tek kullanımlık veli kodları | ✓ | ✓ |
| Sınıf filtresi, grafikler, Excel | ✓ | ✓ |
| Kod kartı yazdırma | ✓ | ✓ |

**Hangisini seçmeli?**
- Soruları ileride sık değiştirecekseniz veya birden fazla öğretmen panele girecekse **Django** sürümünü seçin.
- En sade kurulum için **Node.js** sürümünü seçin.

**Windows'ta hızlı başlangıç:** Node.js LTS'i kurun (https://nodejs.org), sonra bu klasördeki **`BASLAT.bat`** dosyasına çift tıklayın.

Kurulum ve yayına alma adımları her klasörün kendi README dosyasındadır.
