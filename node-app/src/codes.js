// Tek kullanımlık veli kodu üretimi.
// Karışabilecek karakterler (0/O, 1/I/L) kullanılmaz.

const crypto = require('crypto');

const ALPHABET = 'ABCDEFGHJKMNPQRSTUVWXYZ23456789';
const CODE_LENGTH = 6;

function randomCode() {
  const bytes = crypto.randomBytes(CODE_LENGTH);
  let s = '';
  for (let i = 0; i < CODE_LENGTH; i++) s += ALPHABET[bytes[i] % ALPHABET.length];
  return s;
}

function normalizeCode(v) {
  return typeof v === 'string' ? v.toUpperCase().replace(/[^A-Z0-9]/g, '').slice(0, 20) : '';
}

// İstenen sayıda benzersiz kod üretip veritabanına ekler.
// Nadiren var olan bir kodla çakışma olursa eksik kalanlar yeniden üretilir.
async function generateCodes(db, className, count) {
  const created = [];
  for (let attempt = 0; created.length < count && attempt < 10; attempt++) {
    const batch = new Set();
    while (batch.size < count - created.length) batch.add(randomCode());
    const createdAt = new Date().toISOString();
    created.push(...await db.insertCodes([...batch].map(code => ({ code, className, createdAt }))));
  }
  return created;
}

module.exports = { generateCodes, normalizeCode, randomCode };
