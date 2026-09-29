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
      await db.deleteUnusedCodes();
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
      assert.strictEqual((await req('POST', '/api/admin/codes', { className: '1-A', count: 1 }, false)).status, 401);
    });

    test('hatalı şifre reddedilir, doğru şifre oturum açar', async () => {
      assert.strictEqual((await req('POST', '/api/admin/login', { password: 'yanlis' })).status, 401);
      const ok = await req('POST', '/api/admin/login', { password: PASSWORD });
      assert.strictEqual(ok.status, 200);
      cookie = ok.headers.get('set-cookie').split(';')[0];
      assert.strictEqual((await req('GET', '/api/admin/data')).status, 200);
    });

    test('kod üretimi doğrulanır', async () => {
      assert.strictEqual((await req('POST', '/api/admin/codes', { className: 'Yok', count: 5 })).status, 400);
      assert.strictEqual((await req('POST', '/api/admin/codes', { className: '1-A', count: 0 })).status, 400);
      assert.strictEqual((await req('POST', '/api/admin/codes', { className: '1-A', count: 999 })).status, 400);
    });

    test('tek kullanımlık kod ile anket akışı', async () => {
      const a = await req('POST', '/api/admin/codes', { className: '1-A', count: 3 });
      const b = await req('POST', '/api/admin/codes', { className: '2-B', count: 2 });
      assert.strictEqual(a.body.codes.length, 3);
      assert.strictEqual(new Set(a.body.codes).size, 3);
      const [c1, c2] = a.body.codes;
      const [c3] = b.body.codes;

      // Kod kontrolü sınıfı döndürür, küçük harf ve boşluklar tolere edilir
      const check = await req('GET', '/api/codes/' + encodeURIComponent(' ' + c1.toLowerCase()));
      assert.deepStrictEqual(check.body, { className: '1-A' });
      assert.strictEqual((await req('GET', '/api/codes/XXXXXX')).status, 404);

      // Eksik yanıt reddedilir
      assert.strictEqual((await req('POST', '/api/responses', { code: c1, name: 'Ali', answers: ['A'] })).status, 400);
      assert.strictEqual((await req('POST', '/api/responses', { code: c1, name: '', answers: ALL_A })).status, 400);
      assert.strictEqual((await req('POST', '/api/responses', { code: 'YOKKOD', name: 'Ali', answers: ALL_A })).status, 404);

      // Geçerli gönderim, ardından aynı kodla ikinci gönderim reddedilir
      assert.strictEqual((await req('POST', '/api/responses', { code: c1, name: 'Ali Veli', answers: ALL_A })).status, 200);
      assert.strictEqual((await req('POST', '/api/responses', { code: c1, name: 'Başka', answers: ALL_B })).status, 409);
      assert.strictEqual((await req('GET', '/api/codes/' + c1)).status, 409);

      assert.strictEqual((await req('POST', '/api/responses', { code: c2, name: 'Ayşe', answers: ALL_B })).status, 200);
      assert.strictEqual((await req('POST', '/api/responses', { code: c3, name: 'Mehmet', answers: ALL_B })).status, 200);

      // Tüm sınıflar
      const all = await req('GET', '/api/admin/data');
      assert.strictEqual(all.body.responses.length, 3);
      assert.deepStrictEqual(all.body.stats[0], { countA: 1, countB: 2, percentA: 33, percentB: 67 });
      const s1a = all.body.classSummary.find(s => s.className === '1-A');
      assert.deepStrictEqual(s1a, { className: '1-A', participants: 2, codeCount: 3, rate: 67 });

      // Sınıf filtresi
      const f = await req('GET', '/api/admin/data?class=1-A');
      assert.strictEqual(f.body.responses.length, 2);
      assert.ok(f.body.responses.every(r => r.className === '1-A'));
      assert.deepStrictEqual(f.body.stats[0], { countA: 1, countB: 1, percentA: 50, percentB: 50 });

      // Kod listesi kullanan veliyi gösterir
      const codes = await req('GET', '/api/admin/codes?class=1-A');
      assert.strictEqual(codes.body.codes.find(c => c.code === c1).usedBy, 'Ali Veli');

      // Kullanılmış kod silinemez
      assert.strictEqual((await req('DELETE', '/api/admin/codes/' + c1)).status, 400);

      // Yanıt silinince kod yeniden kullanılabilir
      const id = all.body.responses.find(r => r.code === c1).id;
      assert.strictEqual((await req('DELETE', '/api/admin/responses/' + id)).status, 200);
      assert.strictEqual((await req('GET', '/api/codes/' + c1)).status, 200);
    });

    test('aynı koda eşzamanlı iki gönderimden yalnızca biri kabul edilir', async () => {
      const { body } = await req('POST', '/api/admin/codes', { className: '3-C', count: 1 });
      const results = await Promise.all([1, 2, 3, 4].map(i => req('POST', '/api/responses', { code: body.codes[0], name: 'Veli ' + i, answers: ALL_A })));
      assert.strictEqual(results.filter(r => r.status === 200).length, 1);
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

      const codesX = await req('GET', '/api/admin/codes.xlsx');
      const wb2 = new ExcelJS.Workbook();
      await wb2.xlsx.load(codesX.body);
      assert.ok(wb2.getWorksheet('Veli Kodları').rowCount > 1);
    });

    test('kullanılmamış kodları silme ve sıfırlama', async () => {
      const del = await req('POST', '/api/admin/codes/delete-unused?class=2-B');
      assert.strictEqual(del.body.deleted, 1);
      await req('POST', '/api/admin/reset');
      const d = await req('GET', '/api/admin/data');
      assert.strictEqual(d.body.responses.length, 0);
      const codes = await req('GET', '/api/admin/codes');
      assert.ok(codes.body.codes.every(c => !c.usedAt));
    });

    test('sahte oturum çerezi reddedilir', async () => {
      const saved = cookie;
      cookie = 'session=9999999999999.sahte';
      assert.strictEqual((await req('GET', '/api/admin/data')).status, 401);
      cookie = saved;
    });
  });
}
