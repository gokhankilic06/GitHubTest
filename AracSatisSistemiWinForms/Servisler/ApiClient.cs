using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AracSatisSistemiWinForms.Modeller;

namespace AracSatisSistemiWinForms.Servisler
{
    // Sunucudaki aynı JSON API'yi (React arayüzünün kullandığı) HttpClient ile konuşan
    // basit istemci. Oturum boyunca tek bir örneği (Oturum.Api) kullanılır.
    public class ApiClient
    {
        private static readonly JsonSerializerOptions JsonAyarlari = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _http;

        public string? Token { get; private set; }
        public string? KullaniciAdiSoyad { get; private set; }

        public ApiClient(string apiAdresi)
        {
            _http = new HttpClient { BaseAddress = new Uri(apiAdresi), Timeout = TimeSpan.FromSeconds(30) };
        }

        public async Task GirisYapAsync(string kullaniciAdi, string sifre)
        {
            var icerik = new StringContent(
                JsonSerializer.Serialize(new { kullaniciAdi, sifre }, JsonAyarlari),
                Encoding.UTF8, "application/json");

            HttpResponseMessage yanit;
            try
            {
                yanit = await _http.PostAsync("/api/auth/giris", icerik);
            }
            catch (Exception ex)
            {
                throw new ApiHatasi($"Sunucuya bağlanılamadı ({_http.BaseAddress}). Sunucu adresi/ağ bağlantınızı kontrol edin.\n\nTeknik detay: {ex.Message}");
            }

            if (yanit.StatusCode == HttpStatusCode.Unauthorized)
                throw new ApiHatasi("Kullanıcı adı veya şifre hatalı.");
            if (!yanit.IsSuccessStatusCode)
                throw new ApiHatasi($"Giriş başarısız (sunucu kodu: {(int)yanit.StatusCode}).");

            var govde = await yanit.Content.ReadAsStringAsync();
            var sonuc = JsonSerializer.Deserialize<GirisYaniti>(govde, JsonAyarlari)
                ?? throw new ApiHatasi("Sunucudan beklenmeyen bir yanıt geldi.");

            Token = sonuc.Token;
            KullaniciAdiSoyad = sonuc.AdSoyad;
        }

        public void CikisYap()
        {
            Token = null;
            KullaniciAdiSoyad = null;
        }

        public async Task<T> GetAsync<T>(string yol)
        {
            var istek = new HttpRequestMessage(HttpMethod.Get, yol);
            TokenEkle(istek);
            return await GonderVeOkuAsync<T>(istek);
        }

        public async Task<T> PostAsync<T>(string yol, object gonderilecek)
        {
            var istek = new HttpRequestMessage(HttpMethod.Post, yol)
            {
                Content = new StringContent(JsonSerializer.Serialize(gonderilecek, JsonAyarlari), Encoding.UTF8, "application/json")
            };
            TokenEkle(istek);
            return await GonderVeOkuAsync<T>(istek);
        }

        public async Task<T> PutAsync<T>(string yol, object gonderilecek)
        {
            var istek = new HttpRequestMessage(HttpMethod.Put, yol)
            {
                Content = new StringContent(JsonSerializer.Serialize(gonderilecek, JsonAyarlari), Encoding.UTF8, "application/json")
            };
            TokenEkle(istek);
            return await GonderVeOkuAsync<T>(istek);
        }

        private void TokenEkle(HttpRequestMessage istek)
        {
            if (!string.IsNullOrEmpty(Token))
                istek.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        }

        private async Task<T> GonderVeOkuAsync<T>(HttpRequestMessage istek)
        {
            HttpResponseMessage yanit;
            try
            {
                yanit = await _http.SendAsync(istek);
            }
            catch (Exception ex)
            {
                throw new ApiHatasi($"Sunucuya bağlanılamadı. Ağ bağlantınızı kontrol edin.\n\nTeknik detay: {ex.Message}");
            }

            var govde = await yanit.Content.ReadAsStringAsync();

            if (!yanit.IsSuccessStatusCode)
            {
                if (yanit.StatusCode == HttpStatusCode.Unauthorized)
                    throw new OturumSuresiDolduHatasi();

                if (yanit.StatusCode == HttpStatusCode.BadRequest)
                {
                    try
                    {
                        var detay = JsonSerializer.Deserialize<DogrulamaHatasiYaniti>(govde, JsonAyarlari);
                        if (detay?.Errors != null && detay.Errors.Count > 0)
                        {
                            var mesajlar = detay.Errors.SelectMany(e => e.Value);
                            throw new ApiHatasi(string.Join("\n", mesajlar));
                        }
                    }
                    catch (JsonException)
                    {
                        // Beklenen formatta değilse aşağıdaki genel hataya düşülür.
                    }
                }

                throw new ApiHatasi($"Sunucu hatası (kod: {(int)yanit.StatusCode}):\n{govde}");
            }

            if (typeof(T) == typeof(object) && string.IsNullOrWhiteSpace(govde))
                return default!;

            return JsonSerializer.Deserialize<T>(govde, JsonAyarlari)!;
        }

        private class DogrulamaHatasiYaniti
        {
            public Dictionary<string, List<string>>? Errors { get; set; }
        }
    }

    public class ApiHatasi : Exception
    {
        public ApiHatasi(string mesaj) : base(mesaj) { }
    }

    // Token süresi dolduğunda/geçersiz olduğunda fırlatılır - ekranlar bunu yakalayıp
    // kullanıcıyı Giriş ekranına geri gönderir.
    public class OturumSuresiDolduHatasi : Exception
    {
        public OturumSuresiDolduHatasi() : base("Oturumunuzun süresi dolmuş. Lütfen tekrar giriş yapın.") { }
    }
}
