from django.urls import path

from . import views

app_name = 'anket'

urlpatterns = [
    path('', views.anket, name='anket'),
    path('tesekkurler/', views.tesekkurler, name='tesekkurler'),
    path('api/kod/<str:kod>/', views.kod_kontrol, name='kod_kontrol'),

    path('panel/', views.panel, name='panel'),
    path('panel/excel/', views.sonuc_excel, name='sonuc_excel'),
    path('panel/yanit/<int:pk>/sil/', views.yanit_sil, name='yanit_sil'),
    path('panel/sifirla/', views.sifirla, name='sifirla'),

    path('panel/kodlar/', views.kodlar, name='kodlar'),
    path('panel/kodlar/excel/', views.kod_excel, name='kod_excel'),
    path('panel/kodlar/yazdir/', views.kod_yazdir, name='kod_yazdir'),
    path('panel/kodlar/kullanilmayanlari-sil/', views.kullanilmayanlari_sil, name='kullanilmayanlari_sil'),
    path('panel/kodlar/<str:kod>/sil/', views.kod_sil, name='kod_sil'),
]
