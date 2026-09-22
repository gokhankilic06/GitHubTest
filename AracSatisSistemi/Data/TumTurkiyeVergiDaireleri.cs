using AracSatisSistemi.Models;

namespace AracSatisSistemi.Data
{
    // Türkiye genelindeki vergi dairelerinin başlangıç (seed) listesi.
    // "Güncellemeler" ekranından Ayarlar > Vergi Daireleri bölümünden
    // ekleme/silme yapılabilir; bu liste yalnızca ilk kurulumda kullanılır.
    public static class TumTurkiyeVergiDaireleri
    {
        public static readonly List<VergiDairesiKaydi> Liste = new()
        {
            // ADANA
            new() { Ad = "Adana İhtisas Vergi Dairesi Müdürlüğü", Sehir = "Adana" },
            new() { Ad = "Yüreğir Vergi Dairesi Müdürlüğü", Sehir = "Adana" },
            new() { Ad = "Seyhan Vergi Dairesi Müdürlüğü", Sehir = "Adana" },
            new() { Ad = "Ziyapaşa Vergi Dairesi Müdürlüğü", Sehir = "Adana" },
            new() { Ad = "Ceyhan Vergi Dairesi Müdürlüğü", Sehir = "Adana" },
            new() { Ad = "Kozan Vergi Dairesi Müdürlüğü", Sehir = "Adana" },
            new() { Ad = "Karataş Vergi Dairesi Müdürlüğü", Sehir = "Adana" },
            new() { Ad = "Beşocak Vergi Dairesi Müdürlüğü", Sehir = "Adana" },
            new() { Ad = "Çukurova Vergi Dairesi Müdürlüğü", Sehir = "Adana" },

            // ADIYAMAN
            new() { Ad = "Adıyaman Vergi Dairesi Müdürlüğü", Sehir = "Adıyaman" },
            new() { Ad = "Besni Vergi Dairesi Müdürlüğü", Sehir = "Adıyaman" },
            new() { Ad = "Kahta Vergi Dairesi Müdürlüğü", Sehir = "Adıyaman" },

            // AFYONKARAHİSAR
            new() { Ad = "Afyonkarahisar Kocatepe Vergi Dairesi Müdürlüğü", Sehir = "Afyonkarahisar" },
            new() { Ad = "Afyonkarahisar Tınaztepe Vergi Dairesi Müdürlüğü", Sehir = "Afyonkarahisar" },
            new() { Ad = "Bolvadin Vergi Dairesi Müdürlüğü", Sehir = "Afyonkarahisar" },
            new() { Ad = "Dinar Vergi Dairesi Müdürlüğü", Sehir = "Afyonkarahisar" },
            new() { Ad = "Sandıklı Vergi Dairesi Müdürlüğü", Sehir = "Afyonkarahisar" },
            new() { Ad = "Şuhut Vergi Dairesi Müdürlüğü", Sehir = "Afyonkarahisar" },

            // AĞRI
            new() { Ad = "Ağrı Vergi Dairesi Müdürlüğü", Sehir = "Ağrı" },
            new() { Ad = "Doğubayazıt Vergi Dairesi Müdürlüğü", Sehir = "Ağrı" },
            new() { Ad = "Patnos Vergi Dairesi Müdürlüğü", Sehir = "Ağrı" },

            // AMASYA
            new() { Ad = "Amasya Vergi Dairesi Müdürlüğü", Sehir = "Amasya" },
            new() { Ad = "Merzifon Vergi Dairesi Müdürlüğü", Sehir = "Amasya" },
            new() { Ad = "Suluova Vergi Dairesi Müdürlüğü", Sehir = "Amasya" },
            new() { Ad = "Taşova Vergi Dairesi Müdürlüğü", Sehir = "Amasya" },

            // ANKARA
            new() { Ad = "100. Yıl İhtisas Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Ankara Kurumlar Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Anadolu İhtisas Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Ankara İhtisas Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Akyurt Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Ayaş Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Bala Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Başkent Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Beypazarı Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Beytepe Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Cumhuriyet Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Çankaya Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Çubuk Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Dikimevi Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Dışkapı Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Doğanbey Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Elmadağ Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Etimesgut Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Gölbaşı Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Harbiye Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Haymana Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Hitit Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "İvedik Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Kahramankazan Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Kalecik Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Kavaklıdere Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Keçiören Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Kızılcahamam Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Kızılırmak Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Maltepe Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Mithatpaşa Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Muammer Aksoy Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Nallıhan Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Ostim Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Polatlı Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Seğmenler Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Sincan Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Şereflikoçhisar Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Ulus Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Veraset ve İntikal Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Yahya Galip Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Yeğenbey Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Yenimahalle Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },
            new() { Ad = "Yıldırım Beyazıt Vergi Dairesi Müdürlüğü", Sehir = "Ankara" },

            // ANTALYA
            new() { Ad = "Antalya İhtisas Vergi Dairesi Müdürlüğü", Sehir = "Antalya" },
            new() { Ad = "Üçkapılar Vergi Dairesi Müdürlüğü", Sehir = "Antalya" },
            new() { Ad = "Kalekapı Vergi Dairesi Müdürlüğü", Sehir = "Antalya" },
            new() { Ad = "Muratpaşa Vergi Dairesi Müdürlüğü", Sehir = "Antalya" },
            new() { Ad = "Düden Vergi Dairesi Müdürlüğü", Sehir = "Antalya" },
            new() { Ad = "Antalya Kurumlar Vergi Dairesi Müdürlüğü", Sehir = "Antalya" },
            new() { Ad = "Alanya Vergi Dairesi Müdürlüğü", Sehir = "Antalya" },
            new() { Ad = "Manavgat Vergi Dairesi Müdürlüğü", Sehir = "Antalya" },
            new() { Ad = "Serik Vergi Dairesi Müdürlüğü", Sehir = "Antalya" },
            new() { Ad = "Kumluca Vergi Dairesi Müdürlüğü", Sehir = "Antalya" },
            new() { Ad = "Kaş Vergi Dairesi Müdürlüğü", Sehir = "Antalya" },
            new() { Ad = "Kemer Vergi Dairesi Müdürlüğü", Sehir = "Antalya" },

            // ARTVİN
            new() { Ad = "Artvin Vergi Dairesi Müdürlüğü", Sehir = "Artvin" },
            new() { Ad = "Hopa Vergi Dairesi Müdürlüğü", Sehir = "Artvin" },

            // AYDIN
            new() { Ad = "Aydın Efeler Vergi Dairesi Müdürlüğü", Sehir = "Aydın" },
            new() { Ad = "Aydın Güzelhisar Vergi Dairesi Müdürlüğü", Sehir = "Aydın" },
            new() { Ad = "Germencik Vergi Dairesi Müdürlüğü", Sehir = "Aydın" },
            new() { Ad = "Nazilli Vergi Dairesi Müdürlüğü", Sehir = "Aydın" },
            new() { Ad = "Söke Vergi Dairesi Müdürlüğü", Sehir = "Aydın" },
            new() { Ad = "Söke Davutlar Vergi Dairesi Müdürlüğü", Sehir = "Aydın" },
            new() { Ad = "Kuşadası Vergi Dairesi Müdürlüğü", Sehir = "Aydın" },
            new() { Ad = "Didim Vergi Dairesi Müdürlüğü", Sehir = "Aydın" },

            // BALIKESİR
            new() { Ad = "Balıkesir Karesi Vergi Dairesi Müdürlüğü", Sehir = "Balıkesir" },
            new() { Ad = "Balıkesir Altıeylül Vergi Dairesi Müdürlüğü", Sehir = "Balıkesir" },
            new() { Ad = "Bandırma Vergi Dairesi Müdürlüğü", Sehir = "Balıkesir" },
            new() { Ad = "Edremit Vergi Dairesi Müdürlüğü", Sehir = "Balıkesir" },
            new() { Ad = "Edremit Akçay Vergi Dairesi Müdürlüğü", Sehir = "Balıkesir" },
            new() { Ad = "Erdek Vergi Dairesi Müdürlüğü", Sehir = "Balıkesir" },
            new() { Ad = "Ayvalık Vergi Dairesi Müdürlüğü", Sehir = "Balıkesir" },
            new() { Ad = "Gönen Vergi Dairesi Müdürlüğü", Sehir = "Balıkesir" },
            new() { Ad = "Burhaniye Vergi Dairesi Müdürlüğü", Sehir = "Balıkesir" },
            new() { Ad = "Susurluk Vergi Dairesi Müdürlüğü", Sehir = "Balıkesir" },

            // BİLECİK
            new() { Ad = "Bilecik Vergi Dairesi Müdürlüğü", Sehir = "Bilecik" },
            new() { Ad = "Bozüyük Vergi Dairesi Müdürlüğü", Sehir = "Bilecik" },

            // BİNGÖL
            new() { Ad = "Bingöl Vergi Dairesi Müdürlüğü", Sehir = "Bingöl" },

            // BİTLİS
            new() { Ad = "Bitlis Vergi Dairesi Müdürlüğü", Sehir = "Bitlis" },
            new() { Ad = "Tatvan Vergi Dairesi Müdürlüğü", Sehir = "Bitlis" },

            // BOLU
            new() { Ad = "Bolu Vergi Dairesi Müdürlüğü", Sehir = "Bolu" },
            new() { Ad = "Gerede Vergi Dairesi Müdürlüğü", Sehir = "Bolu" },
            new() { Ad = "Mengen Vergi Dairesi Müdürlüğü", Sehir = "Bolu" },

            // BURDUR
            new() { Ad = "Burdur Vergi Dairesi Müdürlüğü", Sehir = "Burdur" },
            new() { Ad = "Bucak Vergi Dairesi Müdürlüğü", Sehir = "Burdur" },

            // BURSA
            new() { Ad = "Bursa İhtisas Vergi Dairesi Müdürlüğü", Sehir = "Bursa" },
            new() { Ad = "Uludağ Vergi Dairesi Müdürlüğü", Sehir = "Bursa" },
            new() { Ad = "Setbaşı Vergi Dairesi Müdürlüğü", Sehir = "Bursa" },
            new() { Ad = "Osmangazi Vergi Dairesi Müdürlüğü", Sehir = "Bursa" },
            new() { Ad = "Yıldırım Vergi Dairesi Müdürlüğü", Sehir = "Bursa" },
            new() { Ad = "Nilüfer Vergi Dairesi Müdürlüğü", Sehir = "Bursa" },
            new() { Ad = "Ertuğrulgazi Vergi Dairesi Müdürlüğü", Sehir = "Bursa" },
            new() { Ad = "Gökdere Vergi Dairesi Müdürlüğü", Sehir = "Bursa" },
            new() { Ad = "Çekirge Vergi Dairesi Müdürlüğü", Sehir = "Bursa" },
            new() { Ad = "Gemlik Vergi Dairesi Müdürlüğü", Sehir = "Bursa" },
            new() { Ad = "İnegöl Vergi Dairesi Müdürlüğü", Sehir = "Bursa" },
            new() { Ad = "Mustafakemalpaşa Vergi Dairesi Müdürlüğü", Sehir = "Bursa" },
            new() { Ad = "Karacabey Vergi Dairesi Müdürlüğü", Sehir = "Bursa" },
            new() { Ad = "Mudanya Vergi Dairesi Müdürlüğü", Sehir = "Bursa" },
            new() { Ad = "Orhaneli Vergi Dairesi Müdürlüğü", Sehir = "Bursa" },
            new() { Ad = "Orhangazi Vergi Dairesi Müdürlüğü", Sehir = "Bursa" },

            // ÇANAKKALE
            new() { Ad = "Çanakkale Vergi Dairesi Müdürlüğü", Sehir = "Çanakkale" },
            new() { Ad = "Biga Vergi Dairesi Müdürlüğü", Sehir = "Çanakkale" },
            new() { Ad = "Biga İçdaş Vergi Dairesi Müdürlüğü", Sehir = "Çanakkale" },
            new() { Ad = "Gelibolu Vergi Dairesi Müdürlüğü", Sehir = "Çanakkale" },
            new() { Ad = "Gelibolu İhtisas Vergi Dairesi Müdürlüğü", Sehir = "Çanakkale" },
            new() { Ad = "Çan Vergi Dairesi Müdürlüğü", Sehir = "Çanakkale" },

            // ÇANKIRI
            new() { Ad = "Çankırı Vergi Dairesi Müdürlüğü", Sehir = "Çankırı" },

            // ÇORUM
            new() { Ad = "Çorum Mimar Sinan Vergi Dairesi Müdürlüğü", Sehir = "Çorum" },
            new() { Ad = "Çorum Hasan Paşa Vergi Dairesi Müdürlüğü", Sehir = "Çorum" },
            new() { Ad = "Osmancık Vergi Dairesi Müdürlüğü", Sehir = "Çorum" },
            new() { Ad = "Sungurlu Vergi Dairesi Müdürlüğü", Sehir = "Çorum" },

            // DENİZLİ
            new() { Ad = "Denizli İhtisas Vergi Dairesi Müdürlüğü", Sehir = "Denizli" },
            new() { Ad = "Pamukkale Vergi Dairesi Müdürlüğü", Sehir = "Denizli" },
            new() { Ad = "Gökpınar Vergi Dairesi Müdürlüğü", Sehir = "Denizli" },
            new() { Ad = "Honaz Vergi Dairesi Müdürlüğü", Sehir = "Denizli" },
            new() { Ad = "Saraylar Vergi Dairesi Müdürlüğü", Sehir = "Denizli" },
            new() { Ad = "Chivril Vergi Dairesi Müdürlüğü", Sehir = "Denizli" },

            // DİYARBAKIR
            new() { Ad = "Diyarbakır Gökalp Vergi Dairesi Müdürlüğü", Sehir = "Diyarbakır" },
            new() { Ad = "Diyarbakır Süleyman Nazif Vergi Dairesi Müdürlüğü", Sehir = "Diyarbakır" },
            new() { Ad = "Diyarbakır Cahit Sıtkı Tarancı Vergi Dairesi Müdürlüğü", Sehir = "Diyarbakır" },
            new() { Ad = "Ergani Vergi Dairesi Müdürlüğü", Sehir = "Diyarbakır" },
            new() { Ad = "Silvan Vergi Dairesi Müdürlüğü", Sehir = "Diyarbakır" },

            // EDİRNE
            new() { Ad = "Edirne Arda Vergi Dairesi Müdürlüğü", Sehir = "Edirne" },
            new() { Ad = "Edirne Kırkpınar Vergi Dairesi Müdürlüğü", Sehir = "Edirne" },
            new() { Ad = "Havsa Vergi Dairesi Müdürlüğü", Sehir = "Edirne" },
            new() { Ad = "Keşan Vergi Dairesi Müdürlüğü", Sehir = "Edirne" },
            new() { Ad = "Uzunköprü Vergi Dairesi Müdürlüğü", Sehir = "Edirne" },

            // ELAZIĞ
            new() { Ad = "Elazığ Harput Vergi Dairesi Müdürlüğü", Sehir = "Elazığ" },
            new() { Ad = "Elazığ Hazar Vergi Dairesi Müdürlüğü", Sehir = "Elazığ" },

            // ERZİNCAN
            new() { Ad = "Erzincan Vergi Dairesi Müdürlüğü", Sehir = "Erzincan" },

            // ERZURUM
            new() { Ad = "Erzurum Aziziye Vergi Dairesi Müdürlüğü", Sehir = "Erzurum" },
            new() { Ad = "Erzurum Palandöken Vergi Dairesi Müdürlüğü", Sehir = "Erzurum" },

            // ESKİŞEHİR
            new() { Ad = "Eskişehir İhtisas Vergi Dairesi Müdürlüğü", Sehir = "Eskişehir" },
            new() { Ad = "Eskişehir Yunus Emre Vergi Dairesi Müdürlüğü", Sehir = "Eskişehir" },
            new() { Ad = "Eskişehir 2 Eylül Vergi Dairesi Müdürlüğü", Sehir = "Eskişehir" },
            new() { Ad = "Eskişehir Taşbaşı Vergi Dairesi Müdürlüğü", Sehir = "Eskişehir" },
            new() { Ad = "Sivrihisar Vergi Dairesi Müdürlüğü", Sehir = "Eskişehir" },

            // GAZİANTEP
            new() { Ad = "Gaziantep İhtisas Vergi Dairesi Müdürlüğü", Sehir = "Gaziantep" },
            new() { Ad = "Şahinbey Vergi Dairesi Müdürlüğü", Sehir = "Gaziantep" },
            new() { Ad = "Şehitkamil Vergi Dairesi Müdürlüğü", Sehir = "Gaziantep" },
            new() { Ad = "Gazikent Vergi Dairesi Müdürlüğü", Sehir = "Gaziantep" },
            new() { Ad = "Suburcu Vergi Dairesi Müdürlüğü", Sehir = "Gaziantep" },
            new() { Ad = "Islahiye Vergi Dairesi Müdürlüğü", Sehir = "Gaziantep" },
            new() { Ad = "Nizip Vergi Dairesi Müdürlüğü", Sehir = "Gaziantep" },

            // GİRESUN
            new() { Ad = "Giresun Vergi Dairesi Müdürlüğü", Sehir = "Giresun" },
            new() { Ad = "Bulancak Vergi Dairesi Müdürlüğü", Sehir = "Giresun" },
            new() { Ad = "Görele Vergi Dairesi Müdürlüğü", Sehir = "Giresun" },

            // GÜMÜŞHANE
            new() { Ad = "Gümüşhane Vergi Dairesi Müdürlüğü", Sehir = "Gümüşhane" },

            // HAKKARİ
            new() { Ad = "Hakkari Vergi Dairesi Müdürlüğü", Sehir = "Hakkari" },
            new() { Ad = "Yüksekova Vergi Dairesi Müdürlüğü", Sehir = "Hakkari" },

            // HATAY
            new() { Ad = "Hatay Antakya Vergi Dairesi Müdürlüğü", Sehir = "Hatay" },
            new() { Ad = "Hatay 23 Temmuz Vergi Dairesi Müdürlüğü", Sehir = "Hatay" },
            new() { Ad = "İskenderun Sahil Vergi Dairesi Müdürlüğü", Sehir = "Hatay" },
            new() { Ad = "İskenderun Akdeniz Vergi Dairesi Müdürlüğü", Sehir = "Hatay" },
            new() { Ad = "Dörtyol Vergi Dairesi Müdürlüğü", Sehir = "Hatay" },
            new() { Ad = "Reyhanlı Vergi Dairesi Müdürlüğü", Sehir = "Hatay" },
            new() { Ad = "Kırıkhan Vergi Dairesi Müdürlüğü", Sehir = "Hatay" },
            new() { Ad = "Samandağ Vergi Dairesi Müdürlüğü", Sehir = "Hatay" },

            // ISPARTA
            new() { Ad = "Isparta Kaymakkapı Vergi Dairesi Müdürlüğü", Sehir = "Isparta" },
            new() { Ad = "Isparta Davraz Vergi Dairesi Müdürlüğü", Sehir = "Isparta" },
            new() { Ad = "Eğirdir Vergi Dairesi Müdürlüğü", Sehir = "Isparta" },
            new() { Ad = "Yalvaç Vergi Dairesi Müdürlüğü", Sehir = "Isparta" },

            // MERSİN
            new() { Ad = "Mersin İhtisas Vergi Dairesi Müdürlüğü", Sehir = "Mersin" },
            new() { Ad = "Mersin İstiklal Vergi Dairesi Müdürlüğü", Sehir = "Mersin" },
            new() { Ad = "Mersin Uray Vergi Dairesi Müdürlüğü", Sehir = "Mersin" },
            new() { Ad = "Mersin Liman Vergi Dairesi Müdürlüğü", Sehir = "Mersin" },
            new() { Ad = "Mersin Toros Vergi Dairesi Müdürlüğü", Sehir = "Mersin" },
            new() { Ad = "Mut Vergi Dairesi Müdürlüğü", Sehir = "Mersin" },
            new() { Ad = "Tarsus Vergi Dairesi Müdürlüğü", Sehir = "Mersin" },
            new() { Ad = "Silifke Vergi Dairesi Müdürlüğü", Sehir = "Mersin" },
            new() { Ad = "Erdemli Vergi Dairesi Müdürlüğü", Sehir = "Mersin" },
            new() { Ad = "Anamur Vergi Dairesi Müdürlüğü", Sehir = "Mersin" },

            // İSTANBUL
            new() { Ad = "İstanbul Büyük Mükellefler Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "İstanbul İhtisas Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Anadolu Kurumlar Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Boğaziçi Kurumlar Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Marmara Kurumlar Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Adalar Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Arnavutköy Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Atışalanı Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Avcılar Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Bahçelievler Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Bakırköy Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Bayrampaşa Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Beşiktaş Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Beykoz Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Beylikdüzü Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Beyoğlu Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Büyükçekmece Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Cisri Ergene Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Çatalca Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Davutpaşa Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Dış Ticaret Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Esenler Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Esenyurt Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Eyüpsultan Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Fatih Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Gaziosmanpaşa Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Göztepe Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Güneşli Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Haliç Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Hocapaşa Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "İkitelli Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Kadıköy Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Kağıthane Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Kartal Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Kasımpaşalılar Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Kocasinan Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Küçükyalı Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Laleli Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Mecidiyeköy Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Merter Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Mevlanakapı Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Nakil Vasıtaları Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Pendik Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Rami Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Rıhtım Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Sancaktepe Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Sarıyer Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Silivri Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Sultanbeyli Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Süleymaniye Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Şile Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Şişli Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Tuzla Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Ümraniye Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Üsküdar Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Vatan Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Yakacık Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Yeditepe Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },
            new() { Ad = "Zeytinburnu Vergi Dairesi Müdürlüğü", Sehir = "İstanbul" },

            // İZMİR
            new() { Ad = "İzmir İhtisas Vergi Dairesi Müdürlüğü", Sehir = "İzmir" },
            new() { Ad = "İzmir Kurumlar Vergi Dairesi Müdürlüğü", Sehir = "İzmir" },
            new() { Ad = "Alsancak Vergi Dairesi Müdürlüğü", Sehir = "İzmir" },
            new() { Ad = "Bayındır Vergi Dairesi Müdürlüğü", Sehir = "İzmir" },
            new() { Ad = "Belkahve Vergi Dairesi Müdürlüğü", Sehir = "İzmir" },
            new() { Ad = "Bornova Vergi Dairesi Müdürlüğü", Sehir = "İzmir" },
            new() { Ad = "Çakabey Vergi Dairesi Müdürlüğü", Sehir = "İzmir" },
            new() { Ad = "Dokuz Eylül Vergi Dairesi Müdürlüğü", Sehir = "İzmir" },
            new() { Ad = "Ege Vergi Dairesi Müdürlüğü", Sehir = "İzmir" },
            new() { Ad = "Hasan Tahsin Vergi Dairesi Müdürlüğü", Sehir = "İzmir" },
            new() { Ad = "Kadifekale Vergi Dairesi Müdürlüğü", Sehir = "İzmir" },
            new() { Ad = "Karşıyaka Vergi Dairesi Müdürlüğü", Sehir = "İzmir" },
            new() { Ad = "Kordon Vergi Dairesi Müdürlüğü", Sehir = "İzmir" },
            new() { Ad = "Seferihisar Vergi Dairesi Müdürlüğü", Sehir = "İzmir" },
            new() { Ad = "Yamanlar Vergi Dairesi Müdürlüğü", Sehir = "İzmir" },
            new() { Ad = "Taşıtlar Vergi Dairesi Müdürlüğü", Sehir = "İzmir" },
            new() { Ad = "Şirinyer Vergi Dairesi Müdürlüğü", Sehir = "İzmir" },
            new() { Ad = "Konak Vergi Dairesi Müdürlüğü", Sehir = "İzmir" },
            new() { Ad = "Balçova Vergi Dairesi Müdürlüğü", Sehir = "İzmir" },
            new() { Ad = "Gaziemir Vergi Dairesi Müdürlüğü", Sehir = "İzmir" },
            new() { Ad = "Bergama Vergi Dairesi Müdürlüğü", Sehir = "İzmir" },
            new() { Ad = "Ödemiş Vergi Dairesi Müdürlüğü", Sehir = "İzmir" },
            new() { Ad = "Tire Vergi Dairesi Müdürlüğü", Sehir = "İzmir" },
            new() { Ad = "Torbalı Vergi Dairesi Müdürlüğü", Sehir = "İzmir" },
            new() { Ad = "Menemen Vergi Dairesi Müdürlüğü", Sehir = "İzmir" },
            new() { Ad = "Urla Vergi Dairesi Müdürlüğü", Sehir = "İzmir" },
            new() { Ad = "Çeşme Vergi Dairesi Müdürlüğü", Sehir = "İzmir" },
            new() { Ad = "Aliağa Vergi Dairesi Müdürlüğü", Sehir = "İzmir" },
            new() { Ad = "Menderes Vergi Dairesi Müdürlüğü", Sehir = "İzmir" },
            new() { Ad = "Kemalpaşa Vergi Dairesi Müdürlüğü", Sehir = "İzmir" },

            // KARS
            new() { Ad = "Kars Vergi Dairesi Müdürlüğü", Sehir = "Kars" },

            // KASTAMONU
            new() { Ad = "Kastamonu Vergi Dairesi Müdürlüğü", Sehir = "Kastamonu" },
            new() { Ad = "Tosya Vergi Dairesi Müdürlüğü", Sehir = "Kastamonu" },

            // KAYSERİ
            new() { Ad = "Kayseri İhtisas Vergi Dairesi Müdürlüğü", Sehir = "Kayseri" },
            new() { Ad = "Kayseri Erciyes Vergi Dairesi Müdürlüğü", Sehir = "Kayseri" },
            new() { Ad = "Kayseri Mimarsinan Vergi Dairesi Müdürlüğü", Sehir = "Kayseri" },
            new() { Ad = "Kayseri Gevher Nesibe Vergi Dairesi Müdürlüğü", Sehir = "Kayseri" },
            new() { Ad = "Develi Vergi Dairesi Müdürlüğü", Sehir = "Kayseri" },

            // KIRKLARELİ
            new() { Ad = "Kırklareli Vergi Dairesi Müdürlüğü", Sehir = "Kırklareli" },
            new() { Ad = "Lüleburgaz Vergi Dairesi Müdürlüğü", Sehir = "Kırklareli" },
            new() { Ad = "Babaeski Vergi Dairesi Müdürlüğü", Sehir = "Kırklareli" },

            // KIRŞEHİR
            new() { Ad = "Kırşehir Vergi Dairesi Müdürlüğü", Sehir = "Kırşehir" },

            // KOCAELİ
            new() { Ad = "Kocaeli İhtisas Vergi Dairesi Müdürlüğü", Sehir = "Kocaeli" },
            new() { Ad = "Kocaeli Alemdar Vergi Dairesi Müdürlüğü", Sehir = "Kocaeli" },
            new() { Ad = "Kocaeli Tepecik Vergi Dairesi Müdürlüğü", Sehir = "Kocaeli" },
            new() { Ad = "Gebze Vergi Dairesi Müdürlüğü", Sehir = "Kocaeli" },
            new() { Ad = "Ihtisas Gebze Vergi Dairesi Müdürlüğü", Sehir = "Kocaeli" },
            new() { Ad = "Uluçınar Vergi Dairesi Müdürlüğü", Sehir = "Kocaeli" },
            new() { Ad = "İlyasbey Vergi Dairesi Müdürlüğü", Sehir = "Kocaeli" },
            new() { Ad = "Kartepe Vergi Dairesi Müdürlüğü", Sehir = "Kocaeli" },
            new() { Ad = "Körfez Vergi Dairesi Müdürlüğü", Sehir = "Kocaeli" },
            new() { Ad = "Gölcük Vergi Dairesi Müdürlüğü", Sehir = "Kocaeli" },
            new() { Ad = "Karamürsel Vergi Dairesi Müdürlüğü", Sehir = "Kocaeli" },

            // KONYA
            new() { Ad = "Konya İhtisas Vergi Dairesi Müdürlüğü", Sehir = "Konya" },
            new() { Ad = "Konya Mevlana Vergi Dairesi Müdürlüğü", Sehir = "Konya" },
            new() { Ad = "Konya Selçuk Vergi Dairesi Müdürlüğü", Sehir = "Konya" },
            new() { Ad = "Konya Meram Vergi Dairesi Müdürlüğü", Sehir = "Konya" },
            new() { Ad = "Konya Alaaddin Vergi Dairesi Müdürlüğü", Sehir = "Konya" },
            new() { Ad = "Ereğli Vergi Dairesi Müdürlüğü", Sehir = "Konya" },
            new() { Ad = "Akşehir Vergi Dairesi Müdürlüğü", Sehir = "Konya" },
            new() { Ad = "Karapınar Vergi Dairesi Müdürlüğü", Sehir = "Konya" },
            new() { Ad = "Seydişehir Vergi Dairesi Müdürlüğü", Sehir = "Konya" },
            new() { Ad = "Cihanbeyli Vergi Dairesi Müdürlüğü", Sehir = "Konya" },

            // KÜTAHYA
            new() { Ad = "Kütahya 30 Ağustos Vergi Dairesi Müdürlüğü", Sehir = "Kütahya" },
            new() { Ad = "Kütahya Çınılı Vergi Dairesi Müdürlüğü", Sehir = "Kütahya" },
            new() { Ad = "Tavşanlı Vergi Dairesi Müdürlüğü", Sehir = "Kütahya" },
            new() { Ad = "Gediz Vergi Dairesi Müdürlüğü", Sehir = "Kütahya" },

            // MALATYA
            new() { Ad = "Malatya Fırat Vergi Dairesi Müdürlüğü", Sehir = "Malatya" },
            new() { Ad = "Malatya Beydağı Vergi Dairesi Müdürlüğü", Sehir = "Malatya" },

            // MANİSA
            new() { Ad = "Manisa Mesir Vergi Dairesi Müdürlüğü", Sehir = "Manisa" },
            new() { Ad = "Manisa Alaybey Vergi Dairesi Müdürlüğü", Sehir = "Manisa" },
            new() { Ad = "Akhisar Vergi Dairesi Müdürlüğü", Sehir = "Manisa" },
            new() { Ad = "Kırkağaç Vergi Dairesi Müdürlüğü", Sehir = "Manisa" },
            new() { Ad = "Turgutlu Vergi Dairesi Müdürlüğü", Sehir = "Manisa" },
            new() { Ad = "Salihli Vergi Dairesi Müdürlüğü", Sehir = "Manisa" },
            new() { Ad = "Soma Vergi Dairesi Müdürlüğü", Sehir = "Manisa" },
            new() { Ad = "Alaşehir Vergi Dairesi Müdürlüğü", Sehir = "Manisa" },

            // KAHRAMANMARAŞ
            new() { Ad = "Kahramanmaraş Aksu Vergi Dairesi Müdürlüğü", Sehir = "Kahramanmaraş" },
            new() { Ad = "Kahramanmaraş Erkenez Vergi Dairesi Müdürlüğü", Sehir = "Kahramanmaraş" },
            new() { Ad = "Elbistan Vergi Dairesi Müdürlüğü", Sehir = "Kahramanmaraş" },
            new() { Ad = "Afşin Vergi Dairesi Müdürlüğü", Sehir = "Kahramanmaraş" },
            new() { Ad = "Pazarcık Vergi Dairesi Müdürlüğü", Sehir = "Kahramanmaraş" },

            // MARDİN
            new() { Ad = "Mardin Vergi Dairesi Müdürlüğü", Sehir = "Mardin" },
            new() { Ad = "Kızıltepe Vergi Dairesi Müdürlüğü", Sehir = "Mardin" },
            new() { Ad = "Nusaybin Vergi Dairesi Müdürlüğü", Sehir = "Mardin" },

            // MUĞLA
            new() { Ad = "Muğla Vergi Dairesi Müdürlüğü", Sehir = "Muğla" },
            new() { Ad = "Bodrum Vergi Dairesi Müdürlüğü", Sehir = "Muğla" },
            new() { Ad = "Fethiye Vergi Dairesi Müdürlüğü", Sehir = "Muğla" },
            new() { Ad = "Marmaris Vergi Dairesi Müdürlüğü", Sehir = "Muğla" },
            new() { Ad = "Milas Vergi Dairesi Müdürlüğü", Sehir = "Muğla" },
            new() { Ad = "Ortaca Vergi Dairesi Müdürlüğü", Sehir = "Muğla" },
            new() { Ad = "Yatağan Vergi Dairesi Müdürlüğü", Sehir = "Muğla" },

            // MUŞ
            new() { Ad = "Muş Vergi Dairesi Müdürlüğü", Sehir = "Muş" },

            // NEVŞEHİR
            new() { Ad = "Nevşehir Kapadokya Vergi Dairesi Müdürlüğü", Sehir = "Nevşehir" },

            // NİĞDE
            new() { Ad = "Niğde Vergi Dairesi Müdürlüğü", Sehir = "Niğde" },
            new() { Ad = "Bor Vergi Dairesi Müdürlüğü", Sehir = "Niğde" },

            // ORDU
            new() { Ad = "Ordu Boztepe Vergi Dairesi Müdürlüğü", Sehir = "Ordu" },
            new() { Ad = "Ünye Vergi Dairesi Müdürlüğü", Sehir = "Ordu" },
            new() { Ad = "Fatsa Vergi Dairesi Müdürlüğü", Sehir = "Ordu" },

            // RİZE
            new() { Ad = "Rize Yeşilçay Vergi Dairesi Müdürlüğü", Sehir = "Rize" },
            new() { Ad = "Rize Kaçkar Vergi Dairesi Müdürlüğü", Sehir = "Rize" },
            new() { Ad = "Çayeli Vergi Dairesi Müdürlüğü", Sehir = "Rize" },
            new() { Ad = "Ardeşen Vergi Dairesi Müdürlüğü", Sehir = "Rize" },

            // SAKARYA
            new() { Ad = "Sakarya Ali Fuat Cebesoy Vergi Dairesi Müdürlüğü", Sehir = "Sakarya" },
            new() { Ad = "Sakarya Gümrükönü Vergi Dairesi Müdürlüğü", Sehir = "Sakarya" },
            new() { Ad = "Hendek Vergi Dairesi Müdürlüğü", Sehir = "Sakarya" },
            new() { Ad = "Akyazı Vergi Dairesi Müdürlüğü", Sehir = "Sakarya" },
            new() { Ad = "Geyve Vergi Dairesi Müdürlüğü", Sehir = "Sakarya" },
            new() { Ad = "Karasu Vergi Dairesi Müdürlüğü", Sehir = "Sakarya" },
            new() { Ad = "Kocaali Vergi Dairesi Müdürlüğü", Sehir = "Sakarya" },

            // SAMSUN
            new() { Ad = "Samsun 19 Mayıs Vergi Dairesi Müdürlüğü", Sehir = "Samsun" },
            new() { Ad = "Samsun Gaziler Vergi Dairesi Müdürlüğü", Sehir = "Samsun" },
            new() { Ad = "Bafra Vergi Dairesi Müdürlüğü", Sehir = "Samsun" },
            new() { Ad = "Çarşamba Vergi Dairesi Müdürlüğü", Sehir = "Samsun" },
            new() { Ad = "Havza Vergi Dairesi Müdürlüğü", Sehir = "Samsun" },
            new() { Ad = "Terme Vergi Dairesi Müdürlüğü", Sehir = "Samsun" },

            // SİİRT
            new() { Ad = "Siirt Vergi Dairesi Müdürlüğü", Sehir = "Siirt" },

            // SİNOP
            new() { Ad = "Sinop Vergi Dairesi Müdürlüğü", Sehir = "Sinop" },
            new() { Ad = "Boyabat Vergi Dairesi Müdürlüğü", Sehir = "Sinop" },

            // SİVAS
            new() { Ad = "Sivas Kale Vergi Dairesi Müdürlüğü", Sehir = "Sivas" },
            new() { Ad = "Sivas Siteler Vergi Dairesi Müdürlüğü", Sehir = "Sivas" },
            new() { Ad = "Şarkışla Vergi Dairesi Müdürlüğü", Sehir = "Sivas" },

            // TEKİRDAĞ
            new() { Ad = "Tekirdağ Süleymanpaşa Vergi Dairesi Müdürlüğü", Sehir = "Tekirdağ" },
            new() { Ad = "Tekirdağ Namık Kemal Vergi Dairesi Müdürlüğü", Sehir = "Tekirdağ" },
            new() { Ad = "Çorlu Vergi Dairesi Müdürlüğü", Sehir = "Tekirdağ" },
            new() { Ad = "İhtisas Çorlu Vergi Dairesi Müdürlüğü", Sehir = "Tekirdağ" },
            new() { Ad = "Çerkezköy Vergi Dairesi Müdürlüğü", Sehir = "Tekirdağ" },
            new() { Ad = "Hayrabolu Vergi Dairesi Müdürlüğü", Sehir = "Tekirdağ" },
            new() { Ad = "Kapaklı Vergi Dairesi Müdürlüğü", Sehir = "Tekirdağ" },
            new() { Ad = "Malkara Vergi Dairesi Müdürlüğü", Sehir = "Tekirdağ" },

            // TOKAT
            new() { Ad = "Tokat Gaziosmanpaşa Vergi Dairesi Müdürlüğü", Sehir = "Tokat" },
            new() { Ad = "Erbaa Vergi Dairesi Müdürlüğü", Sehir = "Tokat" },
            new() { Ad = "Turhal Vergi Dairesi Müdürlüğü", Sehir = "Tokat" },
            new() { Ad = "Niksar Vergi Dairesi Müdürlüğü", Sehir = "Tokat" },
            new() { Ad = "Zile Vergi Dairesi Müdürlüğü", Sehir = "Tokat" },

            // TRABZON
            new() { Ad = "Trabzon Karadeniz Vergi Dairesi Müdürlüğü", Sehir = "Trabzon" },
            new() { Ad = "Trabzon Hızırbey Vergi Dairesi Müdürlüğü", Sehir = "Trabzon" },
            new() { Ad = "Akçaabat Vergi Dairesi Müdürlüğü", Sehir = "Trabzon" },

            // TUNCELİ
            new() { Ad = "Tunceli Vergi Dairesi Müdürlüğü", Sehir = "Tunceli" },

            // ŞANLIURFA
            new() { Ad = "Şanlıurfa Topçu Meydanı Vergi Dairesi Müdürlüğü", Sehir = "Şanlıurfa" },
            new() { Ad = "Şanlıurfa Göbeklitepe Vergi Dairesi Müdürlüğü", Sehir = "Şanlıurfa" },
            new() { Ad = "Siverek Vergi Dairesi Müdürlüğü", Sehir = "Şanlıurfa" },
            new() { Ad = "Viranşehir Vergi Dairesi Müdürlüğü", Sehir = "Şanlıurfa" },
            new() { Ad = "Birecik Vergi Dairesi Müdürlüğü", Sehir = "Şanlıurfa" },

            // UŞAK
            new() { Ad = "Uşak Uşak Vergi Dairesi Müdürlüğü", Sehir = "Uşak" },
            new() { Ad = "Uşak Banaz Vergi Dairesi Müdürlüğü", Sehir = "Uşak" },

            // VAN
            new() { Ad = "Van İpekyolu Vergi Dairesi Müdürlüğü", Sehir = "Van" },
            new() { Ad = "Van Tuşba Vergi Dairesi Müdürlüğü", Sehir = "Van" },
            new() { Ad = "Erciş Vergi Dairesi Müdürlüğü", Sehir = "Van" },

            // YOZGAT
            new() { Ad = "Yozgat Vergi Dairesi Müdürlüğü", Sehir = "Yozgat" },
            new() { Ad = "Sorgun Vergi Dairesi Müdürlüğü", Sehir = "Yozgat" },
            new() { Ad = "Boğazlıyan Vergi Dairesi Müdürlüğü", Sehir = "Yozgat" },

            // ZONGULDAK
            new() { Ad = "Zonguldak Karaelmas Vergi Dairesi Müdürlüğü", Sehir = "Zonguldak" },
            new() { Ad = "Zonguldak Uzun Mehmet Vergi Dairesi Müdürlüğü", Sehir = "Zonguldak" },
            new() { Ad = "Çaycuma Vergi Dairesi Müdürlüğü", Sehir = "Zonguldak" },
            new() { Ad = "Ereğli Vergi Dairesi Müdürlüğü", Sehir = "Zonguldak" },
            new() { Ad = "Devrek Vergi Dairesi Müdürlüğü", Sehir = "Zonguldak" },

            // DİĞER İLLER
            new() { Ad = "Aksaray Aksaray Vergi Dairesi Müdürlüğü", Sehir = "Aksaray" },
            new() { Ad = "Aksaray Hasan Dağı Vergi Dairesi Müdürlüğü", Sehir = "Aksaray" },
            new() { Ad = "Bayburt Vergi Dairesi Müdürlüğü", Sehir = "Bayburt" },
            new() { Ad = "Karaman Vergi Dairesi Müdürlüğü", Sehir = "Karaman" },
            new() { Ad = "Kırıkkale Kaletepe Vergi Dairesi Müdürlüğü", Sehir = "Kırıkkale" },
            new() { Ad = "Kırıkkale Irmak Vergi Dairesi Müdürlüğü", Sehir = "Kırıkkale" },
            new() { Ad = "Batman Vergi Dairesi Müdürlüğü", Sehir = "Batman" },
            new() { Ad = "Şırnak Vergi Dairesi Müdürlüğü", Sehir = "Şırnak" },
            new() { Ad = "Cizre Vergi Dairesi Müdürlüğü", Sehir = "Şırnak" },
            new() { Ad = "Silopi Vergi Dairesi Müdürlüğü", Sehir = "Şırnak" },
            new() { Ad = "Bartın Vergi Dairesi Müdürlüğü", Sehir = "Bartın" },
            new() { Ad = "Ardahan Vergi Dairesi Müdürlüğü", Sehir = "Ardahan" },
            new() { Ad = "Iğdır Vergi Dairesi Müdürlüğü", Sehir = "Iğdır" },
            new() { Ad = "Yalova Vergi Dairesi Müdürlüğü", Sehir = "Yalova" },
            new() { Ad = "Karabük Vergi Dairesi Müdürlüğü", Sehir = "Karabük" },
            new() { Ad = "Safranbolu Vergi Dairesi Müdürlüğü", Sehir = "Karabük" },
            new() { Ad = "Kilis Vergi Dairesi Müdürlüğü", Sehir = "Kilis" },
            new() { Ad = "Osmaniye Vergi Dairesi Müdürlüğü", Sehir = "Osmaniye" },
            new() { Ad = "Kadirli Vergi Dairesi Müdürlüğü", Sehir = "Osmaniye" },
            new() { Ad = "Düzce Düzce Vergi Dairesi Müdürlüğü", Sehir = "Düzce" },
            new() { Ad = "Akçakoca Vergi Dairesi Müdürlüğü", Sehir = "Düzce" }
        };
    }
}
