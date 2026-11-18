namespace AracSatisSistemi.Models
{
    // Listelerde (resmi belgelerde DEĞİL) vergi dairesi adının kısa halini göstermek için
    // (ör. "Maltepe Vergi Dairesi Müdürlüğü" -> "Maltepe"). Resmi belgelerde (tutanak,
    // yazışma, dosya detayı vb.) tam ad kullanılmaya devam eder; bu yalnızca liste/tablo
    // görünümlerinde yer tasarrufu ve okunabilirlik için kullanılır.
    public static class VergiDairesiYardimcisi
    {
        private static readonly string[] Ekler =
        {
            " Vergi Dairesi Başkanlığı",
            " Vergi Dairesi Müdürlüğü",
            " Vergi Dairesi",
        };

        public static string KisaAd(string? tamAd)
        {
            if (string.IsNullOrWhiteSpace(tamAd)) return tamAd ?? string.Empty;
            foreach (var ek in Ekler)
            {
                if (tamAd.EndsWith(ek, StringComparison.OrdinalIgnoreCase))
                    return tamAd[..^ek.Length].TrimEnd();
            }
            return tamAd;
        }
    }
}
