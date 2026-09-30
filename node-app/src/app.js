// Express uygulaması: veli anketi + yönetici API'si.

const path = require('path');
const express = require('express');
const config = require('../questions');
const { createAuth } = require('./auth');
const { questionStats, classSummary } = require('./stats');
const { resultsWorkbook } = require('./excel');

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

  app.post('/api/responses', async (req, res) => {
    const body = req.body || {};
    const name = typeof body.name === 'string' ? body.name.trim().replace(/\s+/g, ' ').slice(0, 100) : '';
    const className = body.className;
    const answers = Array.isArray(body.answers) ? body.answers : [];
    if (!name) return res.status(400).json({ error: 'Lütfen adınızı ve soyadınızı giriniz.' });
    if (!config.CLASSES.includes(className)) return res.status(400).json({ error: 'Lütfen sınıf seçiniz.' });
    if (answers.length !== config.QUESTIONS.length || !answers.every(a => a === 'A' || a === 'B')) {
      return res.status(400).json({ error: 'Lütfen tüm soruları yanıtlayınız.' });
    }
    await db.submitResponse({ name, className, answers });
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
    const [responses, allResponses] = await Promise.all([db.listResponses(className), db.listResponses()]);
    res.json({
      ...survey(),
      className,
      responses,
      stats: questionStats(responses, config.QUESTIONS.length),
      classSummary: classSummary(allResponses, config.CLASSES)
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
    const responses = await db.listResponses(className);
    const wb = await resultsWorkbook({ survey: survey(), responses, className });
    const suffix = className ? '-' + className : '';
    res.set({
      'Content-Type': 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
      'Content-Disposition': `attachment; filename="veli-anket-sonuclari${suffix}.xlsx"`
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
