// PostgreSQL adaptörü (Supabase veya herhangi bir Postgres sunucusu).
// DATABASE_URL ortam değişkeni ile etkinleşir.

const { Pool } = require('pg');

const SCHEMA = `
CREATE TABLE IF NOT EXISTS responses (
  id         SERIAL PRIMARY KEY,
  name       TEXT NOT NULL,
  class_name TEXT NOT NULL,
  answers    TEXT NOT NULL,
  created_at TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_responses_class ON responses(class_name);
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

  const toResponse = r => ({ id: r.id, name: r.name, className: r.class_name, answers: r.answers.split(''), createdAt: r.created_at });

  return {
    kind: 'postgres',

    async submitResponse({ name, className, answers }) {
      const { rows } = await pool.query(
        'INSERT INTO responses (name, class_name, answers, created_at) VALUES ($1, $2, $3, $4) RETURNING id',
        [name, className, answers.join(''), new Date().toISOString()]
      );
      return { id: rows[0].id };
    },

    async listResponses(className) {
      const { rows } = await pool.query(`SELECT * FROM responses ${className ? 'WHERE class_name = $1' : ''} ORDER BY id`, className ? [className] : []);
      return rows.map(toResponse);
    },

    async deleteResponse(id) {
      const res = await pool.query('DELETE FROM responses WHERE id = $1', [id]);
      return res.rowCount > 0;
    },

    async reset() {
      await pool.query('DELETE FROM responses');
    },

    async close() {
      await pool.end();
    }
  };
}

module.exports = { createPostgresDb };
