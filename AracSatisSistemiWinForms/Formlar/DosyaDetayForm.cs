using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using AracSatisSistemiWinForms;
using AracSatisSistemiWinForms.Modeller;
using AracSatisSistemiWinForms.Servisler;

namespace AracSatisSistemiWinForms.Formlar
{
    public class DosyaDetayForm : Form
    {
        private readonly int _dosyaId;
        private readonly Panel _icerikPaneli = new() { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20) };
        private readonly Label _basliketiketi = new() { Dock = DockStyle.Top, Height = 40, Font = new Font(Control.DefaultFont.FontFamily, 13, FontStyle.Bold) };
        private static readonly CultureInfo TrTR = new("tr-TR");

        public DosyaDetayForm(int dosyaId)
        {
            _dosyaId = dosyaId;
            Text = "Dosya Detayı";
            Width = 720;
            Height = 760;
            StartPosition = FormStartPosition.CenterScreen;

            Controls.Add(_icerikPaneli);
            Controls.Add(_basliketiketi);

            Load += async (s, e) => await YukleAsync();
        }

        private int _y;

        private void BolumBasligiEkle(string baslik)
        {
            var etiket = new Label
            {
                Left = 0, Top = _y, Width = 640, Height = 28,
                Text = baslik, Font = new Font(Font.FontFamily, 10, FontStyle.Bold),
                BackColor = Color.FromArgb(30, 41, 59), ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0)
            };
            _icerikPaneli.Controls.Add(etiket);
            _y += 36;
        }

        private void SatirEkle(string etiket, string? deger)
        {
            _icerikPaneli.Controls.Add(new Label { Left = 0, Top = _y, Width = 220, Text = etiket, Font = new Font(Font, FontStyle.Bold) });
            _icerikPaneli.Controls.Add(new Label { Left = 230, Top = _y, Width = 420, Text = deger ?? "" });
            _y += 26;
        }

        private async Task YukleAsync()
        {
            _basliketiketi.Text = "Yükleniyor...";
            try
            {
                var d = await Oturum.Api.GetAsync<Dosya>($"/api/dosyalar/{_dosyaId}");
                _basliketiketi.Text = $"Dosya No: {d.DosyaNo} - {d.DurumGorunenAd}";

                BolumBasligiEkle("İlgili Vergi Dairesi");
                SatirEkle("Vergi Dairesi Adı:", d.VergiDairesiAdi);
                SatirEkle("Vergi Dairesi Yazısı Tarihi:", d.VergiDairesiYaziTarihi?.ToString("dd.MM.yyyy"));
                SatirEkle("Vergi Dairesi Yazısı Sayısı:", d.VergiDairesiYaziSayisi);
                SatirEkle("Alacaklı Vergi Dairesi:", d.AlacakliVergiDairesi);

                BolumBasligiEkle("Borçlu Mükellef");
                SatirEkle("Adı Soyadı / Unvanı:", d.AdiSoyadiUnvani);
                SatirEkle("T.C. Kimlik No:", d.TcKimlikNumarasi);
                SatirEkle("Vergi Kimlik No:", d.VergiKimlikNumarasi);
                SatirEkle("Kanuni Temsilci:", d.KanuniTemsilci);

                BolumBasligiEkle("Araç Bilgileri");
                SatirEkle("Plaka:", d.Plaka);
                SatirEkle("Markası:", d.AracMarkasi);
                SatirEkle("Cinsi:", d.AracCinsi);
                SatirEkle("Tipi:", d.AracTipi);
                SatirEkle("Model Yılı:", d.ModelYili.ToString());
                SatirEkle("Renk:", d.Renk);
                SatirEkle("Ruhsat:", d.RuhsatVar ? "VAR" : "YOK");
                SatirEkle("Anahtar:", d.AnahtarVar ? "VAR" : "YOK");
                SatirEkle("Çekici:", d.CekiciVar ? "VAR" : "YOK");
                SatirEkle("Otopark:", d.OtoparkAdi);
                SatirEkle("Otoparka Giriş Tarihi:", d.OtoparkaGirisTarihi?.ToString("dd.MM.yyyy"));
                SatirEkle("Hasar Durumu:", d.HasarDurumu);
                SatirEkle("Muhammen Bedel:", d.MuhammenBedel.ToString("N2", TrTR) + " TL");
                SatirEkle("Teminat (%5):", d.Teminat.ToString("N2", TrTR) + " TL");
                SatirEkle("KDV Oranı:", $"%{d.KdvOrani}");
                SatirEkle($"Damga Vergisi (Binde {d.DamgaVergisiOrani:0.00}):", d.DamgaVergisiTutari.ToString("N2", TrTR) + " TL");

                if (d.Satislar.Count == 0)
                {
                    BolumBasligiEkle("Planlanan İhale Tarihleri");
                    SatirEkle("1. Satış Tarihi:", d.Satis1Tarihi.ToString("dd.MM.yyyy"));
                    SatirEkle("2. Satış Tarihi:", d.Satis2Tarihi.ToString("dd.MM.yyyy"));
                }
                else
                {
                    BolumBasligiEkle("Satış Bilgileri");
                    var tablo = new DataGridView
                    {
                        Left = 0, Top = _y, Width = 640, Height = 150,
                        ReadOnly = true, AllowUserToAddRows = false,
                        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
                    };
                    tablo.Columns.Add("Tur", "Tür");
                    tablo.Columns.Add("Tarih", "Tarih");
                    tablo.Columns.Add("Sonuc", "Sonuç");
                    tablo.Columns.Add("Bedel", "Bedel (TL)");
                    tablo.Columns.Add("Alici", "Alıcı");
                    foreach (var s in d.Satislar)
                    {
                        tablo.Rows.Add(s.SatisTuruGorunen, s.SatisTarihi.ToString("dd.MM.yyyy"), s.SonucGorunen,
                            s.SatisBedeli?.ToString("N2", TrTR) ?? "-", s.AliciAdiSoyadi);
                    }
                    _icerikPaneli.Controls.Add(tablo);
                    _y += 160;
                }
            }
            catch (OturumSuresiDolduHatasi ex)
            {
                MessageBox.Show(this, ex.Message + "\nProgram kapatılacak, lütfen yeniden açıp giriş yapın.", "Oturum Süresi Doldu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Application.Exit();
            }
            catch (Exception ex)
            {
                _basliketiketi.Text = "Hata";
                SatirEkle("Dosya yüklenemedi:", ex.Message);
            }
        }
    }
}
