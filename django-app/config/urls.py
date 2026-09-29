from django.conf import settings
from django.contrib import admin
from django.urls import include, path

admin.site.site_header = f'{settings.ANKET_OKUL_ADI} - Yönetim'
admin.site.site_title = settings.ANKET_ADI
admin.site.index_title = 'Anket yönetimi'

urlpatterns = [
    path('admin/', admin.site.urls),
    path('', include('anket.urls')),
]
