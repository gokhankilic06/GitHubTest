const { DatabaseSync } = require('node:sqlite');
const path = require('node:path');
const fs = require('node:fs');

const dataDir = path.join(__dirname, 'data');
if (!fs.existsSync(dataDir)) fs.mkdirSync(dataDir, { recursive: true });

const db = new DatabaseSync(path.join(dataDir, 'araclar.db'));

db.exec(`
  CREATE TABLE IF NOT EXISTS araclar (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    satis_tarihi TEXT NOT NULL,
    aciklama TEXT NOT NULL,
    plaka TEXT,
    marka TEXT,
    model TEXT,
    model_yili TEXT,
    sasi_no TEXT,
    motor_no TEXT,
    renk TEXT,
    km TEXT,
    yakit_tipi TEXT,
    vites_tipi TEXT,
    satici_adi TEXT,
    alici_adi TEXT,
    satis_fiyati TEXT,
    vergi_dairesi TEXT,
    vergi_no TEXT,
    olusturma_tarihi TEXT NOT NULL DEFAULT (datetime('now'))
  )
`);

module.exports = db;
