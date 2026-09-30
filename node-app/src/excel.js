// Excel (.xlsx) raporları.

const ExcelJS = require('exceljs');
const { questionStats, classSummary } = require('./stats');

const HEADER_FILL = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFE9ECEF' } };
const BORDER = { top: { style: 'thin' }, left: { style: 'thin' }, bottom: { style: 'thin' }, right: { style: 'thin' } };

function styleSheet(ws, headerRows = 1) {
  ws.eachRow(row => row.eachCell(cell => { cell.border = BORDER; }));
  for (let i = 1; i <= headerRows; i++) {
    ws.getRow(i).eachCell(cell => {
      cell.font = { bold: true };
      cell.fill = HEADER_FILL;
      cell.alignment = { vertical: 'middle', horizontal: 'center', wrapText: true };
    });
  }
  ws.views = [{ state: 'frozen', ySplit: headerRows }];
}

async function resultsWorkbook({ survey, responses, className }) {
  const wb = new ExcelJS.Workbook();
  wb.creator = survey.surveyName;
  const qs = survey.questions;
  const stats = questionStats(responses, qs.length);
  const scope = className || 'Tüm Sınıflar';

  // 1) Katılımcılar
  const ws1 = wb.addWorksheet('Katılımcılar');
  ws1.columns = [
    { header: 'Sıra No', width: 9 },
    { header: 'Ad Soyad', width: 28 },
    { header: 'Sınıf', width: 12 },
    { header: 'Tarih', width: 20 },
    { header: 'Yanıtlar', width: 40 }
  ];
  responses.forEach((r, i) => ws1.addRow([i + 1, r.name, r.className, new Date(r.createdAt), r.answers.join(', ')]));
  ws1.getColumn(4).numFmt = 'dd.mm.yyyy hh:mm';
  styleSheet(ws1);

  // 2) Soru bazlı dağılım
  const ws2 = wb.addWorksheet('Soru Bazlı Dağılım');
  ws2.columns = [
    { header: 'Soru', width: 8 },
    { header: 'A Seçeneği', width: 50 },
    { header: 'A Sayısı', width: 10 },
    { header: 'A %', width: 8 },
    { header: 'B Seçeneği', width: 50 },
    { header: 'B Sayısı', width: 10 },
    { header: 'B %', width: 8 }
  ];
  qs.forEach((q, i) => {
    const s = stats[i];
    ws2.addRow([`${i + 1}. Soru`, q.a, s.countA, s.percentA / 100, q.b, s.countB, s.percentB / 100]);
  });
  ws2.getColumn(4).numFmt = '0%';
  ws2.getColumn(7).numFmt = '0%';
  ws2.getColumn(2).alignment = { wrapText: true, vertical: 'top' };
  ws2.getColumn(5).alignment = { wrapText: true, vertical: 'top' };
  styleSheet(ws2);

  // 3) Bireysel yanıt tablosu (M1..Mn, A/B alt sütunları)
  const ws3 = wb.addWorksheet('Bireysel Yanıtlar');
  const top = ['No', 'Adı Soyadı', 'Sınıf'];
  const sub = ['', '', ''];
  qs.forEach((_, i) => { top.push(`M${i + 1}`, ''); sub.push('A', 'B'); });
  ws3.addRow(top);
  ws3.addRow(sub);
  ws3.mergeCells(1, 1, 2, 1);
  ws3.mergeCells(1, 2, 2, 2);
  ws3.mergeCells(1, 3, 2, 3);
  qs.forEach((_, i) => ws3.mergeCells(1, 4 + i * 2, 1, 5 + i * 2));
  responses.forEach((r, idx) => {
    const row = [idx + 1, r.name, r.className];
    r.answers.forEach(a => row.push(a === 'A' ? 1 : null, a === 'B' ? 1 : null));
    ws3.addRow(row);
  });
  const totals = ['', 'TOPLAM', ''];
  stats.forEach(s => totals.push(s.countA, s.countB));
  const totalRow = ws3.addRow(totals);
  totalRow.font = { bold: true };
  ws3.getColumn(1).width = 6;
  ws3.getColumn(2).width = 26;
  ws3.getColumn(3).width = 10;
  for (let c = 4; c < 4 + qs.length * 2; c++) {
    ws3.getColumn(c).width = 4.5;
    ws3.getColumn(c).alignment = { horizontal: 'center' };
  }
  styleSheet(ws3, 2);

  // 4) Şık bazlı sonuçlar
  const ws4 = wb.addWorksheet('Şık Bazlı Sonuçlar');
  ws4.columns = [
    { header: 'Soru', width: 8 },
    { header: 'Şık', width: 70 },
    { header: 'Seçen Sayısı', width: 14 },
    { header: 'Yüzde', width: 10 }
  ];
  qs.forEach((q, i) => {
    ws4.addRow([`${i + 1}A`, q.shortA, stats[i].countA, stats[i].percentA / 100]);
    ws4.addRow([`${i + 1}B`, q.shortB, stats[i].countB, stats[i].percentB / 100]);
  });
  ws4.getColumn(4).numFmt = '0%';
  styleSheet(ws4);

  // 5) Sınıf özeti
  const ws5 = wb.addWorksheet('Sınıf Özeti');
  ws5.columns = [
    { header: 'Sınıf', width: 14 },
    { header: 'Katılan Veli', width: 14 }
  ];
  classSummary(responses, survey.classes).forEach(s => ws5.addRow([s.className, s.participants]));
  styleSheet(ws5);

  wb.eachSheet(ws => {
    ws.headerFooter.oddHeader = `&C${survey.schoolName} - ${scope}`;
  });
  return wb;
}

module.exports = { resultsWorkbook };
