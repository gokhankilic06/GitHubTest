// SQLite adaptörü (tek sunucuda çalışan kurulumlar için).
// Veritabanı dosyası: SQLITE_FILE (varsayılan data/anket.db)
// Node.js'in kendi SQLite modülü (node:sqlite) kullanılır; derleme gerektiren ek paket yoktur.

const fs = require('fs');
const path = require('path');
const { DatabaseSync } = require('node:sqlite');

const SCHEMA = `
CREATE TABLE IF NOT EXISTS responses (
  id         INTEGER PRIMARY KEY AUTOINCREMENT,
  name       TEXT NOT NULL,
  class_name TEXT NOT NULL,
  answers    TEXT NOT NULL,
  created_at TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_responses_class ON responses(class_name);
`;

function createSqliteDb(file) {
  fs.mkdirSync(path.dirname(file), { recursive: true });
  const db = new DatabaseSync(file);
  db.exec('PRAGMA journal_mode = WAL; PRAGMA busy_timeout = 5000;');
  db.exec(SCHEMA);

  const toResponse = r => ({ id: r.id, name: r.name, className: r.class_name, answers: r.answers.split(''), createdAt: r.created_at });

  return {
    kind: 'sqlite',

    async submitResponse({ name, className, answers }) {
      const info = db.prepare('INSERT INTO responses (name, class_name, answers, created_at) VALUES (?, ?, ?, ?)')
        .run(name, className, answers.join(''), new Date().toISOString());
      return { id: Number(info.lastInsertRowid) };
    },

    async listResponses(className) {
      const sql = `SELECT * FROM responses ${className ? 'WHERE class_name = ?' : ''} ORDER BY id`;
      return db.prepare(sql).all(...(className ? [className] : [])).map(toResponse);
    },

    async deleteResponse(id) {
      return db.prepare('DELETE FROM responses WHERE id = ?').run(id).changes > 0;
    },

    async reset() {
      db.prepare('DELETE FROM responses').run();
    },

    async close() {
      db.close();
    }
  };
}

module.exports = { createSqliteDb };
