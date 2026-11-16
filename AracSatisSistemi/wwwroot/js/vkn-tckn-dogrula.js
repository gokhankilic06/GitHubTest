// T.C. Kimlik Numarası ve Vergi Kimlik Numarası için resmi kontrol hanesi (checksum)
// algoritmaları - sunucu tarafındaki Data/VknTcknDogrulayici.cs ile BİREBİR aynı mantık,
// kullanıcıya yazarken anlık geri bildirim vermek için. NOT: Bu kontrol yalnızca biçimsel
// geçerliliği gösterir, numaranın gerçekten var olup olmadığını doğrulamaz.

function tcknGecerliMi(deger) {
    if (!deger || deger.length !== 11 || !/^\d{11}$/.test(deger)) return false;
    if (deger[0] === '0') return false;

    const d = deger.split('').map(Number);
    const tekToplam = d[0] + d[2] + d[4] + d[6] + d[8];
    const ciftToplam = d[1] + d[3] + d[5] + d[7];
    const hamHane10 = (tekToplam * 7 - ciftToplam) % 10;
    const hane10 = ((hamHane10 % 10) + 10) % 10;
    if (hane10 !== d[9]) return false;

    const hane11 = d.slice(0, 10).reduce((a, b) => a + b, 0) % 10;
    return hane11 === d[10];
}

function vknGecerliMi(deger) {
    if (!deger || deger.length !== 10 || !/^\d{10}$/.test(deger)) return false;

    const d = deger.split('').map(Number);
    let toplam = 0;
    for (let i = 0; i < 9; i++) {
        const x = (d[i] + 9 - i) % 10;
        toplam += x === 9 ? 9 : (x * Math.pow(2, 9 - i)) % 9;
    }
    const kontrolHanesi = (10 - (toplam % 10)) % 10;
    return kontrolHanesi === d[9];
}

// Bir input'a canlı doğrulama bağlar: tam uzunlukta ama kontrol hanesi tutmuyorsa
// kırmızı uyarı metni gösterir. `dogrulaFn` tcknGecerliMi veya vknGecerliMi olmalı.
function vknTcknCanliDogrulamaBagla(inputEl, uyariEl, dogrulaFn, beklenenUzunluk) {
    function kontrolEt() {
        const deger = inputEl.value.replace(/\D/g, '');
        if (deger.length !== beklenenUzunluk) {
            uyariEl.style.display = 'none';
            inputEl.classList.remove('is-invalid');
            return;
        }
        const gecerliMi = dogrulaFn(deger);
        uyariEl.style.display = gecerliMi ? 'none' : '';
        inputEl.classList.toggle('is-invalid', !gecerliMi);
    }
    inputEl.addEventListener('input', kontrolEt);
    inputEl.addEventListener('blur', kontrolEt);
    kontrolEt();
}
