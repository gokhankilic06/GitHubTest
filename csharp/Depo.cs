using System.Security.Cryptography;
using Microsoft.Data.Sqlite;

namespace VerasetRandevu;

public record Randevu(
    long Id,
    string RandevuNo,
    string Ilce,
    string Tarih,
    string Saat,
    string VefatEdenAd,
    string VefatEdenSoyad,
    string VefatEdenTc,
    string BasvuranAd,
    string BasvuranSoyad,
    int BasvuranDogumYili,
    string BasvuranTc,
    string BasvuranCep,
    string Durum,
    string Olusturma);

public record Cakisma(string Alan, string Mesaj);

public class Depo
{
    private const string NoKarakterleri = "ABCDEFGHJKLMNPRSTUVYZ23456789";
    private readonly string _baglantiMetni;

    // Kapasite kontrolü ile ekleme arasının başka bir istek tarafından bölünmemesi için.
    private readonly object _yazmaKilidi = new();

    public Depo(string dbYolu, string semaYolu)
    {
        var klasor = Path.GetDirectoryName(Path.GetFullPath(dbYolu));
        if (!string.IsNullOrEmpty(klasor)) Directory.CreateDirectory(klasor);
        _baglantiMetni = new SqliteConnectionStringBuilder { DataSource = dbYolu, DefaultTimeout = 5 }.ToString();

        using var b = Baglan();
        Calistir(b, "PRAGMA journal_mode = WAL;");
        Calistir(b, File.ReadAllText(semaYolu));
    }

    private SqliteConnection Baglan()
    {
        var b = new SqliteConnection(_baglantiMetni);
        b.Open();
        return b;
    }

    private static void Calistir(SqliteConnection b, string sql)
    {
        using var k = b.CreateCommand();
        k.CommandText = sql;
        k.ExecuteNonQuery();
    }

    private static SqliteCommand Komut(SqliteConnection b, string sql, params object[] parametreler)
    {
        var k = b.CreateCommand();
        k.CommandText = sql;
        for (var i = 0; i < parametreler.Length; i++) k.Parameters.AddWithValue($"$p{i}", parametreler[i]);
        return k;
    }

    private static Randevu Oku(SqliteDataReader r) => new(
        r.GetInt64(r.GetOrdinal("id")),
        r.GetString(r.GetOrdinal("randevu_no")),
        r.GetString(r.GetOrdinal("ilce")),
        r.GetString(r.GetOrdinal("tarih")),
        r.GetString(r.GetOrdinal("saat")),
        r.GetString(r.GetOrdinal("vefat_eden_ad")),
        r.GetString(r.GetOrdinal("vefat_eden_soyad")),
        r.GetString(r.GetOrdinal("vefat_eden_tc")),
        r.GetString(r.GetOrdinal("basvuran_ad")),
        r.GetString(r.GetOrdinal("basvuran_soyad")),
        r.GetInt32(r.GetOrdinal("basvuran_dogum_yili")),
        r.GetString(r.GetOrdinal("basvuran_tc")),
        r.GetString(r.GetOrdinal("basvuran_cep")),
        r.GetString(r.GetOrdinal("durum")),
        r.GetString(r.GetOrdinal("olusturma")));

    private static List<Randevu> Liste(SqliteCommand k)
    {
        var sonuc = new List<Randevu>();
        using var r = k.ExecuteReader();
        while (r.Read()) sonuc.Add(Oku(r));
        return sonuc;
    }

    private Dictionary<string, int> Say(string sql, params object[] parametreler)
    {
        using var b = Baglan();
        using var k = Komut(b, sql, parametreler);
        using var r = k.ExecuteReader();
        var sonuc = new Dictionary<string, int>();
        while (r.Read()) sonuc[r.GetString(0)] = r.GetInt32(1);
        return sonuc;
    }

    // tarih -> aktif randevu sayısı
    public Dictionary<string, int> GunlukDoluluk(string ilk, string son) => Say(
        "SELECT tarih, COUNT(*) FROM randevular WHERE durum = 'aktif' AND tarih BETWEEN $p0 AND $p1 GROUP BY tarih",
        ilk, son);

    // saat -> aktif randevu sayısı
    public Dictionary<string, int> SaatlikDoluluk(string tarih) => Say(
        "SELECT saat, COUNT(*) FROM randevular WHERE durum = 'aktif' AND tarih = $p0 GROUP BY saat", tarih);

    private static string RandevuNoUret() =>
        new(Enumerable.Range(0, 8).Select(_ => NoKarakterleri[RandomNumberGenerator.GetInt32(NoKarakterleri.Length)]).ToArray());

    // Kapasite ve mükerrer kontrolünü tek bir yazma işlemi içinde yapıp randevuyu ekler.
    public (Randevu? randevu, Cakisma? cakisma) Ekle(RandevuVerisi v, string bugun, int kapasite)
    {
        lock (_yazmaKilidi)
        {
            using var b = Baglan();
            using var islem = b.BeginTransaction();

            using (var k = Komut(b,
                "SELECT tarih, saat FROM randevular WHERE durum = 'aktif' AND vefat_eden_tc = $p0 AND tarih >= $p1 LIMIT 1",
                v.VefatEdenTc, bugun))
            using (var r = k.ExecuteReader())
            {
                if (r.Read())
                {
                    var p = r.GetString(0).Split('-');
                    return (null, new Cakisma("vefatEdenTc",
                        $"Bu vefat eden kişi için {p[2]}.{p[1]}.{p[0]} {r.GetString(1)} tarihli aktif bir randevu zaten bulunmaktadır."));
                }
            }

            using (var k = Komut(b,
                "SELECT COUNT(*) FROM randevular WHERE durum = 'aktif' AND tarih = $p0 AND saat = $p1", v.Tarih, v.Saat))
            {
                if (Convert.ToInt32(k.ExecuteScalar()) >= kapasite)
                {
                    return (null, new Cakisma("saat",
                        "Seçtiğiniz saat başka bir kullanıcı tarafından alındı. Lütfen başka bir saat seçiniz."));
                }
            }

            long id = 0;
            for (var deneme = 0; ; deneme++)
            {
                try
                {
                    using var k = Komut(b, """
                        INSERT INTO randevular (randevu_no, ilce, tarih, saat, vefat_eden_ad, vefat_eden_soyad, vefat_eden_tc,
                            basvuran_ad, basvuran_soyad, basvuran_dogum_yili, basvuran_tc, basvuran_cep)
                        VALUES ($p0, $p1, $p2, $p3, $p4, $p5, $p6, $p7, $p8, $p9, $p10, $p11);
                        SELECT last_insert_rowid();
                        """,
                        RandevuNoUret(), v.Ilce, v.Tarih, v.Saat, v.VefatEdenAd, v.VefatEdenSoyad, v.VefatEdenTc,
                        v.BasvuranAd, v.BasvuranSoyad, v.BasvuranDogumYili!.Value, v.BasvuranTc, v.BasvuranCep);
                    id = Convert.ToInt64(k.ExecuteScalar());
                    break;
                }
                // Randevu numarası çakışması (çok düşük ihtimal) — yeni numarayla tekrar dene.
                catch (SqliteException e) when (e.SqliteErrorCode == 19 && deneme < 5)
                {
                }
            }

            Randevu randevu;
            using (var k = Komut(b, "SELECT * FROM randevular WHERE id = $p0", id))
            {
                randevu = Liste(k)[0];
            }
            islem.Commit();
            return (randevu, null);
        }
    }

    // tarih verilirse o günün tüm kayıtları, verilmezse bugünden itibaren tüm kayıtlar
    public List<Randevu> Listele(string? tarih, string bugun)
    {
        using var b = Baglan();
        using var k = string.IsNullOrEmpty(tarih)
            ? Komut(b, "SELECT * FROM randevular WHERE tarih >= $p0 ORDER BY tarih, saat, id", bugun)
            : Komut(b, "SELECT * FROM randevular WHERE tarih = $p0 ORDER BY saat, id", tarih);
        return Liste(k);
    }

    public bool IptalEt(long id)
    {
        lock (_yazmaKilidi)
        {
            using var b = Baglan();
            using var k = Komut(b, "UPDATE randevular SET durum = 'iptal' WHERE id = $p0 AND durum = 'aktif'", id);
            return k.ExecuteNonQuery() > 0;
        }
    }
}
