(function () {
  const $ = id => document.getElementById(id);
  const esc = s => String(s).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

  function showLogin() {
    $('adminView').classList.add('d-none');
    $('loginView').classList.remove('d-none');
    $('password').focus();
  }

  async function load() {
    const res = await fetch('/api/admin/data');
    if (res.status === 401) return showLogin();
    render(await res.json());
    $('loginView').classList.add('d-none');
    $('adminView').classList.remove('d-none');
  }

  function render(d) {
    const n = d.questions.length;
    const total = d.responses.length;

    // Katılımcı listesi
    $('participantCount').textContent = `${total} kişi ankete katılmıştır.`;
    $('responsesBody').innerHTML = total
      ? d.responses.map((r, i) => `
        <tr>
          <td>${i + 1}</td>
          <td>${esc(r.name)}</td>
          <td class="text-primary">${esc(r.className)}</td>
          <td>${r.answers.join(', ')}</td>
          <td><button class="btn btn-danger btn-sm py-0" data-delete="${r.id}" data-name="${esc(r.name)}">Sil</button></td>
        </tr>`).join('')
      : '<tr><td colspan="5" class="text-center text-muted">Henüz yanıt yok.</td></tr>';

    // Soru bazlı dağılım
    $('distributionBody').innerHTML = d.stats.map((s, i) => `
      <tr>
        <td>${i + 1}. Soru</td>
        <td>${s.countA} (${s.percentA}%)<div class="bar"><span style="width:${s.percentA}%"></span></div></td>
        <td>${s.countB} (${s.percentB}%)<div class="bar"><span style="width:${s.percentB}%"></span></div></td>
      </tr>`).join('');

    // Bireysel yanıt tablosu
    let head1 = '<tr><th rowspan="2">No</th><th rowspan="2" class="name">Adı Soyadı</th>';
    let head2 = '<tr>';
    for (let i = 0; i < n; i++) {
      head1 += `<th colspan="2">M${i + 1}</th>`;
      head2 += '<th>A</th><th>B</th>';
    }
    $('individualHead').innerHTML = head1 + '</tr>' + head2 + '</tr>';
    $('individualBody').innerHTML = total
      ? d.responses.map((r, i) => `
        <tr>
          <td>${i + 1}</td>
          <td class="name">${esc(r.name)}</td>
          ${r.answers.map(a => `<td class="mark">${a === 'A' ? 1 : ''}</td><td class="mark">${a === 'B' ? 1 : ''}</td>`).join('')}
        </tr>`).join('')
      : `<tr><td colspan="${2 + n * 2}" class="text-center text-muted">Henüz yanıt yok.</td></tr>`;

    // Şık bazlı sonuçlar
    $('filteredBody').innerHTML = d.questions.map((q, i) => {
      const s = d.stats[i];
      return `
        <tr><td>${esc(q.shortA)}</td><td>${s.countA}</td><td>${s.percentA}%</td></tr>
        <tr><td>${esc(q.shortB)}</td><td>${s.countB}</td><td>${s.percentB}%</td></tr>`;
    }).join('');
  }

  $('loginForm').addEventListener('submit', async e => {
    e.preventDefault();
    $('loginError').classList.add('d-none');
    const res = await fetch('/api/admin/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ password: $('password').value })
    });
    if (!res.ok) {
      $('loginError').textContent = (await res.json()).error || 'Giriş başarısız.';
      $('loginError').classList.remove('d-none');
      return;
    }
    $('password').value = '';
    load();
  });

  $('logoutBtn').addEventListener('click', async () => {
    await fetch('/api/admin/logout', { method: 'POST' });
    showLogin();
  });

  $('responsesBody').addEventListener('click', async e => {
    const btn = e.target.closest('[data-delete]');
    if (!btn) return;
    if (!confirm(`"${btn.dataset.name}" adlı velinin yanıtları silinsin mi?`)) return;
    await fetch('/api/admin/responses/' + btn.dataset.delete, { method: 'DELETE' });
    load();
  });

  $('resetBtn').addEventListener('click', async () => {
    if (!confirm('TÜM anket verileri silinecek. Bu işlem geri alınamaz. Emin misiniz?')) return;
    await fetch('/api/admin/reset', { method: 'POST' });
    load();
  });

  load();
})();
