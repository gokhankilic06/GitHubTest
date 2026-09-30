from django.contrib import admin

from .models import Cevap, Sinif, Soru, VeliYaniti


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


class CevapInline(admin.TabularInline):
    model = Cevap
    extra = 0
    can_delete = False
    readonly_fields = ['soru', 'secim']

    def has_add_permission(self, request, obj=None):
        return False


@admin.register(VeliYaniti)
class VeliYanitiAdmin(admin.ModelAdmin):
    list_display = ['ad_soyad', 'sinif', 'tarih']
    list_filter = ['sinif']
    search_fields = ['ad_soyad']
    readonly_fields = ['tarih']
    inlines = [CevapInline]
