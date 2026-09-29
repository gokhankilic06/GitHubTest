// Yönetici oturumu: imzalı (HMAC) çerez. Sunucu yeniden başlasa da oturum korunur.
// Şifre değişirse eski oturumlar geçersiz olur.

const crypto = require('crypto');

const SESSION_HOURS = 8;
const MAX_ATTEMPTS = 10;
const WINDOW_MS = 15 * 60 * 1000;

function createAuth({ password, secret }) {
  const key = crypto.createHash('sha256').update(`${secret || ''}|${password}`).digest();
  const attempts = new Map(); // ip -> { count, first }

  const sign = value => crypto.createHmac('sha256', key).update(value).digest('base64url');

  function safeEqual(a, b) {
    const ha = crypto.createHash('sha256').update(String(a)).digest();
    const hb = crypto.createHash('sha256').update(String(b)).digest();
    return crypto.timingSafeEqual(ha, hb);
  }

  function parseCookies(req) {
    const out = {};
    (req.headers.cookie || '').split(';').forEach(part => {
      const i = part.indexOf('=');
      if (i > -1) out[part.slice(0, i).trim()] = decodeURIComponent(part.slice(i + 1).trim());
    });
    return out;
  }

  function issueCookie(secure) {
    const expires = Date.now() + SESSION_HOURS * 3600 * 1000;
    const token = `${expires}.${sign(String(expires))}`;
    return `session=${token}; HttpOnly; SameSite=Strict; Path=/; Max-Age=${SESSION_HOURS * 3600}${secure ? '; Secure' : ''}`;
  }

  function isAdmin(req) {
    const token = parseCookies(req).session || '';
    const [expires, sig] = token.split('.');
    if (!expires || !sig || Number(expires) < Date.now()) return false;
    return safeEqual(sig, sign(expires));
  }

  // Kaba kuvvet denemelerine karşı IP başına sınır
  function tooManyAttempts(ip) {
    const a = attempts.get(ip);
    if (!a) return false;
    if (Date.now() - a.first > WINDOW_MS) {
      attempts.delete(ip);
      return false;
    }
    return a.count >= MAX_ATTEMPTS;
  }

  function recordFailure(ip) {
    const a = attempts.get(ip);
    if (!a || Date.now() - a.first > WINDOW_MS) attempts.set(ip, { count: 1, first: Date.now() });
    else a.count++;
  }

  function checkPassword(ip, candidate) {
    if (tooManyAttempts(ip)) return 'locked';
    if (safeEqual(candidate || '', password)) {
      attempts.delete(ip);
      return 'ok';
    }
    recordFailure(ip);
    return 'wrong';
  }

  return { isAdmin, issueCookie, checkPassword };
}

module.exports = { createAuth };
