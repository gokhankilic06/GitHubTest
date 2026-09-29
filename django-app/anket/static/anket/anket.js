// Veli formu: kodu anında doğrular ve eksik soruları gönderimden önce işaretler.
// Sunucu tarafı doğrulama her durumda yapılır; bu dosya yalnızca kullanım kolaylığı sağlar.
(function () {
  const $ = id => document.getElementById(id);
  const form = $('surveyForm');
  const normalize = v => v.toUpperCase().replace(/[^A-Z0-9]/g, '');
  const defaultHelp = 'Kodunuzu öğretmeninizden alabilirsiniz. Her kod yalnızca bir kez kullanılabilir.';

  function setCodeState(state, message) {
    $('code').classList.toggle('is-valid', state === 'ok');
    $('code').classList.toggle('is-invalid', state === 'error');
    $('codeHelp').textContent = message;
    $('codeHelp').className = 'form-text' + (state === 'error' ? ' text-danger' : state === 'ok' ? ' text-success' : '');
  }

  async function verifyCode() {
    const code = normalize($('code').value);
    $('code').value = code;
    $('className').value = '';
    if (!code) return setCodeState('error', 'Lütfen veli kodunu giriniz.');
    try {
      const res = await fetch(form.dataset.kodUrl.replace('KOD', encodeURIComponent(code)));
      const out = await res.json();
      if (!res.ok) return setCodeState('error', out.error || 'Kod doğrulanamadı.');
      $('className').value = out.className;
      setCodeState('ok', `Kod doğrulandı. Sınıf: ${out.className}`);
    } catch {
      setCodeState('error', 'Bağlantı hatası. Lütfen tekrar deneyiniz.');
    }
  }

  $('checkCodeBtn').addEventListener('click', verifyCode);
  $('code').addEventListener('change', verifyCode);
  $('code').addEventListener('input', () => {
    if ($('className').value) {
      $('className').value = '';
      setCodeState('', defaultHelp);
    }
  });

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
    const err = $('error');
    if (!normalize($('code').value) || !$('name').value.trim() || firstMissing) {
      e.preventDefault();
      const msgs = [];
      if (!normalize($('code').value)) msgs.push('Lütfen veli kodunu giriniz.');
      if (!$('name').value.trim()) msgs.push('Lütfen adınızı ve soyadınızı giriniz.');
      if (firstMissing) msgs.push('Lütfen tüm soruları yanıtlayınız. Eksik sorular kırmızı ile işaretlenmiştir.');
      err.innerHTML = '';
      msgs.forEach(m => { const d = document.createElement('div'); d.textContent = m; err.appendChild(d); });
      err.classList.remove('d-none');
      (firstMissing || $('code')).scrollIntoView({ behavior: 'smooth', block: 'center' });
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
