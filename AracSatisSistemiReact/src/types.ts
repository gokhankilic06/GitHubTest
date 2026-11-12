export interface KayitFormuVerisi {
  cinsler: { id: number; ad: string }[];
  markalar: { id: number; ad: string }[];
  otoparklar: { id: number; ad: string }[];
  renkler: { id: number; ad: string }[];
  maksimumModelYili: number;
  ankaraVergiDaireleri: { id: number; ad: string; sehir: string }[];
  vergiDaireleri: { id: number; ad: string; sehir: string }[];
  sehirler: string[];
  modelSozlugu: Record<string, string[]>;
}

export interface YeniDosyaOnerileri {
  satis1Onerisi: string;
  satis2Onerisi: string;
  komisyonBaskani: string | null;
  komisyonUye1: string | null;
  komisyonUye2: string | null;
  komisyonUye3: string | null;
}

export interface DosyaOzeti {
  id: number;
  dosyaNo: string;
  kayitTarihi: string;
  plaka: string;
  adiSoyadiUnvani: string;
  vergiDairesiAdi: string;
  aracMarkasi: string;
  aracCinsi: string;
  durum: string;
  durumGorunenAd: string;
  genelDurumGorunen: string;
  asamaGorunen: string;
  asamaRozetSinifi: string;
  aktif: boolean;
}

export interface SatisKaydi {
  id: number;
  satisTuru: string;
  satisTuruGorunen: string;
  satisTarihi: string;
  satisSonucu: string;
  sonucGorunen: string;
  satisBedeli: number | null;
  aliciAdiSoyadi: string | null;
}

export interface Dosya {
  id: number;
  dosyaNo: string;
  kayitTarihi: string;
  vergiDairesiAdi: string;
  vergiDairesiYaziTarihi: string | null;
  vergiDairesiYaziSayisi: string | null;
  alacakliVergiDairesi: string;
  vergiKimlikNumarasi: string | null;
  tcKimlikNumarasi: string | null;
  adiSoyadiUnvani: string;
  kanuniTemsilci: string | null;
  plaka: string;
  aracCinsi: string;
  aracMarkasi: string;
  aracTipi: string | null;
  modelYili: number;
  renk: string | null;
  ruhsatVar: boolean;
  anahtarVar: boolean;
  cekiciVar: boolean;
  otoparkAdi: string | null;
  hasarDurumu: string | null;
  muhammenBedel: number;
  kdvOrani: number;
  otoparkaGirisTarihi: string | null;
  komisyonBaskani: string | null;
  komisyonUye1: string | null;
  komisyonUye2: string | null;
  komisyonUye3: string | null;
  durum: string;
  durumGorunenAd: string;
  asamaGorunen: string;
  asamaRozetSinifi: string;
  satis1Tarihi: string;
  satis2Tarihi: string;
  teminat: number;
  damgaVergisiOrani: number;
  damgaVergisiTutari: number;
  satislar: SatisKaydi[];
}

export interface DosyaFormVerisi {
  vergiDairesiAdi: string;
  vergiDairesiYaziTarihi: string;
  vergiDairesiYaziSayisi: string;
  alacakliVergiDairesi: string;
  vergiKimlikNumarasi: string;
  tcKimlikNumarasi: string;
  adiSoyadiUnvani: string;
  kanuniTemsilci: string;
  plaka: string;
  aracCinsi: string;
  aracMarkasi: string;
  aracTipi: string;
  modelYili: number | '';
  renk: string;
  ruhsatVar: boolean;
  anahtarVar: boolean;
  cekiciVar: boolean;
  otoparkAdi: string;
  hasarDurumu: string;
  muhammenBedel: number | '';
  kdvOrani: number;
  damgaVergisiOrani: number | '';
  otoparkaGirisTarihi: string;
  komisyonBaskani: string;
  komisyonUye1: string;
  komisyonUye2: string;
  komisyonUye3: string;
  satis1Tarihi: string;
  satis2Tarihi: string;
}
