CREATE TABLE IF NOT EXISTS randevular (
    id                  INTEGER PRIMARY KEY AUTOINCREMENT,
    randevu_no          TEXT    NOT NULL UNIQUE,
    ilce                TEXT    NOT NULL,
    tarih               TEXT    NOT NULL,
    saat                TEXT    NOT NULL,
    vefat_eden_ad       TEXT    NOT NULL,
    vefat_eden_soyad    TEXT    NOT NULL,
    vefat_eden_tc       TEXT    NOT NULL,
    basvuran_ad         TEXT    NOT NULL,
    basvuran_soyad      TEXT    NOT NULL,
    basvuran_dogum_yili INTEGER NOT NULL,
    basvuran_tc         TEXT    NOT NULL,
    basvuran_cep        TEXT    NOT NULL,
    durum               TEXT    NOT NULL DEFAULT 'aktif',
    olusturma           TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now'))
);

CREATE INDEX IF NOT EXISTS ix_randevular_tarih_saat ON randevular (tarih, saat);
CREATE INDEX IF NOT EXISTS ix_randevular_vefat_eden_tc ON randevular (vefat_eden_tc);
