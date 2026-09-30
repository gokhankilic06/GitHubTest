import re

ISIM_DESENI = re.compile(r"[A-Za-zÇĞİÖŞÜçğıöşüÂâÎîÛû .'-]{2,50}")


def tc_gecerli_mi(tc) -> bool:
    if not isinstance(tc, str) or not re.fullmatch(r"[1-9][0-9]{10}", tc):
        return False
    d = [int(c) for c in tc]
    tek = d[0] + d[2] + d[4] + d[6] + d[8]
    cift = d[1] + d[3] + d[5] + d[7]
    if (tek * 7 - cift) % 10 != d[9]:
        return False
    return sum(d[:10]) % 10 == d[10]


def buyuk_harf(metin: str) -> str:
    """Türkçe kurallarına göre büyük harfe çevirir (i → İ, ı → I)."""
    return metin.replace("i", "İ").replace("ı", "I").upper()


def _metin(deger) -> str:
    return " ".join(deger.split()) if isinstance(deger, str) else ""


def _yil(deger):
    if isinstance(deger, bool):
        return None
    if isinstance(deger, int):
        return deger
    if isinstance(deger, float) and deger.is_integer():
        return int(deger)
    if isinstance(deger, str) and re.fullmatch(r"[0-9]{1,4}", deger.strip()):
        return int(deger.strip())
    return None


def randevu_dogrula(govde, ilceler: list[str], bu_yil: int) -> tuple[dict, dict]:
    """Formdan gelen randevu verisini doğrular ve normalleştirir.
    Dönüş: (veri, hatalar) — hatalar boşsa veri kaydedilmeye hazırdır."""
    g = govde if isinstance(govde, dict) else {}
    hatalar: dict[str, str] = {}
    veri: dict = {}

    if g.get("taahhut") is not True:
        hatalar["taahhut"] = "Açıklamaları okuduğunuzu onaylamanız gerekmektedir."

    veri["ilce"] = _metin(g.get("ilce"))
    if veri["ilce"] not in ilceler:
        hatalar["ilce"] = "Lütfen geçerli bir ilçe seçiniz."

    veri["tarih"] = _metin(g.get("tarih"))
    veri["saat"] = _metin(g.get("saat"))

    for alan, ad in (("vefatEdenAd", "Ad"), ("vefatEdenSoyad", "Soyad"),
                     ("basvuranAd", "Ad"), ("basvuranSoyad", "Soyad")):
        deger = _metin(g.get(alan))
        if not deger:
            hatalar[alan] = f"{ad} alanı zorunludur."
        elif not ISIM_DESENI.fullmatch(deger):
            hatalar[alan] = f"{ad} yalnızca harflerden oluşmalı (2-50 karakter)."
        veri[alan] = buyuk_harf(deger)

    veri["vefatEdenTc"] = _metin(g.get("vefatEdenTc"))
    veri["basvuranTc"] = _metin(g.get("basvuranTc"))
    if not tc_gecerli_mi(veri["vefatEdenTc"]):
        hatalar["vefatEdenTc"] = "Geçerli bir T.C. Kimlik Numarası giriniz."
    if not tc_gecerli_mi(veri["basvuranTc"]):
        hatalar["basvuranTc"] = "Geçerli bir T.C. Kimlik Numarası giriniz."
    elif veri["basvuranTc"] == veri["vefatEdenTc"]:
        hatalar["basvuranTc"] = "Randevuyu alanın T.C. numarası vefat edenle aynı olamaz."

    yil = _yil(g.get("basvuranDogumYili"))
    if yil is None or yil < 1900 or yil > bu_yil:
        hatalar["basvuranDogumYili"] = "Geçerli bir doğum yılı giriniz (örn: 1980)."
    veri["basvuranDogumYili"] = yil

    cep = re.sub(r"[^0-9]", "", _metin(g.get("basvuranCep")))
    if re.fullmatch(r"05[0-9]{9}", cep):
        cep = cep[1:]
    veri["basvuranCep"] = cep
    if not re.fullmatch(r"5[0-9]{9}", cep):
        hatalar["basvuranCep"] = "Cep numarasını başında 0 olmadan 5xxxxxxxxx şeklinde giriniz."

    return veri, hatalar
