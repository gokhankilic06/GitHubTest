import base64
import hmac
import json
import os
import re
from datetime import datetime, timezone
from functools import wraps
from pathlib import Path
from urllib.parse import quote

from flask import Flask, jsonify, request, send_from_directory

import takvim
from depo import Depo
from dogrulama import randevu_dogrula

KOK = Path(__file__).resolve().parent.parent
WEB = KOK / "web"
CONFIG = json.loads((KOK / "config.json").read_text(encoding="utf-8"))


def simdi() -> datetime:
    """SIMDI ortam değişkeni (ISO tarih) test amaçlı sabit bir "şu an" vermeyi sağlar."""
    deger = os.environ.get("SIMDI")
    if not deger:
        return datetime.now(timezone.utc)
    an = datetime.fromisoformat(deger.replace("Z", "+00:00"))
    return an if an.tzinfo else an.replace(tzinfo=timezone.utc)


def _esit_mi(a: str, b: str) -> bool:
    return hmac.compare_digest(a.encode("utf-8"), b.encode("utf-8"))


def yonetici_dogrula(fonksiyon):
    """Personel paneli için HTTP Basic kimlik doğrulaması."""
    @wraps(fonksiyon)
    def sarmalayici(*args, **kwargs):
        sifre = os.environ.get("ADMIN_PASSWORD")
        if not sifre:
            return jsonify(hata="Yönetim paneli kapalı: ADMIN_PASSWORD ortam değişkeni tanımlanmamış."), 503
        kullanici = os.environ.get("ADMIN_USER", "yonetici")
        tip, _, deger = request.headers.get("Authorization", "").partition(" ")
        if tip == "Basic" and deger:
            try:
                cozulen = base64.b64decode(deger).decode("utf-8")
            except ValueError:
                cozulen = ""
            k, ayrac, s = cozulen.partition(":")
            if ayrac and _esit_mi(k, kullanici) and _esit_mi(s, sifre):
                return fonksiyon(*args, **kwargs)
        yanit = jsonify(hata="Yetkisiz erişim.")
        yanit.status_code = 401
        yanit.headers["WWW-Authenticate"] = 'Basic realm="Randevu Yonetimi", charset="UTF-8"'
        return yanit
    return sarmalayici


def uygulama_olustur(depo: Depo) -> Flask:
    app = Flask(__name__, static_folder=str(WEB / "public"), static_url_path="")
    app.json.ensure_ascii = False
    app.json.sort_keys = False

    @app.after_request
    def guvenlik_basliklari(yanit):
        yanit.headers["X-Content-Type-Options"] = "nosniff"
        return yanit

    def _json_govde():
        """İstek gövdesini JSON olarak okur; geçersizse None döner."""
        try:
            return True, json.loads(request.get_data(as_text=True) or "null")
        except ValueError:
            return False, None

    @app.get("/")
    def ana_sayfa():
        return app.send_static_file("index.html")

    @app.get("/api/ayarlar")
    def ayarlar():
        belge = CONFIG["belge"]
        return jsonify(
            vergiDairesi=CONFIG["vergiDairesi"],
            ilceler=CONFIG["ilceler"],
            yeniGunAcilisSaati=CONFIG["yeniGunAcilisSaati"],
            belge={
                "ad": belge["ad"],
                "url": "/belgeler/" + quote(belge["dosya"]),
                "mevcut": (WEB / "public" / "belgeler" / belge["dosya"]).is_file(),
            },
        )

    @app.get("/api/gunler")
    def gunler():
        p = takvim.pencere(CONFIG, simdi())
        doluluk = depo.gunluk_doluluk(p["ilk"], p["son"])
        sonuc = {}
        t = p["ilk"]
        while t <= p["son"]:
            kapasite = len(takvim.gun_saatleri(CONFIG, t)) * CONFIG["slotKapasitesi"]
            if kapasite == 0:
                sonuc[t] = "kapali"
            else:
                sonuc[t] = "dolu" if doluluk.get(t, 0) >= kapasite else "uygun"
            t = takvim.gun_ekle(t, 1)
        return jsonify(bugun=p["bugun"], ilk=p["ilk"], son=p["son"], gunler=sonuc)

    def _gun_acik_mi(tarih: str, p: dict) -> bool:
        return (takvim.gecerli_tarih_mi(tarih) and p["ilk"] <= tarih <= p["son"]
                and takvim.is_gunu_mu(CONFIG, tarih))

    @app.get("/api/saatler")
    def saatler():
        tarih = request.args.get("tarih", "")
        if not _gun_acik_mi(tarih, takvim.pencere(CONFIG, simdi())):
            return jsonify(hata="Seçilen gün için randevu alınamaz."), 400
        doluluk = depo.saatlik_doluluk(tarih)
        liste = [{"saat": s, "uygun": doluluk.get(s, 0) < CONFIG["slotKapasitesi"]}
                 for s in takvim.gun_saatleri(CONFIG, tarih)]
        return jsonify(tarih=tarih, saatler=liste)

    @app.post("/api/randevular")
    def randevu_olustur():
        gecerli, govde = _json_govde()
        if not gecerli:
            return jsonify(hata="Geçersiz istek."), 400

        p = takvim.pencere(CONFIG, simdi())
        veri, hatalar = randevu_dogrula(govde, CONFIG["ilceler"], int(p["bugun"][:4]))

        if not _gun_acik_mi(veri["tarih"], p):
            hatalar["tarih"] = "Seçilen gün için randevu alınamaz."
        elif veri["saat"] not in takvim.gun_saatleri(CONFIG, veri["tarih"]):
            hatalar["saat"] = "Geçerli bir randevu saati seçiniz."

        if hatalar:
            return jsonify(hata="Lütfen hatalı alanları düzeltiniz.", hatalar=hatalar), 400

        sonuc = depo.ekle(veri, p["bugun"], CONFIG["slotKapasitesi"])
        if "cakisma" in sonuc:
            c = sonuc["cakisma"]
            return jsonify(hata=c["mesaj"], hatalar={c["alan"]: c["mesaj"]}), 409
        return jsonify(randevu=sonuc["randevu"]), 201

    def _vatandas_randevusu(govde):
        """Vatandaşın randevu numarası ve kendi T.C. numarasıyla randevusunu bulması.
        Dönüş: (randevu, None) veya (None, (durum_kodu, hata))"""
        g = govde if isinstance(govde, dict) else {}
        randevu_no = g.get("randevuNo").strip().upper() if isinstance(g.get("randevuNo"), str) else ""
        basvuran_tc = g.get("basvuranTc").strip() if isinstance(g.get("basvuranTc"), str) else ""
        if not re.fullmatch(r"[A-Z0-9]{8}", randevu_no) or not re.fullmatch(r"[0-9]{11}", basvuran_tc):
            return None, (400, "Randevu numarası ve T.C. Kimlik Numarası eksik veya hatalı.")
        r = depo.numara_ile_getir(randevu_no)
        if not r or r["basvuranTc"] != basvuran_tc:
            return None, (404, "Girdiğiniz bilgilerle eşleşen bir randevu bulunamadı.")
        return r, None

    def _vatandas_ozeti(r: dict, bugun: str) -> dict:
        """Vatandaşa gösterilen randevu özeti (T.C. ve telefon numaraları gösterilmez)."""
        return {
            "randevuNo": r["randevuNo"],
            "vergiDairesi": CONFIG["vergiDairesi"],
            "ilce": r["ilce"],
            "tarih": r["tarih"],
            "saat": r["saat"],
            "vefatEdenAd": r["vefatEdenAd"],
            "vefatEdenSoyad": r["vefatEdenSoyad"],
            "basvuranAd": r["basvuranAd"],
            "basvuranSoyad": r["basvuranSoyad"],
            "durum": r["durum"],
            "iptalEdilebilir": r["durum"] == "aktif" and r["tarih"] >= bugun,
        }

    @app.post("/api/randevu-sorgula")
    def randevu_sorgula():
        gecerli, govde = _json_govde()
        if not gecerli:
            return jsonify(hata="Geçersiz istek."), 400
        r, hata = _vatandas_randevusu(govde)
        if hata:
            return jsonify(hata=hata[1]), hata[0]
        bugun = takvim.pencere(CONFIG, simdi())["bugun"]
        return jsonify(randevu=_vatandas_ozeti(r, bugun))

    @app.post("/api/randevu-iptal")
    def randevu_iptal():
        gecerli, govde = _json_govde()
        if not gecerli:
            return jsonify(hata="Geçersiz istek."), 400
        r, hata = _vatandas_randevusu(govde)
        if hata:
            return jsonify(hata=hata[1]), hata[0]
        bugun = takvim.pencere(CONFIG, simdi())["bugun"]
        if r["durum"] != "aktif":
            return jsonify(hata="Bu randevu zaten iptal edilmiş."), 409
        if r["tarih"] < bugun:
            return jsonify(hata="Tarihi geçmiş randevular iptal edilemez."), 409
        if not depo.iptal_et(r["id"]):
            return jsonify(hata="Bu randevu zaten iptal edilmiş."), 409
        return jsonify(randevu=_vatandas_ozeti(depo.getir(r["id"]), bugun))

    @app.get("/yonetim")
    @yonetici_dogrula
    def yonetim_sayfasi():
        return send_from_directory(WEB, "yonetim.html")

    @app.get("/api/yonetim/randevular")
    @yonetici_dogrula
    def yonetim_listele():
        tarih = request.args.get("tarih", "")
        if tarih and not takvim.gecerli_tarih_mi(tarih):
            return jsonify(hata="Geçersiz tarih."), 400
        bugun = takvim.pencere(CONFIG, simdi())["bugun"]
        return jsonify(randevular=depo.listele(tarih, bugun))

    @app.post("/api/yonetim/randevular/<int:randevu_id>/iptal")
    @yonetici_dogrula
    def yonetim_iptal(randevu_id: int):
        if not depo.iptal_et(randevu_id):
            return jsonify(hata="Aktif randevu bulunamadı."), 404
        return jsonify(tamam=True)

    @app.errorhandler(404)
    def bulunamadi(_hata):
        if request.path.startswith("/api/"):
            return jsonify(hata="Bulunamadı."), 404
        return "Bulunamadı.", 404

    @app.errorhandler(500)
    def sunucu_hatasi(_hata):
        return jsonify(hata="Sunucu hatası. Lütfen daha sonra tekrar deneyiniz."), 500

    return app


if __name__ == "__main__":
    depo = Depo(os.environ.get("DB_PATH") or str(KOK / "data" / "randevu.db"), str(KOK / "schema.sql"))
    port = int(os.environ.get("PORT", "5000"))
    print(f"Veraset randevu sistemi (Python) http://localhost:{port} adresinde çalışıyor.", flush=True)
    from waitress import serve
    serve(uygulama_olustur(depo), host=os.environ.get("HOST", "0.0.0.0"), port=port)
