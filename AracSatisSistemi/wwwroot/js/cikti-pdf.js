// Ortak PDF/Yazdırma yardımcı fonksiyonu.
// Önceden her sayfa kendi içinde tüm alanı TEK bir resme sıkıştırıp sayfaya yapıştırıyordu;
// içerik bir A4 sayfasından uzun olduğunda bu resim orantılı olarak çok büyüyüp sayfa
// sınırının dışına taşıyordu (çıktı "çok büyük" görünüyordu), kısa içerikte ise sayfanın
// büyük kısmı boş kalıyordu. Bu fonksiyon alanı tek bir canvas'a yakalayıp, gerekirse
// birden fazla A4 sayfasına bölerek ekler; böylece her sayfa gerçek ölçeğinde kalır.
function alanDanPdfOlustur(alan, pdf, secenekler) {
    secenekler = secenekler || {};
    const kenarBosluk = secenekler.kenarBosluk ?? 10;
    const sayfaGenislikMm = pdf.internal.pageSize.getWidth();
    const sayfaYukseklikMm = pdf.internal.pageSize.getHeight();
    const genislik = sayfaGenislikMm - (2 * kenarBosluk);
    const kullanilabilirYukseklikMm = sayfaYukseklikMm - (2 * kenarBosluk);

    return html2canvas(alan, { scale: 2 }).then(canvas => {
        const pxPerMm = canvas.width / genislik;
        const sayfaYukseklikPx = Math.floor(kullanilabilirYukseklikMm * pxPerMm);

        if (canvas.height <= sayfaYukseklikPx) {
            const yukseklik = canvas.height / pxPerMm;
            pdf.addImage(canvas.toDataURL('image/png'), 'PNG', kenarBosluk, kenarBosluk, genislik, yukseklik);
            return pdf;
        }

        let kalan = canvas.height;
        let offset = 0;
        let ilkParca = true;
        while (kalan > 0) {
            const buParcaPx = Math.min(sayfaYukseklikPx, kalan);
            const parca = document.createElement('canvas');
            parca.width = canvas.width;
            parca.height = buParcaPx;
            parca.getContext('2d').drawImage(
                canvas, 0, offset, canvas.width, buParcaPx,
                0, 0, canvas.width, buParcaPx
            );

            if (!ilkParca) pdf.addPage();
            const yukseklikMm = buParcaPx / pxPerMm;
            pdf.addImage(parca.toDataURL('image/png'), 'PNG', kenarBosluk, kenarBosluk, genislik, yukseklikMm);

            offset += buParcaPx;
            kalan -= buParcaPx;
            ilkParca = false;
        }
        return pdf;
    });
}
