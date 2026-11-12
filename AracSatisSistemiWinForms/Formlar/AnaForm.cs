using System.Drawing;
using System.Windows.Forms;
using AracSatisSistemiWinForms;

namespace AracSatisSistemiWinForms.Formlar
{
    public class AnaForm : Form
    {
        public AnaForm()
        {
            Text = "Araç Satış Sistemi";
            Width = 560;
            Height = 420;
            StartPosition = FormStartPosition.CenterScreen;

            var baslik = new Label
            {
                Left = 20, Top = 20, Width = 500,
                Text = "Araç Satış Programı",
                Font = new Font(Font.FontFamily, 16, FontStyle.Bold)
            };
            Controls.Add(baslik);

            var kullaniciEtiketi = new Label
            {
                Left = 20, Top = 55, Width = 500,
                Text = $"Giriş yapan: {AracSatisSistemiWinForms.Oturum.Api.KullaniciAdiSoyad}",
                ForeColor = Color.DimGray
            };
            Controls.Add(kullaniciEtiketi);

            var kayitButonu = ButonOlustur("Kayıt İşlemleri", 20, 100);
            kayitButonu.Click += (s, e) =>
            {
                var form = new DosyaOlusturForm();
                form.ShowDialog(this);
            };
            Controls.Add(kayitButonu);

            var listeButonu = ButonOlustur("Dosya Ara / Listele", 20, 160);
            listeButonu.Click += (s, e) =>
            {
                var form = new DosyaListeleForm();
                form.ShowDialog(this);
            };
            Controls.Add(listeButonu);

            var cikisButonu = ButonOlustur("Çıkış Yap", 20, 240);
            cikisButonu.BackColor = Color.MistyRose;
            // Not: "Çıkış Yap", programı tamamen kapatır (her bilgisayarda tek bir
            // kullanıcı oturumu açık kalır); aynı bilgisayarda başka biriyle giriş
            // yapmak için program yeniden açılır.
            cikisButonu.Click += (s, e) =>
            {
                AracSatisSistemiWinForms.Oturum.Api.CikisYap();
                Close();
            };
            Controls.Add(cikisButonu);
        }

        private static Button ButonOlustur(string metin, int left, int top) => new()
        {
            Left = left,
            Top = top,
            Width = 300,
            Height = 50,
            Text = metin,
            Font = new Font(Control.DefaultFont.FontFamily, 11)
        };
    }
}
