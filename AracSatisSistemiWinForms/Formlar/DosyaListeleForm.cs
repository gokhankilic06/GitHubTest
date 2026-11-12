using System.Windows.Forms;
using AracSatisSistemiWinForms;
using AracSatisSistemiWinForms.Modeller;
using AracSatisSistemiWinForms.Servisler;

namespace AracSatisSistemiWinForms.Formlar
{
    public class DosyaListeleForm : Form
    {
        private readonly TextBox _aramaKutusu = new() { Left = 20, Top = 20, Width = 250 };
        private readonly ComboBox _durumFiltresi = new() { Left = 280, Top = 20, Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly Button _araButonu = new() { Left = 450, Top = 18, Width = 100, Text = "Ara" };
        private readonly Button _yeniDosyaButonu = new() { Left = 560, Top = 18, Width = 140, Text = "+ Yeni Dosya" };
        private readonly DataGridView _tablo = new()
        {
            Left = 20, Top = 55, Width = 940, Height = 480,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
        };
        private readonly Label _durumEtiketi = new() { Left = 20, Top = 545, Width = 900, Text = "" };

        private List<DosyaOzeti> _sonListe = new();

        public DosyaListeleForm()
        {
            Text = "Dosya Ara / Listele";
            Width = 1000;
            Height = 650;
            StartPosition = FormStartPosition.CenterScreen;

            _durumFiltresi.Items.Add("Tümü (Aktif + Pasif)");
            _durumFiltresi.Items.Add("Sadece Aktif");
            _durumFiltresi.Items.Add("Sadece Pasif");
            _durumFiltresi.SelectedIndex = 0;

            Controls.Add(new Label { Left = 20, Top = 2, Width = 250, Text = "Dosya No / Plaka / Adı Soyadı" });
            Controls.Add(_aramaKutusu);
            Controls.Add(_durumFiltresi);
            Controls.Add(_araButonu);
            Controls.Add(_yeniDosyaButonu);
            Controls.Add(_tablo);
            Controls.Add(_durumEtiketi);

            _tablo.Columns.Add("DosyaNo", "Dosya No");
            _tablo.Columns.Add("KayitTarihi", "Kayıt Tarihi");
            _tablo.Columns.Add("Plaka", "Plaka");
            _tablo.Columns.Add("AdiSoyadiUnvani", "Adı Soyadı / Unvanı");
            _tablo.Columns.Add("VergiDairesiAdi", "Vergi Dairesi");
            _tablo.Columns.Add("GenelDurumGorunen", "Genel Durum");
            _tablo.Columns.Add("AsamaGorunen", "Aşama");

            _araButonu.Click += async (s, e) => await ListeyiYenileAsync();
            _yeniDosyaButonu.Click += (s, e) =>
            {
                var form = new DosyaOlusturForm();
                form.ShowDialog(this);
                _ = ListeyiYenileAsync();
            };
            _tablo.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= _sonListe.Count) return;
                var dosya = _sonListe[e.RowIndex];
                var detayForm = new DosyaDetayForm(dosya.Id);
                detayForm.ShowDialog(this);
            };

            Load += async (s, e) => await ListeyiYenileAsync();
        }

        private async Task ListeyiYenileAsync()
        {
            _durumEtiketi.Text = "Yükleniyor...";
            _tablo.Rows.Clear();
            try
            {
                var durumFiltreDegeri = _durumFiltresi.SelectedIndex switch
                {
                    1 => "aktif",
                    2 => "pasif",
                    _ => ""
                };
                var arama = _aramaKutusu.Text.Trim();
                var yol = $"/api/dosyalar?arama={Uri.EscapeDataString(arama)}&durumFiltre={durumFiltreDegeri}";
                _sonListe = await Oturum.Api.GetAsync<List<DosyaOzeti>>(yol);

                foreach (var d in _sonListe)
                {
                    _tablo.Rows.Add(d.DosyaNo, d.KayitTarihi.ToString("dd.MM.yyyy HH:mm"), d.Plaka, d.AdiSoyadiUnvani, d.VergiDairesiAdi, d.GenelDurumGorunen, d.AsamaGorunen);
                }
                _durumEtiketi.Text = _sonListe.Count == 0 ? "Kayıt bulunamadı." : $"{_sonListe.Count} kayıt listelendi. (Detay için bir satıra çift tıklayın.)";
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
        }
    }
}
