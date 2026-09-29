"""
Riba Veli Anket Sistemi - Django ayarları.

Tüm gizli/ortama bağlı değerler ortam değişkenlerinden okunur:
  SECRET_KEY, DEBUG, ALLOWED_HOSTS, CSRF_TRUSTED_ORIGINS, DATABASE_URL
DATABASE_URL tanımlı değilse SQLite (db.sqlite3) kullanılır;
Supabase/PostgreSQL için DATABASE_URL verilmesi yeterlidir.
"""

import os
from pathlib import Path

import dj_database_url

BASE_DIR = Path(__file__).resolve().parent.parent


def env_list(name, default=''):
    return [v.strip() for v in os.environ.get(name, default).split(',') if v.strip()]


DEBUG = os.environ.get('DEBUG', '1') == '1'
SECRET_KEY = os.environ.get('SECRET_KEY', 'gelistirme-anahtari-yayinda-mutlaka-degistirin' if DEBUG else '')
if not SECRET_KEY:
    raise RuntimeError('SECRET_KEY ortam değişkeni tanımlanmalıdır (DEBUG=0 iken).')

ALLOWED_HOSTS = env_list('ALLOWED_HOSTS', 'localhost,127.0.0.1' if DEBUG else '')
CSRF_TRUSTED_ORIGINS = env_list('CSRF_TRUSTED_ORIGINS')

INSTALLED_APPS = [
    'django.contrib.admin',
    'django.contrib.auth',
    'django.contrib.contenttypes',
    'django.contrib.sessions',
    'django.contrib.messages',
    'django.contrib.staticfiles',
    'anket',
]

MIDDLEWARE = [
    'django.middleware.security.SecurityMiddleware',
    'django.contrib.sessions.middleware.SessionMiddleware',
    'django.middleware.common.CommonMiddleware',
    'django.middleware.csrf.CsrfViewMiddleware',
    'django.contrib.auth.middleware.AuthenticationMiddleware',
    'django.contrib.messages.middleware.MessageMiddleware',
    'django.middleware.clickjacking.XFrameOptionsMiddleware',
]

# Yayında statik dosyaları WhiteNoise sunar (geliştirmede runserver sunar)
if not DEBUG:
    MIDDLEWARE.insert(1, 'whitenoise.middleware.WhiteNoiseMiddleware')

ROOT_URLCONF = 'config.urls'

TEMPLATES = [
    {
        'BACKEND': 'django.template.backends.django.DjangoTemplates',
        'DIRS': [],
        'APP_DIRS': True,
        'OPTIONS': {
            'context_processors': [
                'django.template.context_processors.request',
                'django.contrib.auth.context_processors.auth',
                'django.contrib.messages.context_processors.messages',
                'anket.context_processors.okul',
            ],
        },
    },
]

WSGI_APPLICATION = 'config.wsgi.application'

# Veritabanı: DATABASE_URL (Supabase/PostgreSQL) yoksa SQLite
DATABASES = {
    'default': dj_database_url.config(
        default=f"sqlite:///{BASE_DIR / 'db.sqlite3'}",
        conn_max_age=60,
    )
}

AUTH_PASSWORD_VALIDATORS = [
    {'NAME': 'django.contrib.auth.password_validation.UserAttributeSimilarityValidator'},
    {'NAME': 'django.contrib.auth.password_validation.MinimumLengthValidator'},
    {'NAME': 'django.contrib.auth.password_validation.CommonPasswordValidator'},
    {'NAME': 'django.contrib.auth.password_validation.NumericPasswordValidator'},
]

LANGUAGE_CODE = 'tr'
TIME_ZONE = 'Europe/Istanbul'
USE_I18N = True
USE_TZ = True

STATIC_URL = 'static/'
STATIC_ROOT = BASE_DIR / 'staticfiles'
STORAGES = {
    'default': {'BACKEND': 'django.core.files.storage.FileSystemStorage'},
    'staticfiles': {
        'BACKEND': 'django.contrib.staticfiles.storage.StaticFilesStorage'
        if DEBUG else 'whitenoise.storage.CompressedManifestStaticFilesStorage'
    },
}

DEFAULT_AUTO_FIELD = 'django.db.models.BigAutoField'

LOGIN_URL = 'admin:login'

# Yayında (DEBUG=0) güvenli çerezler
if not DEBUG:
    SESSION_COOKIE_SECURE = True
    CSRF_COOKIE_SECURE = True
    SECURE_PROXY_SSL_HEADER = ('HTTP_X_FORWARDED_PROTO', 'https')
    SECURE_CONTENT_TYPE_NOSNIFF = True

# Okul / anket bilgileri
ANKET_OKUL_ADI = os.environ.get('ANKET_OKUL_ADI', 'Yavuz Sultan Selim İlkokulu')
ANKET_ADI = os.environ.get('ANKET_ADI', 'Riba Veli Anket Sistemi')
ANKET_SORU_METNI = 'Aşağıdakilerden hangisi çocuğunuz için daha önemlidir?'
