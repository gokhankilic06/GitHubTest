using AracSatisSistemiWinForms.Servisler;

namespace AracSatisSistemiWinForms
{
    // Giriş yapıldıktan sonra tüm formların paylaştığı tek ApiClient örneği (token dahil).
    public static class Oturum
    {
        public static ApiClient Api { get; private set; } = new ApiClient(Ayarlar.ApiAdresiniOku());

        public static void YenidenOlustur()
        {
            Api = new ApiClient(Ayarlar.ApiAdresiniOku());
        }
    }
}
