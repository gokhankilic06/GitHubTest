// Uçtan uca API testleri. SQLite ile her zaman çalışır;
// TEST_DATABASE_URL tanımlıysa aynı testler PostgreSQL üzerinde de çalışır.
//   npm test
//   TEST_DATABASE_URL=postgres://... npm test

const { test, describe, before, after } = require('node:test');
const assert = require('node:assert');
const fs = require('fs');
const os = require('os');
const path = require('path');
const ExcelJS = require('exceljs');
const { createDb } = require('../src/db');
const { createApp } = require('../src/app');
const { QUESTIONS } = require('../questions');

const PASSWORD = 'test-sifre';
const ALL_A = QUESTIONS.map(() => 'A');
const ALL_B = QUESTIONS.map(() => 'B');

const backends = [['SQLite', () => ({ SQLITE_FILE: path.join(fs.mkdtempSync(path.join(os.tmpdir(), 'anket-')), 'test.db') })]];
if (process.env.TEST_DATABASE_URL) backends.push(['PostgreSQL', () => ({ DATABASE_URL: process.env.TEST_DATABASE_URL })]);

for (const [label, envFn] of backends) {
  describe(`API (${label})`, () => {
    let db, server, base, cookie;

    const req = async (method, url, body, withCookie = true) => {
      const res = await fetch(base + url, {
        method,
        headers: { ...(body ? { 'Content-Type': 'application/json' } : {}), ...(withCookie && cookie ? { Cookie: cookie } : {}) },
        body: body ? JSON.stringify(body) : undefined
      });
      const type = res.headers.get('content-type') || '';
      return { status: res.status, headers: res.headers, body: type.includes('json') ? await res.json() : Buffer.from(await res.arrayBuffer()) };
    };

    before(async () => {
      db = await createDb(envFn());
      await db.reset();
      const app = createApp({ db, adminPassword: PASSWORD, sessionSecret: 'secret' });
      server = app.listen(0);
      base = `http://127.0.0.1:${server.address().port}`;
    });

    after(async () => {
      server.close();
      await db.close();
    });

    test('admin uçları oturumsuz erişime kapalı', async () => {
      assert.strictEqual((await req('GET', '/api/admin/data', null, false)).status, 401);
      assert.strictEqual((await req('POST', '/api/admin/reset', null, false)).status, 401);
    });

    test('hatalı şifre reddedilir, doğru şifre oturum açar', async () => {
      assert.strictEqual((await req('POST', '/api/admin/login', { password: 'yanlis' })).status, 401);
      const ok = await req('POST', '/api/admin/login', { password: PASSWORD });
      assert.strictEqual(ok.status, 200);
      cookie = ok.headers.get('set-cookie').split(';')[0];
      assert.strictEqual((await req('GET', '/api/admin/data')).status, 200);
    });

    test('anket gönderimi, doğrulama, istatistik ve sınıf filtresi', async () => {
      // Eksik veya hatalı bilgi reddedilir
      assert.strictEqual((await req('POST', '/api/responses', { name: 'Ali', className: '1-A', answers: ['A'] })).status, 400);
      assert.strictEqual((await req('POST', '/api/responses', { name: '', className: '1-A', answers: ALL_A })).status, 400);
      assert.strictEqual((await req('POST', '/api/responses', { name: 'Ali', className: 'Yok', answers: ALL_A })).status, 400);

      assert.strictEqual((await req('POST', '/api/responses', { name: 'Ali Veli', className: '1-A', answers: ALL_A })).status, 200);
      assert.strictEqual((await req('POST', '/api/responses', { name: 'Ayşe', className: '1-A', answers: ALL_B })).status, 200);
      assert.strictEqual((await req('POST', '/api/responses', { name: 'Mehmet', className: '2-B', answers: ALL_B })).status, 200);

      // Tüm sınıflar
      const all = await req('GET', '/api/admin/data');
      assert.strictEqual(all.body.responses.length, 3);
      assert.deepStrictEqual(all.body.stats[0], { countA: 1, countB: 2, percentA: 33, percentB: 67 });
      assert.deepStrictEqual(all.body.classSummary, [{ className: '1-A', participants: 2 }, { className: '2-B', participants: 1 }]);

      // Sınıf filtresi
      const f = await req('GET', '/api/admin/data?class=1-A');
      assert.strictEqual(f.body.responses.length, 2);
      assert.ok(f.body.responses.every(r => r.className === '1-A'));
      assert.deepStrictEqual(f.body.stats[0], { countA: 1, countB: 1, percentA: 50, percentB: 50 });

      // Tek yanıt silme
      const id = all.body.responses.find(r => r.name === 'Ali Veli').id;
      assert.strictEqual((await req('DELETE', '/api/admin/responses/' + id)).status, 200);
      assert.strictEqual((await req('DELETE', '/api/admin/responses/' + id)).status, 404);
      assert.strictEqual((await req('GET', '/api/admin/data')).body.responses.length, 2);
    });

    test('Excel çıktısı beş sayfa içerir ve filtreye uyar', async () => {
      const res = await req('GET', '/api/admin/export.xlsx?class=1-A');
      assert.strictEqual(res.status, 200);
      const wb = new ExcelJS.Workbook();
      await wb.xlsx.load(res.body);
      assert.deepStrictEqual(wb.worksheets.map(w => w.name), ['Katılımcılar', 'Soru Bazlı Dağılım', 'Bireysel Yanıtlar', 'Şık Bazlı Sonuçlar', 'Sınıf Özeti']);
      const participants = wb.getWorksheet('Katılımcılar');
      assert.strictEqual(participants.rowCount, 2); // başlık + 1-A sınıfındaki tek yanıt
      assert.strictEqual(participants.getRow(2).getCell(3).value, '1-A');
    });

    test('verileri sıfırlama', async () => {
      await req('POST', '/api/admin/reset');
      assert.strictEqual((await req('GET', '/api/admin/data')).body.responses.length, 0);
    });

    test('sahte oturum çerezi reddedilir', async () => {
      const saved = cookie;
      cookie = 'session=9999999999999.sahte';
      assert.strictEqual((await req('GET', '/api/admin/data')).status, 401);
      cookie = saved;
    });
  });
}
