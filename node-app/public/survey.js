(async function () {
  const $ = id => document.getElementById(id);
  const esc = s => String(s).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

  const data = await fetch('/api/survey').then(r => r.json());
  document.title = data.surveyName;
  $('schoolName').textContent = data.schoolName;
  $('surveyName').textContent = data.surveyName;

  data.classes.forEach(c => $('className').insertAdjacentHTML('beforeend', `<option>${esc(c)}</option>`));

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

  $('surveyForm').addEventListener('submit', async e => {
    e.preventDefault();
    $('error').classList.add('d-none');

    const name = $('name').value.trim();
    const className = $('className').value;
    if (!name) return showError('Lütfen adınızı ve soyadınızı giriniz.', $('name'));
    if (!className) return showError('Lütfen sınıf seçiniz.', $('className'));

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
        body: JSON.stringify({ name, className, answers })
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
