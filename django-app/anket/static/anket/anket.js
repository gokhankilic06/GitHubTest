// Veli formu: eksik bilgileri gönderimden önce işaretler.
// Sunucu tarafı doğrulama her durumda yapılır; bu dosya yalnızca kullanım kolaylığı sağlar.
(function () {
  const $ = id => document.getElementById(id);
  const form = $('surveyForm');

  form.addEventListener('change', e => {
    const q = e.target.closest('.question');
    if (q) q.classList.remove('missing');
  });

  form.addEventListener('submit', e => {
    let firstMissing = null;
    form.querySelectorAll('.question').forEach(q => {
      if (!q.querySelector('input:checked')) {
        q.classList.add('missing');
        firstMissing = firstMissing || q;
      }
    });
    const msgs = [];
    if (!$('name').value.trim()) msgs.push('Lütfen adınızı ve soyadınızı giriniz.');
    if (!$('className').value) msgs.push('Lütfen sınıf seçiniz.');
    if (firstMissing) msgs.push('Lütfen tüm soruları yanıtlayınız. Eksik sorular kırmızı ile işaretlenmiştir.');

    if (msgs.length) {
      e.preventDefault();
      const err = $('error');
      err.innerHTML = '';
      msgs.forEach(m => { const d = document.createElement('div'); d.textContent = m; err.appendChild(d); });
      err.classList.remove('d-none');
      const target = !$('name').value.trim() ? $('name') : !$('className').value ? $('className') : firstMissing;
      target.scrollIntoView({ behavior: 'smooth', block: 'center' });
      return;
    }
    $('submitBtn').disabled = true;
  });

  // Sunucudan hata ile dönüldüyse hata kutusuna kaydır
  if (!$('error').classList.contains('d-none')) {
    const target = document.querySelector('.question.missing') || $('error');
    target.scrollIntoView({ block: 'center' });
  }
})();
