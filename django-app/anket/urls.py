from django.urls import path

from . import views

app_name = 'anket'

urlpatterns = [
    path('', views.anket, name='anket'),
    path('tesekkurler/', views.tesekkurler, name='tesekkurler'),

    path('panel/', views.panel, name='panel'),
    path('panel/excel/', views.sonuc_excel, name='sonuc_excel'),
    path('panel/yanit/<int:pk>/sil/', views.yanit_sil, name='yanit_sil'),
    path('panel/sifirla/', views.sifirla, name='sifirla'),
]
