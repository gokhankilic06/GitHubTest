import os
import secrets
import sqlite3
import threading

NO_KARAKTERLERI = "ABCDEFGHJKLMNPRSTUVYZ23456789"


def randevu_no_uret() -> str:
    return "".join(secrets.choice(NO_KARAKTERLERI) for _ in range(8))


def _satirdan_randevu(s: sqlite3.Row) -> dict:
    return {
        "id": s["id"],
        "randevuNo": s["randevu_no"],
        "ilce": s["ilce"],
        "tarih": s["tarih"],
        "saat": s["saat"],
        "vefatEdenAd": s["vefat_eden_ad"],
        "vefatEdenSoyad": s["vefat_eden_soyad"],
        "vefatEdenTc": s["vefat_eden_tc"],
        "basvuranAd": s["basvuran_ad"],
        "basvuranSoyad": s["basvuran_soyad"],
        "basvuranDogumYili": s["basvuran_dogum_yili"],
        "basvuranTc": s["basvuran_tc"],
        "basvuranCep": s["basvuran_cep"],
        "durum": s["durum"],
        "olusturma": s["olusturma"],
    }


class Depo:
    def __init__(self, db_yolu: str, sema_yolu: str):
        klasor = os.path.dirname(db_yolu)
        if klasor:
            os.makedirs(klasor, exist_ok=True)
        self._db_yolu = db_yolu
        # Kapasite kontrolü ile ekleme arasının başka bir istek tarafından bölünmemesi için.
        self._yazma_kilidi = threading.Lock()
        with self._baglan() as b, open(sema_yolu, encoding="utf-8") as f:
            b.execute("PRAGMA journal_mode = WAL")
            b.executescript(f.read())

    def _baglan(self) -> sqlite3.Connection:
        b = sqlite3.connect(self._db_yolu, timeout=5, isolation_level=None)
        b.row_factory = sqlite3.Row
        return b

    def _sorgu(self, sql: str, *parametreler) -> list[sqlite3.Row]:
        b = self._baglan()
        try:
            return b.execute(sql, parametreler).fetchall()
        finally:
            b.close()

    def gunluk_doluluk(self, ilk: str, son: str) -> dict[str, int]:
        """tarih -> aktif randevu sayısı"""
        satirlar = self._sorgu(
            "SELECT tarih, COUNT(*) AS adet FROM randevular "
            "WHERE durum = 'aktif' AND tarih BETWEEN ? AND ? GROUP BY tarih", ilk, son)
        return {s["tarih"]: s["adet"] for s in satirlar}

    def saatlik_doluluk(self, tarih: str) -> dict[str, int]:
        """saat -> aktif randevu sayısı"""
        satirlar = self._sorgu(
            "SELECT saat, COUNT(*) AS adet FROM randevular "
            "WHERE durum = 'aktif' AND tarih = ? GROUP BY saat", tarih)
        return {s["saat"]: s["adet"] for s in satirlar}

    def ekle(self, veri: dict, bugun: str, kapasite: int) -> dict:
        """Kapasite ve mükerrer kontrolünü tek bir yazma işlemi içinde yapıp randevuyu ekler.
        Dönüş: {"randevu": ...} veya {"cakisma": {"alan": ..., "mesaj": ...}}"""
        with self._yazma_kilidi:
            b = self._baglan()
            try:
                b.execute("BEGIN IMMEDIATE")
                mevcut = b.execute(
                    "SELECT tarih, saat FROM randevular WHERE durum = 'aktif' AND vefat_eden_tc = ? AND tarih >= ?",
                    (veri["vefatEdenTc"], bugun)).fetchone()
                if mevcut:
                    b.execute("ROLLBACK")
                    y, a, g = mevcut["tarih"].split("-")
                    return {"cakisma": {
                        "alan": "vefatEdenTc",
                        "mesaj": f"Bu vefat eden kişi için {g}.{a}.{y} {mevcut['saat']} tarihli aktif bir randevu zaten bulunmaktadır.",
                    }}

                adet = b.execute(
                    "SELECT COUNT(*) FROM randevular WHERE durum = 'aktif' AND tarih = ? AND saat = ?",
                    (veri["tarih"], veri["saat"])).fetchone()[0]
                if adet >= kapasite:
                    b.execute("ROLLBACK")
                    return {"cakisma": {
                        "alan": "saat",
                        "mesaj": "Seçtiğiniz saat başka bir kullanıcı tarafından alındı. Lütfen başka bir saat seçiniz.",
                    }}

                deneme = 0
                while True:
                    try:
                        imlec = b.execute(
                            "INSERT INTO randevular (randevu_no, ilce, tarih, saat, vefat_eden_ad, vefat_eden_soyad, "
                            "vefat_eden_tc, basvuran_ad, basvuran_soyad, basvuran_dogum_yili, basvuran_tc, basvuran_cep) "
                            "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                            (randevu_no_uret(), veri["ilce"], veri["tarih"], veri["saat"], veri["vefatEdenAd"],
                             veri["vefatEdenSoyad"], veri["vefatEdenTc"], veri["basvuranAd"], veri["basvuranSoyad"],
                             veri["basvuranDogumYili"], veri["basvuranTc"], veri["basvuranCep"]))
                        break
                    except sqlite3.IntegrityError:
                        # Randevu numarası çakışması (çok düşük ihtimal) — yeni numarayla tekrar dene.
                        deneme += 1
                        if deneme > 5:
                            raise
                b.execute("COMMIT")
                satir = b.execute("SELECT * FROM randevular WHERE id = ?", (imlec.lastrowid,)).fetchone()
                return {"randevu": _satirdan_randevu(satir)}
            except Exception:
                if b.in_transaction:
                    b.execute("ROLLBACK")
                raise
            finally:
                b.close()

    def listele(self, tarih: str, bugun: str) -> list[dict]:
        """tarih verilirse o günün tüm kayıtları, verilmezse bugünden itibaren tüm kayıtlar"""
        if tarih:
            satirlar = self._sorgu("SELECT * FROM randevular WHERE tarih = ? ORDER BY saat, id", tarih)
        else:
            satirlar = self._sorgu("SELECT * FROM randevular WHERE tarih >= ? ORDER BY tarih, saat, id", bugun)
        return [_satirdan_randevu(s) for s in satirlar]

    def iptal_et(self, randevu_id: int) -> bool:
        with self._yazma_kilidi:
            b = self._baglan()
            try:
                imlec = b.execute(
                    "UPDATE randevular SET durum = 'iptal' WHERE id = ? AND durum = 'aktif'", (randevu_id,))
                return imlec.rowcount > 0
            finally:
                b.close()
