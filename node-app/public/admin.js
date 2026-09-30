(function () {
  const $ = id => document.getElementById(id);
  const esc = s => String(s).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
  const css = name => getComputedStyle(document.documentElement).getPropertyValue(name).trim();

  let survey = null;         // okul adı, sorular, sınıflar
  let questionChart = null;
  let classChart = null;

  async function api(url, options = {}) {
    const res = await fetch(url, {
      ...options,
      headers: options.body ? { 'Content-Type': 'application/json' } : undefined
    });
    if (res.status === 401) {
      showLogin();
      throw new Error('Yetkisiz');
    }
    const out = await res.json();
    if (!res.ok) throw new Error(out.error || 'Bir hata oluştu.');
    return out;
  }

  // ---------- Giriş / sekmeler ----------
  function showLogin() {
    $('adminView').classList.add('d-none');
    $('loginView').classList.remove('d-none');
    $('password').focus();
  }

  function showPanel() {
    $('loginView').classList.add('d-none');
    $('adminView').classList.remove('d-none');
  }

  function fillClassSelect(select, withAll) {
    const current = select.value;
    select.innerHTML = (withAll ? '<option value="">Tüm Sınıflar</option>' : '') +
      survey.classes.map(c => `<option>${esc(c)}</option>`).join('');
    if (current) select.value = current;
  }

  async function init() {
    const res = await fetch('/api/admin/data');
    if (res.status === 401) return showLogin();
    const d = await res.json();
    survey = d;
    fillClassSelect($('classFilter'), true);
    renderResults(d);
    showPanel();
  }

  // ---------- Sonuçlar ----------
  async function loadResults() {
    const cls = $('classFilter').value;
    const d = await api('/api/admin/data' + (cls ? '?class=' + encodeURIComponent(cls) : ''));
    renderResults(d);
  }

  $('classFilter').addEventListener('change', () => {
    const cls = $('classFilter').value;
    $('excelBtn').href = '/api/admin/export.xlsx' + (cls ? '?class=' + encodeURIComponent(cls) : '');
    loadResults();
  });

  function renderResults(d) {
    const n = d.questions.length;
    const total = d.responses.length;
    const scope = d.className ? `${d.className} sınıfından ` : '';

    $('participantCount').textContent = `${scope}${total} kişi ankete katılmıştır.`;
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

    $('distributionBody').innerHTML = d.stats.map((s, i) => `
      <tr>
        <td>${i + 1}. Soru</td>
        <td>${s.countA} (${s.percentA}%)<div class="bar"><span class="a" style="width:${s.percentA}%"></span></div></td>
        <td>${s.countB} (${s.percentB}%)<div class="bar"><span class="b" style="width:${s.percentB}%"></span></div></td>
      </tr>`).join('');

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

    $('filteredBody').innerHTML = d.questions.map((q, i) => {
      const s = d.stats[i];
      return `
        <tr><td>${esc(q.shortA)}</td><td>${s.countA}</td><td>${s.percentA}%</td></tr>
        <tr><td>${esc(q.shortB)}</td><td>${s.countB}</td><td>${s.percentB}%</td></tr>`;
    }).join('');

    $('classSummaryBody').innerHTML = d.classSummary.length
      ? d.classSummary.map(s => `
        <tr><td>${esc(s.className)}</td><td>${s.participants}</td></tr>`).join('')
      : '<tr><td colspan="2" class="text-center text-muted">Henüz veri yok.</td></tr>';

    renderCharts(d);
  }

  // ---------- Grafikler ----------
  function renderCharts(d) {
    if (typeof Chart === 'undefined') return;
    const colorA = css('--series-a');
    const colorB = css('--series-b');
    const surface = css('--chart-surface');
    const ink = css('--chart-ink');
    const grid = css('--chart-grid');
    Chart.defaults.color = ink;
    Chart.defaults.font.family = getComputedStyle(document.body).fontFamily;

    const labels = d.questions.map((_, i) => `${i + 1}. Soru`);
    const bar = { borderColor: surface, borderWidth: 2, borderSkipped: false, borderRadius: 4, barPercentage: 0.8, maxBarThickness: 28 };
    const datasets = [
      { label: 'A seçeneği', data: d.stats.map(s => s.percentA), counts: d.stats.map(s => s.countA), texts: d.questions.map(q => q.shortA), backgroundColor: colorA, ...bar },
      { label: 'B seçeneği', data: d.stats.map(s => s.percentB), counts: d.stats.map(s => s.countB), texts: d.questions.map(q => q.shortB), backgroundColor: colorB, ...bar }
    ];

    if (questionChart) {
      questionChart.data.datasets.forEach((ds, i) => Object.assign(ds, datasets[i]));
      questionChart.update();
    } else {
      questionChart = new Chart($('questionChart'), {
        type: 'bar',
        data: { labels, datasets },
        options: {
          indexAxis: 'y',
          maintainAspectRatio: false,
          animation: { duration: 250 },
          plugins: {
            legend: { display: false },
            tooltip: {
              callbacks: {
                title: items => items[0].label,
                label: ctx => {
                  const ds = ctx.dataset;
                  return `${ds.label === 'A seçeneği' ? 'A' : 'B'}) ${ds.texts[ctx.dataIndex]}: ${ds.counts[ctx.dataIndex]} veli (${ctx.parsed.x}%)`;
                }
              }
            }
          },
          scales: {
            x: { stacked: true, min: 0, max: 100, ticks: { callback: v => v + '%' }, grid: { color: grid }, border: { display: false } },
            y: { stacked: true, grid: { display: false }, border: { display: false } }
          }
        }
      });
    }

    const summary = d.classSummary.filter(s => s.participants > 0);
    $('classChartEmpty').classList.toggle('d-none', summary.length > 0);
    $('classChart').parentElement.classList.toggle('d-none', summary.length === 0);
    const classData = {
      labels: summary.map(s => s.className),
      datasets: [{ label: 'Katılan veli', data: summary.map(s => s.participants), backgroundColor: colorA, ...bar }]
    };
    if (classChart) {
      classChart.data = classData;
      classChart.update();
    } else {
      classChart = new Chart($('classChart'), {
        type: 'bar',
        data: classData,
        options: {
          indexAxis: 'y',
          maintainAspectRatio: false,
          animation: { duration: 250 },
          plugins: {
            legend: { display: false },
            tooltip: { callbacks: { label: ctx => `${ctx.parsed.x} veli katıldı` } }
          },
          scales: {
            x: { beginAtZero: true, ticks: { precision: 0 }, grid: { color: grid }, border: { display: false } },
            y: { grid: { display: false }, border: { display: false } }
          }
        }
      });
    }
  }

  // ---------- Katılımcı işlemleri ----------
  $('responsesBody').addEventListener('click', async e => {
    const btn = e.target.closest('[data-delete]');
    if (!btn) return;
    if (!confirm(`"${btn.dataset.name}" adlı velinin yanıtları silinsin mi?`)) return;
    await api('/api/admin/responses/' + btn.dataset.delete, { method: 'DELETE' });
    loadResults();
  });

  $('resetBtn').addEventListener('click', async () => {
    if (!confirm('TÜM anket yanıtları silinecek. Bu işlem geri alınamaz. Emin misiniz?')) return;
    await api('/api/admin/reset', { method: 'POST' });
    loadResults();
  });

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
    init();
  });

  $('logoutBtn').addEventListener('click', async () => {
    await fetch('/api/admin/logout', { method: 'POST' });
    showLogin();
  });

  init();
})();
