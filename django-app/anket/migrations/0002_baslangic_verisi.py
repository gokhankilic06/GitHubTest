from django.db import migrations

from anket.baslangic import SINIFLAR, SORULAR


def yukle(apps, schema_editor):
    Sinif = apps.get_model('anket', 'Sinif')
    Soru = apps.get_model('anket', 'Soru')
    for i, ad in enumerate(SINIFLAR, start=1):
        Sinif.objects.get_or_create(ad=ad, defaults={'sira': i})
    for i, (a, b, kisa_a, kisa_b) in enumerate(SORULAR, start=1):
        Soru.objects.get_or_create(sira=i, defaults={'secenek_a': a, 'secenek_b': b, 'kisa_a': kisa_a, 'kisa_b': kisa_b})


class Migration(migrations.Migration):
    dependencies = [('anket', '0001_ilk')]
    operations = [migrations.RunPython(yukle, migrations.RunPython.noop)]
