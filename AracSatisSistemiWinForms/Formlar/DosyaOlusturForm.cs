using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using AracSatisSistemiWinForms;
using AracSatisSistemiWinForms.Modeller;
using AracSatisSistemiWinForms.Servisler;

namespace AracSatisSistemiWinForms.Formlar
{
    public class DosyaOlusturForm : Form
    {
        private const string DigerSecenegi = "Diğer (elle yaz)";

        private readonly Panel _icerikPaneli = new() { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20) };
        private readonly Label _durumEtiketi = new() { Dock = DockStyle.Top, Height = 30, ForeColor = Color.Firebrick, Text = "" };
        private readonly Button _kaydetButonu = new() { Text = "Dosyayı Kaydet", Width = 150, Height = 35 };
        private readonly Button _vazgecButonu = new() { Text = "Vazgeç", Width = 100, Height = 35 };

        // Form alanları
        private readonly ComboBox _vergiDairesiAdi = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
        private readonly DateTimePicker _vergiDairesiYaziTarihi = new() { ShowCheckBox = true, Checked = false, Width = 300, Format = DateTimePickerFormat.Short };
        private readonly TextBox _vergiDairesiYaziSayisi = new() { Width = 300 };
        private readonly ComboBox _il = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
        private readonly ComboBox _alacakliVergiDairesi = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300, Enabled = false };
        private readonly TextBox _vergiKimlikNumarasi = new() { Width = 300, MaxLength = 10 };
        private readonly Label _vknUyari = new() { Width = 400, ForeColor = Color.Firebrick, Text = "", AutoSize = true };
        private readonly TextBox _tcKimlikNumarasi = new() { Width = 300, MaxLength = 11 };
        private readonly Label _tcknUyari = new() { Width = 400, ForeColor = Color.Firebrick, Text = "", AutoSize = true };
        private readonly TextBox _adiSoyadiUnvani = new() { Width = 300 };
        private readonly TextBox _kanuniTemsilci = new() { Width = 300 };
        private readonly TextBox _plaka = new() { Width = 300 };
        private readonly Label _plakaUyari = new() { Width = 400, ForeColor = Color.Firebrick, Text = "", AutoSize = true };
        private readonly ComboBox _aracCinsi = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
        private readonly ComboBox _aracMarkasi = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
        private readonly ComboBox _aracTipi = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300, Enabled = false };
        private readonly TextBox _aracTipiManuel = new() { Width = 300, Visible = false };
        private readonly ComboBox _modelYili = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
        private readonly ComboBox _renk = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
        private readonly TextBox _renkManuel = new() { Width = 300, Visible = false };
        private readonly ComboBox _ruhsatVar = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
        private readonly ComboBox _anahtarVar = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
        private readonly ComboBox _otoparkAdi = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
        private readonly DateTimePicker _otoparkaGirisTarihi = new() { ShowCheckBox = true, Checked = false, Width = 300, Format = DateTimePickerFormat.Short };
        private readonly TextBox _muhammenBedel = new() { Width = 300, Text = "0" };
        private readonly TextBox _teminatGoster = new() { Width = 300, ReadOnly = true };
        private readonly ComboBox _kdvOrani = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
        private readonly NumericUpDown _damgaVergisiOrani = new() { Width = 300, DecimalPlaces = 2, Minimum = 0, Maximum = 1000, Value = 5.69m, Increment = 0.01m };
        private readonly TextBox _damgaVergisiGoster = new() { Width = 300, ReadOnly = true };
        private readonly TextBox _hasarDurumu = new() { Width = 300, Height = 60, Multiline = true };
        private readonly DateTimePicker _satis1Tarihi = new() { Width = 300, Format = DateTimePickerFormat.Short };
        private readonly DateTimePicker _satis2Tarihi = new() { Width = 300, Format = DateTimePickerFormat.Short };

        private KayitFormuVerisi? _veri;
        private YeniDosyaOnerileri? _oneriler;

        public DosyaOlusturForm()
        {
            Text = "Kayıt İşlemleri";
            Width = 780;
            Height = 800;
            StartPosition = FormStartPosition.CenterScreen;

            var altPanel = new Panel { Dock = DockStyle.Bottom, Height = 60 };
            _kaydetButonu.Top = 12; _kaydetButonu.Left = 20;
            _vazgecButonu.Top = 12; _vazgecButonu.Left = 190;
            altPanel.Controls.Add(_kaydetButonu);
            altPanel.Controls.Add(_vazgecButonu);

            Controls.Add(_icerikPaneli);
            Controls.Add(altPanel);
            Controls.Add(_durumEtiketi);

            _kaydetButonu.Click += async (s, e) => await KaydetAsync();
            _vazgecButonu.Click += (s, e) => Close();

            AlanlariOlustur();
            BagintilariKur();

            Load += async (s, e) => await VerileriYukleAsync();
        }

        private int _y = 0;

        private void AlanEkle(string etiket, Control kontrol, Control? ekKontrol = null)
        {
            var etiketKontrolu = new Label { Left = 0, Top = _y + 4, Width = 220, Text = etiket };
            kontrol.Left = 230;
            kontrol.Top = _y;
            _icerikPaneli.Controls.Add(etiketKontrolu);
            _icerikPaneli.Controls.Add(kontrol);
            var yukseklik = Math.Max(kontrol.Height, 23);
            if (ekKontrol != null)
            {
                ekKontrol.Left = 230;
                ekKontrol.Top = _y + yukseklik + 2;
                _icerikPaneli.Controls.Add(ekKontrol);
                yukseklik += ekKontrol.Height + 4;
            }
            _y += yukseklik + 14;
        }

        private void BolumBasligiEkle(string baslik)
        {
            var etiket = new Label
            {
                Left = 0, Top = _y, Width = 700, Height = 28,
                Text = baslik, Font = new Font(Font.FontFamily, 10, FontStyle.Bold),
                BackColor = Color.FromArgb(30, 41, 59), ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0)
            };
            _icerikPaneli.Controls.Add(etiket);
            _y += 36;
        }

        private void AlanlariOlustur()
        {
            BolumBasligiEkle("1. İlgili Vergi Dairesi");
            AlanEkle("Vergi Dairesi Adı:", _vergiDairesiAdi);
            AlanEkle("Vergi Dairesi Yazı Tarihi:", _vergiDairesiYaziTarihi);
            AlanEkle("Vergi Dairesi Yazı Sayısı:", _vergiDairesiYaziSayisi);
            AlanEkle("Alacaklı Vergi Dairesi - İl:", _il);
            AlanEkle("Alacaklı Vergi Dairesi:", _alacakliVergiDairesi);

            BolumBasligiEkle("2. Borçlu Mükellef Bilgileri");
            AlanEkle("Vergi Kimlik Numarası:", _vergiKimlikNumarasi, _vknUyari);
            AlanEkle("T.C. Kimlik Numarası:", _tcKimlikNumarasi, _tcknUyari);
            AlanEkle("Adı Soyadı / Unvanı:", _adiSoyadiUnvani);
            AlanEkle("Kanuni Temsilci / Ortak vb.:", _kanuniTemsilci);

            BolumBasligiEkle("3. Araç Bilgi Girişi");
            AlanEkle("Plaka:", _plaka, _plakaUyari);
            AlanEkle("Cinsi:", _aracCinsi);
            AlanEkle("Markası:", _aracMarkasi);
            AlanEkle("Tipi:", _aracTipi);
            _aracTipiManuel.Left = 230; _aracTipiManuel.Top = _aracTipi.Top;
            _icerikPaneli.Controls.Add(_aracTipiManuel);
            AlanEkle("Model Yılı:", _modelYili);
            AlanEkle("Renk:", _renk);
            _renkManuel.Left = 230; _renkManuel.Top = _renk.Top;
            _icerikPaneli.Controls.Add(_renkManuel);
            AlanEkle("Ruhsatın Olup Olmadığı:", _ruhsatVar);
            AlanEkle("Anahtarın Bulunup Bulunmadığı:", _anahtarVar);
            AlanEkle("Aracın Bulunduğu Otopark:", _otoparkAdi);
            AlanEkle("Otoparka Giriş Tarihi:", _otoparkaGirisTarihi);
            AlanEkle("Muhammen Bedeli (TL):", _muhammenBedel);
            AlanEkle("Teminat (%5, 10.000 TL üzeri):", _teminatGoster);
            AlanEkle("KDV Oranı (%):", _kdvOrani);
            AlanEkle("Damga Vergisi Oranı (Binde):", _damgaVergisiOrani);
            AlanEkle("Damga Vergisi Tutarı (Tahmini):", _damgaVergisiGoster);
            AlanEkle("Hasar Durumu:", _hasarDurumu);

            BolumBasligiEkle("4. Satış Bilgileri");
            AlanEkle("1. Satış Tarihi:", _satis1Tarihi);
            AlanEkle("2. Satış Tarihi:", _satis2Tarihi);

            _ruhsatVar.Items.AddRange(new object[] { "VAR", "YOK" });
            _ruhsatVar.SelectedIndex = 1;
            _anahtarVar.Items.AddRange(new object[] { "VAR", "YOK" });
            _anahtarVar.SelectedIndex = 1;
            _kdvOrani.Items.AddRange(new object[] { "%1", "%10", "%20" });
            _kdvOrani.SelectedIndex = 0;
        }

        private void BagintilariKur()
        {
            _plaka.TextChanged += (s, e) => PlakaDogrula();
            _plaka.Leave += (s, e) => PlakaDogrula();

            _vergiKimlikNumarasi.TextChanged += (s, e) => VknDogrula();
            _tcKimlikNumarasi.TextChanged += (s, e) => TcknDogrula();

            _muhammenBedel.TextChanged += (s, e) => OnizlemeleriHesapla();
            _damgaVergisiOrani.ValueChanged += (s, e) => OnizlemeleriHesapla();

            _il.SelectedIndexChanged += (s, e) => IlDegisti();
            _aracMarkasi.SelectedIndexChanged += (s, e) => MarkaDegisti();
            _aracTipi.SelectedIndexChanged += (s, e) =>
            {
                if (_aracTipi.SelectedItem?.ToString() == DigerSecenegi)
                {
                    _aracTipi.Visible = false;
                    _aracTipiManuel.Visible = true;
                    _aracTipiManuel.Text = "";
                    _aracTipiManuel.Focus();
                }
            };
            _renk.SelectedIndexChanged += (s, e) =>
            {
                if (_renk.SelectedItem?.ToString() == DigerSecenegi)
                {
                    _renk.Visible = false;
                    _renkManuel.Visible = true;
                    _renkManuel.Text = "";
                    _renkManuel.Focus();
                }
            };
        }

        private void PlakaDogrula()
        {
            var imlecKonumu = _plaka.SelectionStart;
            var temiz = new string(_plaka.Text.ToUpperInvariant().Where(c => char.IsDigit(c) || "ABCDEFGHIJKLMNOPRSTUVYZ".Contains(c)).ToArray());
            if (temiz != _plaka.Text)
            {
                _plaka.Text = temiz;
                _plaka.SelectionStart = Math.Min(imlecKonumu, temiz.Length);
            }
            var gecerliMi = temiz == "" || Dogrulamalar.PlakaGecerliMi(temiz);
            _plakaUyari.Text = gecerliMi ? "" : "Geçerli bir plaka giriniz (ör. 34ABC123; il kodu 01-81, Q/W/X kullanılmaz).";
        }

        private void VknDogrula()
        {
            var deger = _vergiKimlikNumarasi.Text.Trim();
            _vknUyari.Text = (deger.Length == 10 && !Dogrulamalar.VknGecerliMi(deger))
                ? "Geçerli bir Vergi Kimlik Numarası değil (kontrol hanesi tutmuyor)."
                : "";
        }

        private void TcknDogrula()
        {
            var deger = _tcKimlikNumarasi.Text.Trim();
            _tcknUyari.Text = (deger.Length == 11 && !Dogrulamalar.TcknGecerliMi(deger))
                ? "Geçerli bir T.C. Kimlik Numarası değil (kontrol hanesi tutmuyor)."
                : "";
        }

        private void OnizlemeleriHesapla()
        {
            var bedel = ParaAyristir(_muhammenBedel.Text);
            var teminat = bedel > 10000m ? Math.Round(bedel * 0.05m, 2) : 0m;
            var damgaVergisi = Math.Round(bedel * _damgaVergisiOrani.Value / 1000m, 2);
            _teminatGoster.Text = teminat.ToString("N2", new CultureInfo("tr-TR")) + " TL";
            _damgaVergisiGoster.Text = damgaVergisi.ToString("N2", new CultureInfo("tr-TR")) + " TL";
        }

        private static decimal ParaAyristir(string metin)
        {
            metin = metin.Trim().Replace(".", "").Replace(",", ".");
            return decimal.TryParse(metin, NumberStyles.Any, CultureInfo.InvariantCulture, out var deger) ? deger : 0m;
        }

        private void IlDegisti()
        {
            _alacakliVergiDairesi.Items.Clear();
            var secilenIl = _il.SelectedItem?.ToString();
            if (_veri == null || string.IsNullOrEmpty(secilenIl))
            {
                _alacakliVergiDairesi.Enabled = false;
                return;
            }
            _alacakliVergiDairesi.Enabled = true;
            foreach (var v in _veri.VergiDaireleri.Where(v => v.Sehir == secilenIl).OrderBy(v => v.Ad))
            {
                _alacakliVergiDairesi.Items.Add(v.Ad);
            }
        }

        private void MarkaDegisti()
        {
            _aracTipi.Items.Clear();
            _aracTipi.Visible = true;
            _aracTipiManuel.Visible = false;
            var secilenMarka = _aracMarkasi.SelectedItem?.ToString();
            if (_veri == null || string.IsNullOrEmpty(secilenMarka))
            {
                _aracTipi.Enabled = false;
                return;
            }
            _aracTipi.Enabled = true;
            if (_veri.ModelSozlugu.TryGetValue(secilenMarka, out var modeller))
            {
                foreach (var m in modeller.OrderBy(x => x)) _aracTipi.Items.Add(m);
            }
            _aracTipi.Items.Add(DigerSecenegi);
        }

        private async Task VerileriYukleAsync()
        {
            _durumEtiketi.Text = "Yükleniyor...";
            try
            {
                _veri = await Oturum.Api.GetAsync<KayitFormuVerisi>("/api/referans/kayit-formu");
                _oneriler = await Oturum.Api.GetAsync<YeniDosyaOnerileri>("/api/referans/yeni-dosya-onerileri");

                _vergiDairesiAdi.Items.AddRange(_veri.AnkaraVergiDaireleri.Select(v => v.Ad).ToArray());
                _il.Items.AddRange(_veri.Sehirler.ToArray());
                _aracCinsi.Items.AddRange(_veri.Cinsler.Select(c => c.Ad).ToArray());
                _aracMarkasi.Items.AddRange(_veri.Markalar.Select(m => m.Ad).ToArray());
                _otoparkAdi.Items.AddRange(_veri.Otoparklar.Select(o => o.Ad).ToArray());
                _renk.Items.Clear();
                _renk.Items.AddRange(_veri.Renkler.Select(r => r.Ad).ToArray());
                _renk.Items.Add(DigerSecenegi);

                for (var yil = _veri.MaksimumModelYili; yil >= 1940; yil--) _modelYili.Items.Add(yil.ToString());
                if (_modelYili.Items.Count > 0) _modelYili.SelectedIndex = 0;

                _satis1Tarihi.Value = _oneriler.Satis1Onerisi;
                _satis2Tarihi.Value = _oneriler.Satis2Onerisi;

                OnizlemeleriHesapla();
                _durumEtiketi.Text = "";
            }
            catch (OturumSuresiDolduHatasi ex)
            {
                MessageBox.Show(this, ex.Message + "\nProgram kapatılacak, lütfen yeniden açıp giriş yapın.", "Oturum Süresi Doldu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Application.Exit();
            }
            catch (Exception ex)
            {
                _durumEtiketi.Text = "Veriler yüklenemedi: " + ex.Message;
            }
        }

        private async Task KaydetAsync()
        {
            _durumEtiketi.Text = "";
            PlakaDogrula();
            VknDogrula();
            TcknDogrula();

            if (string.IsNullOrWhiteSpace(_plaka.Text) || !Dogrulamalar.PlakaGecerliMi(_plaka.Text))
            {
                _durumEtiketi.Text = "Geçerli bir plaka giriniz.";
                return;
            }
            if (_vknUyari.Text != "" || _tcknUyari.Text != "")
            {
                _durumEtiketi.Text = "Vergi Kimlik Numarası / T.C. Kimlik Numarası alanlarını kontrol edin.";
                return;
            }
            if (string.IsNullOrWhiteSpace(_adiSoyadiUnvani.Text))
            {
                _durumEtiketi.Text = "Adı Soyadı / Unvanı zorunludur.";
                return;
            }
            if (_vergiDairesiAdi.SelectedItem == null || _alacakliVergiDairesi.SelectedItem == null)
            {
                _durumEtiketi.Text = "Vergi Dairesi Adı ve Alacaklı Vergi Dairesi seçimi zorunludur.";
                return;
            }
            if (_aracCinsi.SelectedItem == null || _aracMarkasi.SelectedItem == null)
            {
                _durumEtiketi.Text = "Araç Cinsi ve Markası seçimi zorunludur.";
                return;
            }

            var form = new DosyaFormVerisi
            {
                VergiDairesiAdi = _vergiDairesiAdi.SelectedItem.ToString()!,
                VergiDairesiYaziTarihi = _vergiDairesiYaziTarihi.Checked ? _vergiDairesiYaziTarihi.Value : null,
                VergiDairesiYaziSayisi = string.IsNullOrWhiteSpace(_vergiDairesiYaziSayisi.Text) ? null : _vergiDairesiYaziSayisi.Text.Trim(),
                AlacakliVergiDairesi = _alacakliVergiDairesi.SelectedItem.ToString()!,
                VergiKimlikNumarasi = string.IsNullOrWhiteSpace(_vergiKimlikNumarasi.Text) ? null : _vergiKimlikNumarasi.Text.Trim(),
                TcKimlikNumarasi = string.IsNullOrWhiteSpace(_tcKimlikNumarasi.Text) ? null : _tcKimlikNumarasi.Text.Trim(),
                AdiSoyadiUnvani = _adiSoyadiUnvani.Text.Trim(),
                KanuniTemsilci = string.IsNullOrWhiteSpace(_kanuniTemsilci.Text) ? null : _kanuniTemsilci.Text.Trim(),
                Plaka = _plaka.Text.Trim(),
                AracCinsi = _aracCinsi.SelectedItem.ToString()!,
                AracMarkasi = _aracMarkasi.SelectedItem.ToString()!,
                AracTipi = _aracTipiManuel.Visible ? _aracTipiManuel.Text.Trim() : _aracTipi.SelectedItem?.ToString(),
                ModelYili = int.TryParse(_modelYili.SelectedItem?.ToString(), out var my) ? my : 0,
                Renk = _renkManuel.Visible ? _renkManuel.Text.Trim() : _renk.SelectedItem?.ToString(),
                RuhsatVar = _ruhsatVar.SelectedItem?.ToString() == "VAR",
                AnahtarVar = _anahtarVar.SelectedItem?.ToString() == "VAR",
                OtoparkAdi = _otoparkAdi.SelectedItem?.ToString(),
                HasarDurumu = string.IsNullOrWhiteSpace(_hasarDurumu.Text) ? null : _hasarDurumu.Text.Trim(),
                MuhammenBedel = ParaAyristir(_muhammenBedel.Text),
                KdvOrani = _kdvOrani.SelectedIndex switch { 1 => 10, 2 => 20, _ => 1 },
                DamgaVergisiOrani = _damgaVergisiOrani.Value,
                OtoparkaGirisTarihi = _otoparkaGirisTarihi.Checked ? _otoparkaGirisTarihi.Value : null,
                KomisyonBaskani = _oneriler?.KomisyonBaskani,
                KomisyonUye1 = _oneriler?.KomisyonUye1,
                KomisyonUye2 = _oneriler?.KomisyonUye2,
                KomisyonUye3 = _oneriler?.KomisyonUye3,
                Satis1Tarihi = _satis1Tarihi.Value,
                Satis2Tarihi = _satis2Tarihi.Value,
            };

            _kaydetButonu.Enabled = false;
            _kaydetButonu.Text = "Kaydediliyor...";
            try
            {
                var yeniDosya = await Oturum.Api.PostAsync<Dosya>("/api/dosyalar", form);
                MessageBox.Show(this, $"Dosya başarıyla oluşturuldu. Dosya No: {yeniDosya.DosyaNo}", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                var detayForm = new DosyaDetayForm(yeniDosya.Id);
                detayForm.Show(this.Owner);
                Close();
            }
            catch (OturumSuresiDolduHatasi ex)
            {
                MessageBox.Show(this, ex.Message + "\nProgram kapatılacak, lütfen yeniden açıp giriş yapın.", "Oturum Süresi Doldu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Application.Exit();
            }
            catch (ApiHatasi ex)
            {
                _durumEtiketi.Text = "Hata: " + ex.Message;
            }
            catch (Exception ex)
            {
                _durumEtiketi.Text = "Beklenmeyen hata: " + ex.Message;
            }
            finally
            {
                _kaydetButonu.Enabled = true;
                _kaydetButonu.Text = "Dosyayı Kaydet";
            }
        }
    }
}
