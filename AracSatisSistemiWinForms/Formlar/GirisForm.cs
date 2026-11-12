using System.Drawing;
using System.Windows.Forms;
using AracSatisSistemiWinForms;
using AracSatisSistemiWinForms.Servisler;

namespace AracSatisSistemiWinForms.Formlar
{
    public class GirisForm : Form
    {
        private readonly TextBox _kullaniciAdiKutusu = new() { Left = 150, Top = 60, Width = 220 };
        private readonly TextBox _sifreKutusu = new() { Left = 150, Top = 100, Width = 220, UseSystemPasswordChar = true };
        private readonly TextBox _sunucuAdresiKutusu = new() { Left = 150, Top = 150, Width = 220 };
        private readonly Label _hataEtiketi = new()
        {
            Left = 20, Top = 185, Width = 460, Height = 40,
            ForeColor = Color.Firebrick, Text = "", AutoSize = false
        };
        private readonly Button _girisButonu = new() { Left = 150, Top = 230, Width = 120, Text = "Giriş Yap" };

        public GirisForm()
        {
            Text = "Araç Satış Sistemi - Giriş";
            Width = 520;
            Height = 350;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            Controls.Add(new Label { Left = 20, Top = 20, Width = 460, Text = "Araç Satış Sistemi", Font = new Font(Font.FontFamily, 14, FontStyle.Bold) });

            Controls.Add(new Label { Left = 20, Top = 63, Width = 120, Text = "Kullanıcı Adı:" });
            Controls.Add(_kullaniciAdiKutusu);

            Controls.Add(new Label { Left = 20, Top = 103, Width = 120, Text = "Şifre:" });
            Controls.Add(_sifreKutusu);

            Controls.Add(new Label { Left = 20, Top = 153, Width = 120, Text = "Sunucu Adresi:" });
            _sunucuAdresiKutusu.Text = Ayarlar.ApiAdresiniOku();
            Controls.Add(_sunucuAdresiKutusu);

            Controls.Add(_hataEtiketi);
            Controls.Add(_girisButonu);

            _girisButonu.Click += GirisButonu_Click;
            _sifreKutusu.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; _ = GirisYapAsync(); } };

            AcceptButton = _girisButonu;
        }

        private async void GirisButonu_Click(object? sender, EventArgs e) => await GirisYapAsync();

        private async Task GirisYapAsync()
        {
            _hataEtiketi.Text = "";

            var kullaniciAdi = _kullaniciAdiKutusu.Text.Trim();
            var sifre = _sifreKutusu.Text;
            var sunucuAdresi = _sunucuAdresiKutusu.Text.Trim();

            if (string.IsNullOrWhiteSpace(kullaniciAdi) || string.IsNullOrWhiteSpace(sifre))
            {
                _hataEtiketi.Text = "Kullanıcı adı ve şifre zorunludur.";
                return;
            }
            if (string.IsNullOrWhiteSpace(sunucuAdresi))
            {
                _hataEtiketi.Text = "Sunucu adresi zorunludur.";
                return;
            }

            Ayarlar.ApiAdresiniKaydet(sunucuAdresi);
            Oturum.YenidenOlustur();

            _girisButonu.Enabled = false;
            _girisButonu.Text = "Giriş yapılıyor...";
            try
            {
                await Oturum.Api.GirisYapAsync(kullaniciAdi, sifre);

                var anaForm = new AnaForm();
                Hide();
                anaForm.FormClosed += (s, e) => Close();
                anaForm.Show();
            }
            catch (ApiHatasi ex)
            {
                _hataEtiketi.Text = ex.Message;
            }
            catch (Exception ex)
            {
                _hataEtiketi.Text = "Beklenmeyen bir hata oluştu: " + ex.Message;
            }
            finally
            {
                _girisButonu.Enabled = true;
                _girisButonu.Text = "Giriş Yap";
            }
        }
    }
}
