"""Tarih/saat kuralları. Tüm tarihler "YYYY-MM-DD", saatler "HH:MM" metinleridir."""
from datetime import date, datetime, timedelta
from zoneinfo import ZoneInfo


def _dakikaya(saat: str) -> int:
    s, d = saat.split(":")
    return int(s) * 60 + int(d)


def _saate(dakika: int) -> str:
    return f"{dakika // 60:02d}:{dakika % 60:02d}"


def yerel_zaman(an: datetime, saat_dilimi: str) -> tuple[str, int]:
    """Verilen anın yapılandırılmış saat dilimindeki tarihini ve günün kaçıncı dakikası olduğunu döner."""
    yerel = an.astimezone(ZoneInfo(saat_dilimi))
    return yerel.date().isoformat(), yerel.hour * 60 + yerel.minute


def gun_ekle(tarih: str, gun: int) -> str:
    return (date.fromisoformat(tarih) + timedelta(days=gun)).isoformat()


def gecerli_tarih_mi(tarih) -> bool:
    if not isinstance(tarih, str) or len(tarih) != 10 or tarih[4] != "-" or tarih[7] != "-":
        return False
    try:
        return date.fromisoformat(tarih).isoformat() == tarih
    except ValueError:
        return False


def pencere(config: dict, an: datetime) -> dict:
    """Randevu alınabilecek ilk ve son gün. Aynı gün için randevu verilmez;
    her gün yeniGunAcilisSaati'nde pencerenin sonuna yeni bir gün eklenir."""
    bugun, dakika = yerel_zaman(an, config["saatDilimi"])
    acildi = dakika >= _dakikaya(config["yeniGunAcilisSaati"])
    return {
        "bugun": bugun,
        "ilk": gun_ekle(bugun, 1),
        "son": gun_ekle(bugun, config["ileriGunSayisi"] - (0 if acildi else 1)),
    }


def is_gunu_mu(config: dict, tarih: str) -> bool:
    """Kurum çalışıyor mu? (hafta sonu ve resmi tatiller hariç)"""
    return date.fromisoformat(tarih).weekday() < 5 and tarih not in config["tatiller"]


def gun_saatleri(config: dict, tarih: str) -> list[str]:
    """Verilen iş gününün randevu saatleri. Yarım günlerde yalnızca ilk mesai bloğu kullanılır."""
    if not is_gunu_mu(config, tarih):
        return []
    bloklar = config["mesaiBloklari"][:1] if tarih in config["yarimGunler"] else config["mesaiBloklari"]
    saatler = []
    for blok in bloklar:
        dakika = _dakikaya(blok["baslangic"])
        while dakika < _dakikaya(blok["bitis"]):
            saatler.append(_saate(dakika))
            dakika += config["randevuAraligiDakika"]
    return saatler
