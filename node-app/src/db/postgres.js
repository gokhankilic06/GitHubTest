// PostgreSQL adaptörü (Supabase veya herhangi bir Postgres sunucusu).
// DATABASE_URL ortam değişkeni ile etkinleşir.

const { Pool } = require('pg');

const SCHEMA = `
CREATE TABLE IF NOT EXISTS codes (
  code       TEXT PRIMARY KEY,
  class_name TEXT NOT NULL,
  created_at TEXT NOT NULL,
  used_at    TEXT
);
CREATE TABLE IF NOT EXISTS responses (
  id         SERIAL PRIMARY KEY,
  code       TEXT UNIQUE,
  name       TEXT NOT NULL,
  class_name TEXT NOT NULL,
  answers    TEXT NOT NULL,
  created_at TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_responses_class ON responses(class_name);
CREATE INDEX IF NOT EXISTS idx_codes_class ON codes(class_name);
`;

function isLocal(url) {
  try {
    return ['localhost', '127.0.0.1', '::1', ''].includes(new URL(url).hostname);
  } catch {
    return false;
  }
}

async function createPostgresDb(url) {
  // Supabase gibi uzak sunucular SSL ister; yerel kurulumda SSL kapalıdır.
  const pool = new Pool({
    connectionString: url,
    ssl: isLocal(url) ? false : { rejectUnauthorized: false },
    max: 5
  });
  await pool.query(SCHEMA);

  const toResponse = r => ({ id: r.id, code: r.code, name: r.name, className: r.class_name, answers: r.answers.split(''), createdAt: r.created_at });
  const toCode = c => ({ code: c.code, className: c.class_name, createdAt: c.created_at, usedAt: c.used_at, usedBy: c.used_by || null });

  async function tx(fn) {
    const client = await pool.connect();
    try {
      await client.query('BEGIN');
      const out = await fn(client);
      await client.query('COMMIT');
      return out;
    } catch (err) {
      await client.query('ROLLBACK');
      throw err;
    } finally {
      client.release();
    }
  }

  return {
    kind: 'postgres',

    async insertCodes(rows) {
      if (!rows.length) return [];
      const values = [];
      const params = rows.map((r, i) => {
        values.push(r.code, r.className, r.createdAt);
        return `($${i * 3 + 1}, $${i * 3 + 2}, $${i * 3 + 3})`;
      });
      const res = await pool.query(`INSERT INTO codes (code, class_name, created_at) VALUES ${params.join(',')} ON CONFLICT (code) DO NOTHING RETURNING code`, values);
      return res.rows.map(r => r.code);
    },

    async getCode(code) {
      const { rows } = await pool.query('SELECT * FROM codes WHERE code = $1', [code]);
      return rows[0] ? toCode(rows[0]) : null;
    },

    async listCodes(className) {
      const { rows } = await pool.query(
        `SELECT c.*, r.name AS used_by FROM codes c LEFT JOIN responses r ON r.code = c.code
         ${className ? 'WHERE c.class_name = $1' : ''} ORDER BY c.class_name, c.created_at, c.code`,
        className ? [className] : []
      );
      return rows.map(toCode);
    },

    async deleteUnusedCodes(className) {
      const res = await pool.query(`DELETE FROM codes WHERE used_at IS NULL ${className ? 'AND class_name = $1' : ''}`, className ? [className] : []);
      return res.rowCount;
    },

    async deleteCode(code) {
      const res = await pool.query('DELETE FROM codes WHERE code = $1 AND used_at IS NULL', [code]);
      return res.rowCount > 0;
    },

    async submitResponse({ code, name, answers }) {
      const now = new Date().toISOString();
      return tx(async c => {
        // Kodu kilitleyerek işaretle: aynı kodla eşzamanlı iki gönderimden yalnızca biri geçer.
        const upd = await c.query('UPDATE codes SET used_at = $1 WHERE code = $2 AND used_at IS NULL RETURNING class_name', [now, code]);
        if (!upd.rowCount) return null;
        const className = upd.rows[0].class_name;
        const ins = await c.query(
          'INSERT INTO responses (code, name, class_name, answers, created_at) VALUES ($1, $2, $3, $4, $5) RETURNING id',
          [code, name, className, answers.join(''), now]
        );
        return { id: ins.rows[0].id, className };
      });
    },

    async listResponses(className) {
      const { rows } = await pool.query(`SELECT * FROM responses ${className ? 'WHERE class_name = $1' : ''} ORDER BY id`, className ? [className] : []);
      return rows.map(toResponse);
    },

    async deleteResponse(id) {
      return tx(async c => {
        const del = await c.query('DELETE FROM responses WHERE id = $1 RETURNING code', [id]);
        if (!del.rowCount) return false;
        if (del.rows[0].code) await c.query('UPDATE codes SET used_at = NULL WHERE code = $1', [del.rows[0].code]);
        return true;
      });
    },

    async reset() {
      await tx(async c => {
        await c.query('DELETE FROM responses');
        await c.query('UPDATE codes SET used_at = NULL');
      });
    },

    async close() {
      await pool.end();
    }
  };
}

module.exports = { createPostgresDb };
