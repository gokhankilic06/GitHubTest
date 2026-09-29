// Express uygulaması: veli anketi + yönetici API'si.

const path = require('path');
const express = require('express');
const config = require('../questions');
const { createAuth } = require('./auth');
const { generateCodes, normalizeCode } = require('./codes');
const { questionStats, classSummary } = require('./stats');
const { resultsWorkbook, codesWorkbook } = require('./excel');

const MAX_CODES_PER_REQUEST = 200;

function survey() {
  return {
    schoolName: config.SCHOOL_NAME,
    surveyName: config.SURVEY_NAME,
    questionText: config.QUESTION_TEXT,
    classes: config.CLASSES,
    questions: config.QUESTIONS
  };
}

function createApp({ db, adminPassword, sessionSecret, secureCookies = false }) {
  const app = express();
  const auth = createAuth({ password: adminPassword, secret: sessionSecret });
  const publicDir = path.join(__dirname, '..', 'public');

  app.disable('x-powered-by');
  app.set('trust proxy', 1);
  app.use(express.json({ limit: '50kb' }));
  app.use((req, res, next) => {
    res.set('X-Content-Type-Options', 'nosniff');
    res.set('X-Frame-Options', 'DENY');
    res.set('Referrer-Policy', 'same-origin');
    next();
  });

  // ---------- Sayfalar ----------
  app.get('/', (req, res) => res.sendFile(path.join(publicDir, 'index.html')));
  app.get('/admin', (req, res) => res.sendFile(path.join(publicDir, 'admin.html')));
  app.use(express.static(publicDir, { index: false }));

  // ---------- Veli API ----------
  app.get('/api/survey', (req, res) => res.json(survey()));

  app.get('/api/codes/:code', async (req, res) => {
    const c = await db.getCode(normalizeCode(req.params.code));
    if (!c) return res.status(404).json({ error: 'Veli kodu bulunamadı. Lütfen kodu kontrol ediniz.' });
    if (c.usedAt) return res.status(409).json({ error: 'Bu veli kodu daha önce kullanılmış.' });
    res.json({ className: c.className });
  });

  app.post('/api/responses', async (req, res) => {
    const body = req.body || {};
    const code = normalizeCode(body.code);
    const name = typeof body.name === 'string' ? body.name.trim().replace(/\s+/g, ' ').slice(0, 100) : '';
    const answers = Array.isArray(body.answers) ? body.answers : [];
    if (!code) return res.status(400).json({ error: 'Lütfen veli kodunu giriniz.' });
    if (!name) return res.status(400).json({ error: 'Lütfen adınızı ve soyadınızı giriniz.' });
    if (answers.length !== config.QUESTIONS.length || !answers.every(a => a === 'A' || a === 'B')) {
      return res.status(400).json({ error: 'Lütfen tüm soruları yanıtlayınız.' });
    }
    const result = await db.submitResponse({ code, name, answers });
    if (!result) {
      const c = await db.getCode(code);
      return res.status(c ? 409 : 404).json({ error: c ? 'Bu veli kodu daha önce kullanılmış.' : 'Veli kodu bulunamadı.' });
    }
    res.json({ ok: true });
  });

  // ---------- Yönetici girişi ----------
  app.post('/api/admin/login', (req, res) => {
    const result = auth.checkPassword(req.ip, (req.body || {}).password);
    if (result === 'locked') return res.status(429).json({ error: 'Çok fazla hatalı deneme. Lütfen 15 dakika sonra tekrar deneyiniz.' });
    if (result !== 'ok') return res.status(401).json({ error: 'Şifre hatalı.' });
    res.set('Set-Cookie', auth.issueCookie(secureCookies || req.secure)).json({ ok: true });
  });

  app.post('/api/admin/logout', (req, res) => {
    res.set('Set-Cookie', 'session=; Max-Age=0; Path=/').json({ ok: true });
  });

  // ---------- Yönetici API (oturum gerekli) ----------
  const admin = express.Router();
  admin.use((req, res, next) => (auth.isAdmin(req) ? next() : res.status(401).json({ error: 'Yetkisiz' })));

  const classFilter = req => {
    const c = req.query.class;
    return typeof c === 'string' && config.CLASSES.includes(c) ? c : null;
  };

  admin.get('/data', async (req, res) => {
    const className = classFilter(req);
    const [responses, allResponses, codes] = await Promise.all([db.listResponses(className), db.listResponses(), db.listCodes()]);
    res.json({
      ...survey(),
      className,
      responses,
      stats: questionStats(responses, config.QUESTIONS.length),
      classSummary: classSummary(allResponses, codes, config.CLASSES)
    });
  });

  admin.delete('/responses/:id', async (req, res) => {
    const ok = await db.deleteResponse(Number(req.params.id));
    ok ? res.json({ ok: true }) : res.status(404).json({ error: 'Kayıt bulunamadı.' });
  });

  admin.post('/reset', async (req, res) => {
    await db.reset();
    res.json({ ok: true });
  });

  admin.get('/export.xlsx', async (req, res) => {
    const className = classFilter(req);
    const [responses, codes] = await Promise.all([db.listResponses(className), db.listCodes()]);
    const wb = await resultsWorkbook({ survey: survey(), responses, codes, className });
    const suffix = className ? '-' + className : '';
    res.set({
      'Content-Type': 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
      'Content-Disposition': `attachment; filename="veli-anket-sonuclari${suffix}.xlsx"`
    });
    await wb.xlsx.write(res);
    res.end();
  });

  // Veli kodları
  admin.get('/codes', async (req, res) => res.json({ codes: await db.listCodes(classFilter(req)) }));

  admin.post('/codes', async (req, res) => {
    const { className } = req.body || {};
    const count = Number((req.body || {}).count);
    if (!config.CLASSES.includes(className)) return res.status(400).json({ error: 'Geçerli bir sınıf seçiniz.' });
    if (!Number.isInteger(count) || count < 1 || count > MAX_CODES_PER_REQUEST) {
      return res.status(400).json({ error: `Kod sayısı 1 ile ${MAX_CODES_PER_REQUEST} arasında olmalıdır.` });
    }
    const codes = await generateCodes(db, className, count);
    res.json({ codes });
  });

  admin.delete('/codes/:code', async (req, res) => {
    const ok = await db.deleteCode(normalizeCode(req.params.code));
    ok ? res.json({ ok: true }) : res.status(400).json({ error: 'Kod bulunamadı veya kullanılmış (kullanılmış kodlar silinemez).' });
  });

  admin.post('/codes/delete-unused', async (req, res) => {
    const deleted = await db.deleteUnusedCodes(classFilter(req));
    res.json({ deleted });
  });

  admin.get('/codes.xlsx', async (req, res) => {
    const className = classFilter(req);
    const wb = await codesWorkbook({ survey: survey(), codes: await db.listCodes(className) });
    const suffix = className ? '-' + className : '';
    res.set({
      'Content-Type': 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
      'Content-Disposition': `attachment; filename="veli-kodlari${suffix}.xlsx"`
    });
    await wb.xlsx.write(res);
    res.end();
  });

  app.use('/api/admin', admin);

  app.use((req, res) => res.status(404).json({ error: 'Bulunamadı' }));
  // eslint-disable-next-line no-unused-vars
  app.use((err, req, res, next) => {
    if (err.type === 'entity.parse.failed') return res.status(400).json({ error: 'Geçersiz istek' });
    console.error(err);
    res.status(500).json({ error: 'Sunucu hatası' });
  });

  return app;
}

module.exports = { createApp };
