// Yönetici paneli grafikleri (Chart.js)
(function () {
  if (typeof Chart === 'undefined') return;
  const d = JSON.parse(document.getElementById('grafikVerisi').textContent);
  const css = name => getComputedStyle(document.documentElement).getPropertyValue(name).trim();
  const colorA = css('--series-a');
  const colorB = css('--series-b');
  const grid = css('--chart-grid');
  Chart.defaults.color = css('--chart-ink');
  Chart.defaults.font.family = getComputedStyle(document.body).fontFamily;
  const bar = { borderColor: css('--chart-surface'), borderWidth: 2, borderSkipped: false, borderRadius: 4, barPercentage: 0.8, maxBarThickness: 28 };

  new Chart(document.getElementById('questionChart'), {
    type: 'bar',
    data: {
      labels: d.sorular,
      datasets: [
        { label: 'A', data: d.yuzdeA, counts: d.a, texts: d.kisaA, backgroundColor: colorA, ...bar },
        { label: 'B', data: d.yuzdeB, counts: d.b, texts: d.kisaB, backgroundColor: colorB, ...bar }
      ]
    },
    options: {
      indexAxis: 'y',
      maintainAspectRatio: false,
      animation: { duration: 250 },
      plugins: {
        legend: { display: false },
        tooltip: {
          callbacks: {
            label: ctx => `${ctx.dataset.label}) ${ctx.dataset.texts[ctx.dataIndex]}: ${ctx.dataset.counts[ctx.dataIndex]} veli (${ctx.parsed.x}%)`
          }
        }
      },
      scales: {
        x: { stacked: true, min: 0, max: 100, ticks: { callback: v => v + '%' }, grid: { color: grid }, border: { display: false } },
        y: { stacked: true, grid: { display: false }, border: { display: false } }
      }
    }
  });

  const classCanvas = document.getElementById('classChart');
  if (classCanvas) {
    new Chart(classCanvas, {
      type: 'bar',
      data: { labels: d.siniflar, datasets: [{ label: 'Katılan veli', data: d.katilan, backgroundColor: colorA, ...bar }] },
      options: {
        indexAxis: 'y',
        maintainAspectRatio: false,
        animation: { duration: 250 },
        plugins: { legend: { display: false }, tooltip: { callbacks: { label: ctx => `${ctx.parsed.x} veli katıldı` } } },
        scales: {
          x: { beginAtZero: true, ticks: { precision: 0 }, grid: { color: grid }, border: { display: false } },
          y: { grid: { display: false }, border: { display: false } }
        }
      }
    });
  }
})();
