from django.conf import settings


def okul(request):
    return {'OKUL_ADI': settings.ANKET_OKUL_ADI, 'ANKET_ADI': settings.ANKET_ADI}
