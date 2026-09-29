(async function () {
  const $ = id => document.getElementById(id);
  const esc = s => String(s).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
  const normalize = v => v.toUpperCase().replace(/[^A-Z0-9]/g, '');

  const data = await fetch('/api/survey').then(r => r.json());
  document.title = data.surveyName;
  $('schoolName').textContent = data.schoolName;
  $('surveyName').textContent = data.surveyName;

  $('questions').innerHTML = data.questions.map((q, i) => `
    <div class="question" id="q${i}">
      <div class="question-title">${i + 1}. ${esc(data.questionText)}</div>
      <div class="form-check">
        <input class="form-check-input" type="radio" name="q${i}" id="q${i}a" value="A">
        <label class="form-check-label" for="q${i}a">A) ${esc(q.a)}</label>
      </div>
      <div class="form-check">
        <input class="form-check-input" type="radio" name="q${i}" id="q${i}b" value="B">
        <label class="form-check-label" for="q${i}b">B) ${esc(q.b)}</label>
      </div>
    </div>`).join('');

  $('questions').addEventListener('change', e => e.target.closest('.question').classList.remove('missing'));

  function showError(msg, focusEl) {
    $('error').textContent = msg;
    $('error').classList.remove('d-none');
    if (focusEl) focusEl.scrollIntoView({ behavior: 'smooth', block: 'center' });
  }

  // ---------- Veli kodu doğrulama ----------
  let verifiedCode = null;

  function setCodeState(state, message) {
    const input = $('code');
    input.classList.toggle('is-valid', state === 'ok');
    input.classList.toggle('is-invalid', state === 'error');
    $('codeHelp').textContent = message;
    $('codeHelp').className = 'form-text' + (state === 'error' ? ' text-danger' : state === 'ok' ? ' text-success' : '');
  }

  async function verifyCode() {
    const code = normalize($('code').value);
    $('code').value = code;
    verifiedCode = null;
    $('className').value = '';
    if (!code) {
      setCodeState('error', 'Lütfen veli kodunu giriniz.');
      return false;
    }
    try {
      const res = await fetch('/api/codes/' + encodeURIComponent(code));
      const out = await res.json();
      if (!res.ok) {
        setCodeState('error', out.error || 'Kod doğrulanamadı.');
        return false;
      }
      verifiedCode = code;
      $('className').value = out.className;
      setCodeState('ok', `Kod doğrulandı. Sınıf: ${out.className}`);
      return true;
    } catch {
      setCodeState('error', 'Bağlantı hatası. Lütfen tekrar deneyiniz.');
      return false;
    }
  }

  $('checkCodeBtn').addEventListener('click', verifyCode);
  $('code').addEventListener('change', verifyCode);
  $('code').addEventListener('input', () => {
    if (verifiedCode && normalize($('code').value) !== verifiedCode) {
      verifiedCode = null;
      $('className').value = '';
      setCodeState('', 'Kodunuzu öğretmeninizden alabilirsiniz. Her kod yalnızca bir kez kullanılabilir.');
    }
  });

  // Bağlantı ile gelen kod: /?kod=ABC123
  const urlCode = new URLSearchParams(location.search).get('kod');
  if (urlCode) {
    $('code').value = urlCode;
    verifyCode();
  }

  // ---------- Gönderim ----------
  $('surveyForm').addEventListener('submit', async e => {
    e.preventDefault();
    $('error').classList.add('d-none');

    if (!verifiedCode && !(await verifyCode())) return showError('Lütfen geçerli bir veli kodu giriniz.', $('code'));

    const name = $('name').value.trim();
    if (!name) return showError('Lütfen adınızı ve soyadınızı giriniz.', $('name'));

    const answers = [];
    let firstMissing = null;
    data.questions.forEach((_, i) => {
      const sel = document.querySelector(`input[name="q${i}"]:checked`);
      answers.push(sel ? sel.value : null);
      if (!sel) {
        $('q' + i).classList.add('missing');
        firstMissing = firstMissing || $('q' + i);
      }
    });
    if (firstMissing) return showError('Lütfen tüm soruları yanıtlayınız. Eksik sorular kırmızı ile işaretlenmiştir.', firstMissing);

    $('submitBtn').disabled = true;
    try {
      const res = await fetch('/api/responses', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ code: verifiedCode, name, answers })
      });
      const out = await res.json();
      if (!res.ok) throw new Error(out.error || 'Bir hata oluştu.');
      $('surveyForm').classList.add('d-none');
      $('thanks').classList.remove('d-none');
      window.scrollTo({ top: 0, behavior: 'smooth' });
    } catch (err) {
      showError(err.message);
      $('submitBtn').disabled = false;
    }
  });
})();
