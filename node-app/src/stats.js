// Yanıt listesinden soru bazlı sayı/yüzde ve sınıf özetini hesaplar.

function pct(n, total) {
  return total ? Math.round((n / total) * 100) : 0;
}

function questionStats(responses, questionCount) {
  const total = responses.length;
  const out = [];
  for (let i = 0; i < questionCount; i++) {
    let countA = 0;
    let countB = 0;
    for (const r of responses) {
      if (r.answers[i] === 'A') countA++;
      else if (r.answers[i] === 'B') countB++;
    }
    out.push({ countA, countB, percentA: pct(countA, total), percentB: pct(countB, total) });
  }
  return out;
}

function classSummary(responses, classes) {
  return classes
    .map(className => ({ className, participants: responses.filter(r => r.className === className).length }))
    .filter(s => s.participants);
}

module.exports = { pct, questionStats, classSummary };
