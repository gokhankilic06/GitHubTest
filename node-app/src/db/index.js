// DATABASE_URL tanımlıysa PostgreSQL (Supabase), değilse SQLite kullanılır.

const path = require('path');

async function createDb(env = process.env) {
  if (env.DATABASE_URL) {
    const { createPostgresDb } = require('./postgres');
    return createPostgresDb(env.DATABASE_URL);
  }
  const { createSqliteDb } = require('./sqlite');
  return createSqliteDb(env.SQLITE_FILE || path.join(__dirname, '..', '..', 'data', 'anket.db'));
}

module.exports = { createDb };
