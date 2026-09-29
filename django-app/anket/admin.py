from django.contrib import admin
from django.utils.html import format_html

from .models import Cevap, Sinif, Soru, VeliKodu, VeliYaniti


@admin.register(Sinif)
class SinifAdmin(admin.ModelAdmin):
    list_display = ['ad', 'sira']
    list_editable = ['sira']
    search_fields = ['ad']


@admin.register(Soru)
class SoruAdmin(admin.ModelAdmin):
    list_display = ['sira', 'kisa_a', 'kisa_b', 'aktif']
    list_display_links = ['sira']
    list_editable = ['aktif']
    list_filter = ['aktif']


@admin.register(VeliKodu)
class VeliKoduAdmin(admin.ModelAdmin):
    list_display = ['kod', 'sinif', 'durum', 'olusturma', 'kullanim']
    list_filter = ['sinif', ('kullanim', admin.EmptyFieldListFilter)]
    search_fields = ['kod']
    readonly_fields = ['olusturma', 'kullanim']

    @admin.display(description='Durum')
    def durum(self, obj):
        return format_html('<b style="color:{}">{}</b>', '#6c757d' if obj.kullanildi else '#198754',
                           'Kullanıldı' if obj.kullanildi else 'Kullanılmadı')


class CevapInline(admin.TabularInline):
    model = Cevap
    extra = 0
    can_delete = False
    readonly_fields = ['soru', 'secim']

    def has_add_permission(self, request, obj=None):
        return False


@admin.register(VeliYaniti)
class VeliYanitiAdmin(admin.ModelAdmin):
    list_display = ['ad_soyad', 'sinif', 'kod', 'tarih']
    list_filter = ['sinif']
    search_fields = ['ad_soyad', 'kod__kod']
    readonly_fields = ['kod', 'sinif', 'tarih']
    inlines = [CevapInline]
