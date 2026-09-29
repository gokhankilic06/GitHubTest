// Riba Veli Anket Sistemi - bağımlılıksız Node.js sunucusu
// Çalıştırma: node server.js   (varsayılan port 3000)
// Admin şifresi: ADMIN_PASSWORD ortam değişkeni (varsayılan: admin123)

const http = require('http');
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { SCHOOL_NAME, SURVEY_NAME, QUESTION_TEXT, CLASSES, QUESTIONS } = require('./questions');

const PORT = process.env.PORT || 3000;
const ADMIN_PASSWORD = process.env.ADMIN_PASSWORD || 'admin123';
const DB_FILE = path.join(__dirname, 'data', 'db.json');
const PUBLIC_DIR = path.join(__dirname, 'public');

// ---------- Veri katmanı ----------
// db = { nextId, responses: [{ id, name, className, answers: ['A','B',...], createdAt }] }
function loadDb() {
  try {
    return JSON.parse(fs.readFileSync(DB_FILE, 'utf8'));
  } catch {
    const fresh = { nextId: 1, responses: [] };
    saveDb(fresh);
    return fresh;
  }
}

function saveDb(data) {
  fs.mkdirSync(path.dirname(DB_FILE), { recursive: true });
  const tmp = DB_FILE + '.tmp';
  fs.writeFileSync(tmp, JSON.stringify(data, null, 2));
  fs.renameSync(tmp, DB_FILE);
}

let db = loadDb();

// ---------- Oturum ----------
const sessions = new Set();

function parseCookies(req) {
  const out = {};
  (req.headers.cookie || '').split(';').forEach(part => {
    const i = part.indexOf('=');
    if (i > -1) out[part.slice(0, i).trim()] = decodeURIComponent(part.slice(i + 1).trim());
  });
  return out;
}

function isAdmin(req) {
  return sessions.has(parseCookies(req).session);
}

function safeEqual(a, b) {
  const ha = crypto.createHash('sha256').update(String(a)).digest();
  const hb = crypto.createHash('sha256').update(String(b)).digest();
  return crypto.timingSafeEqual(ha, hb);
}

// ---------- Yardımcılar ----------
function send(res, status, body, headers = {}) {
  const isObj = typeof body === 'object';
  res.writeHead(status, {
    'Content-Type': isObj ? 'application/json; charset=utf-8' : 'text/plain; charset=utf-8',
    ...headers
  });
  res.end(isObj ? JSON.stringify(body) : body);
}

function readBody(req) {
  return new Promise((resolve, reject) => {
    let data = '';
    req.on('data', chunk => {
      data += chunk;
      if (data.length > 1e5) { req.destroy(); reject(new Error('too large')); }
    });
    req.on('end', () => {
      try { resolve(data ? JSON.parse(data) : {}); } catch { reject(new Error('bad json')); }
    });
  });
}

function survey() {
  return {
    schoolName: SCHOOL_NAME,
    surveyName: SURVEY_NAME,
    questionText: QUESTION_TEXT,
    classes: CLASSES,
    questions: QUESTIONS
  };
}

function stats() {
  const total = db.responses.length;
  const pct = n => (total ? Math.round((n / total) * 100) : 0);
  return QUESTIONS.map((q, i) => {
    const countA = db.responses.filter(r => r.answers[i] === 'A').length;
    const countB = db.responses.filter(r => r.answers[i] === 'B').length;
    return { countA, countB, percentA: pct(countA), percentB: pct(countB) };
  });
}

function csvEscape(v) {
  const s = String(v);
  return /[",;\n]/.test(s) ? '"' + s.replace(/"/g, '""') + '"' : s;
}

const MIME = {
  '.html': 'text/html; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.js': 'application/javascript; charset=utf-8'
};

function serveStatic(res, file) {
  const full = path.join(PUBLIC_DIR, file);
  if (!full.startsWith(PUBLIC_DIR + path.sep)) return send(res, 403, 'Yasak');
  fs.readFile(full, (err, data) => {
    if (err) return send(res, 404, 'Bulunamadı');
    res.writeHead(200, { 'Content-Type': MIME[path.extname(full)] || 'application/octet-stream' });
    res.end(data);
  });
}

// ---------- Rotalar ----------
async function handle(req, res) {
  const p = new URL(req.url, 'http://localhost').pathname;
  const m = req.method;

  // Sayfalar
  if (m === 'GET' && p === '/') return serveStatic(res, 'index.html');
  if (m === 'GET' && (p === '/admin' || p === '/admin/')) return serveStatic(res, 'admin.html');

  // Veli tarafı
  if (m === 'GET' && p === '/api/survey') return send(res, 200, survey());

  if (m === 'POST' && p === '/api/responses') {
    const body = await readBody(req);
    const name = typeof body.name === 'string' ? body.name.trim().slice(0, 100) : '';
    const className = body.className;
    const answers = Array.isArray(body.answers) ? body.answers : [];
    if (!name) return send(res, 400, { error: 'Lütfen adınızı ve soyadınızı giriniz.' });
    if (!CLASSES.includes(className)) return send(res, 400, { error: 'Lütfen sınıf seçiniz.' });
    if (answers.length !== QUESTIONS.length || !answers.every(a => a === 'A' || a === 'B')) {
      return send(res, 400, { error: 'Lütfen tüm soruları yanıtlayınız.' });
    }
    db.responses.push({ id: db.nextId++, name, className, answers, createdAt: new Date().toISOString() });
    saveDb(db);
    return send(res, 200, { ok: true });
  }

  // Admin giriş/çıkış
  if (m === 'POST' && p === '/api/admin/login') {
    const body = await readBody(req);
    if (!safeEqual(body.password || '', ADMIN_PASSWORD)) return send(res, 401, { error: 'Şifre hatalı.' });
    const token = crypto.randomBytes(24).toString('hex');
    sessions.add(token);
    return send(res, 200, { ok: true }, { 'Set-Cookie': `session=${token}; HttpOnly; SameSite=Strict; Path=/` });
  }
  if (m === 'POST' && p === '/api/admin/logout') {
    sessions.delete(parseCookies(req).session);
    return send(res, 200, { ok: true }, { 'Set-Cookie': 'session=; Max-Age=0; Path=/' });
  }

  // Bundan sonrası admin yetkisi ister
  if (p.startsWith('/api/admin/')) {
    if (!isAdmin(req)) return send(res, 401, { error: 'Yetkisiz' });

    if (m === 'GET' && p === '/api/admin/data') {
      return send(res, 200, { ...survey(), responses: db.responses, stats: stats() });
    }

    const del = p.match(/^\/api\/admin\/responses\/(\d+)$/);
    if (m === 'DELETE' && del) {
      const id = Number(del[1]);
      const before = db.responses.length;
      db.responses = db.responses.filter(r => r.id !== id);
      if (db.responses.length === before) return send(res, 404, { error: 'Kayıt bulunamadı.' });
      saveDb(db);
      return send(res, 200, { ok: true });
    }

    if (m === 'POST' && p === '/api/admin/reset') {
      db = { nextId: 1, responses: [] };
      saveDb(db);
      return send(res, 200, { ok: true });
    }

    if (m === 'GET' && p === '/api/admin/export.csv') {
      const header = ['Sıra No', 'Ad Soyad', 'Sınıf', ...QUESTIONS.map((_, i) => `M${i + 1}`), 'Tarih'];
      const rows = db.responses.map((r, i) => [i + 1, r.name, r.className, ...r.answers, r.createdAt]);
      const csv = '﻿' + [header, ...rows].map(r => r.map(csvEscape).join(';')).join('\r\n');
      return send(res, 200, csv, {
        'Content-Type': 'text/csv; charset=utf-8',
        'Content-Disposition': 'attachment; filename="veli-anket-sonuclari.csv"'
      });
    }
  }

  // Statik dosyalar (css/js)
  if (m === 'GET' && /^\/(vendor\/)?[\w.-]+\.(css|js)$/.test(p)) return serveStatic(res, p.slice(1));

  send(res, 404, 'Bulunamadı');
}

http.createServer((req, res) => {
  handle(req, res).catch(() => send(res, 400, { error: 'Geçersiz istek' }));
}).listen(PORT, () => {
  console.log(`Veli anketi:     http://localhost:${PORT}`);
  console.log(`Yönetici paneli: http://localhost:${PORT}/admin`);
});
