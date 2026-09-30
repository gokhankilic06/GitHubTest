// SQLite adaptörü (tek sunucuda çalışan kurulumlar için).
// Veritabanı dosyası: SQLITE_FILE (varsayılan data/anket.db)
// Node.js'in kendi SQLite modülü (node:sqlite) kullanılır; derleme gerektiren ek paket yoktur.

const fs = require('fs');
const path = require('path');
const { DatabaseSync } = require('node:sqlite');

const SCHEMA = `
CREATE TABLE IF NOT EXISTS codes (
  code       TEXT PRIMARY KEY,
  class_name TEXT NOT NULL,
  created_at TEXT NOT NULL,
  used_at    TEXT
);
CREATE TABLE IF NOT EXISTS responses (
  id         INTEGER PRIMARY KEY AUTOINCREMENT,
  code       TEXT UNIQUE,
  name       TEXT NOT NULL,
  class_name TEXT NOT NULL,
  answers    TEXT NOT NULL,
  created_at TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_responses_class ON responses(class_name);
CREATE INDEX IF NOT EXISTS idx_codes_class ON codes(class_name);
`;

function createSqliteDb(file) {
  fs.mkdirSync(path.dirname(file), { recursive: true });
  const db = new DatabaseSync(file);
  db.exec('PRAGMA journal_mode = WAL; PRAGMA busy_timeout = 5000;');
  db.exec(SCHEMA);

  // fn'i tek bir işlem (transaction) içinde çalıştırır
  const transaction = fn => (...args) => {
    db.exec('BEGIN IMMEDIATE');
    try {
      const out = fn(...args);
      db.exec('COMMIT');
      return out;
    } catch (err) {
      db.exec('ROLLBACK');
      throw err;
    }
  };

  const toResponse = r => ({ id: r.id, code: r.code, name: r.name, className: r.class_name, answers: r.answers.split(''), createdAt: r.created_at });
  const toCode = c => ({ code: c.code, className: c.class_name, createdAt: c.created_at, usedAt: c.used_at, usedBy: c.used_by || null });

  const submit = transaction(({ code, name, answers, now }) => {
    const row = db.prepare('SELECT class_name FROM codes WHERE code = ? AND used_at IS NULL').get(code);
    if (!row) return null;
    db.prepare('UPDATE codes SET used_at = ? WHERE code = ?').run(now, code);
    const info = db.prepare('INSERT INTO responses (code, name, class_name, answers, created_at) VALUES (?, ?, ?, ?, ?)')
      .run(code, name, row.class_name, answers.join(''), now);
    return { id: Number(info.lastInsertRowid), className: row.class_name };
  });

  const deleteResponse = transaction(id => {
    const row = db.prepare('SELECT code FROM responses WHERE id = ?').get(id);
    if (!row) return false;
    db.prepare('DELETE FROM responses WHERE id = ?').run(id);
    if (row.code) db.prepare('UPDATE codes SET used_at = NULL WHERE code = ?').run(row.code);
    return true;
  });

  const reset = transaction(() => {
    db.prepare('DELETE FROM responses').run();
    db.prepare('UPDATE codes SET used_at = NULL').run();
  });

  return {
    kind: 'sqlite',

    async insertCodes(rows) {
      const stmt = db.prepare('INSERT OR IGNORE INTO codes (code, class_name, created_at) VALUES (?, ?, ?)');
      const inserted = [];
      transaction(() => {
        for (const r of rows) if (stmt.run(r.code, r.className, r.createdAt).changes) inserted.push(r.code);
      })();
      return inserted;
    },

    async getCode(code) {
      const c = db.prepare('SELECT * FROM codes WHERE code = ?').get(code);
      return c ? toCode(c) : null;
    },

    async listCodes(className) {
      const sql = `SELECT c.*, r.name AS used_by FROM codes c LEFT JOIN responses r ON r.code = c.code
                   ${className ? 'WHERE c.class_name = ?' : ''} ORDER BY c.class_name, c.created_at, c.code`;
      return db.prepare(sql).all(...(className ? [className] : [])).map(toCode);
    },

    async deleteUnusedCodes(className) {
      const sql = `DELETE FROM codes WHERE used_at IS NULL ${className ? 'AND class_name = ?' : ''}`;
      return db.prepare(sql).run(...(className ? [className] : [])).changes;
    },

    async deleteCode(code) {
      return db.prepare('DELETE FROM codes WHERE code = ? AND used_at IS NULL').run(code).changes > 0;
    },

    async submitResponse({ code, name, answers }) {
      return submit({ code, name, answers, now: new Date().toISOString() });
    },

    async listResponses(className) {
      const sql = `SELECT * FROM responses ${className ? 'WHERE class_name = ?' : ''} ORDER BY id`;
      return db.prepare(sql).all(...(className ? [className] : [])).map(toResponse);
    },

    async deleteResponse(id) {
      return deleteResponse(id);
    },

    async reset() {
      reset();
    },

    async close() {
      db.close();
    }
  };
}

module.exports = { createSqliteDb };
