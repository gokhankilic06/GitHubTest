using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace AracSatisSistemi.Data
{
    // Uygulama tr-TR kültüründe çalıştığı için (virgül ondalık ayracı), ASP.NET Core'un
    // varsayılan model binder'ı formdan gelen decimal değerleri CurrentCulture (tr-TR) ile
    // ayrıştırır. Ancak HTML "number" inputları, sayfanın diline bakılmaksızın değeri HER ZAMAN
    // nokta ondalık ayracıyla gönderir (ör. "99.99"); tr-TR ayrıştırması bu noktayı binlik ayracı
    // sanıp sildiğinden "99.99" yanlışlıkla 9999 olarak kaydedilir. Bu, tüm para alanlarını
    // (Muhammen Bedel, Satış Bedeli, tarifeler, vb.) etkileyen ciddi bir veri bozulması riskidir.
    // Bu binder, decimal/decimal? alanlarını her zaman InvariantCulture (nokta ondalık ayracı)
    // ile ayrıştırarak formdan gelen gerçek değeri doğru okur; ekranda gösterim (ToString) hâlâ
    // tr-TR biçiminde (virgüllü) kalmaya devam eder, sadece GİRİŞ ayrıştırması düzeltilir.
    public class InvariantDecimalModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            ArgumentNullException.ThrowIfNull(bindingContext);

            var modelName = bindingContext.ModelName;
            var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);
            if (valueProviderResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

            var value = valueProviderResult.FirstValue;
            var isNullable = Nullable.GetUnderlyingType(bindingContext.ModelType) != null;

            if (string.IsNullOrWhiteSpace(value))
            {
                if (!isNullable)
                {
                    bindingContext.ModelState.TryAddModelError(modelName, "Bir değer giriniz.");
                }
                return Task.CompletedTask;
            }

            // Önce noktayı ondalık ayracı sayan InvariantCulture ile denenir (HTML number
            // input'unun doğal biçimi); başarısız olursa, elle/formdan tr-TR biçiminde (virgüllü)
            // girilmiş bir değer olabileceği ihtimaline karşı tr-TR ile de denenir.
            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var sonuc)
                || decimal.TryParse(value, NumberStyles.Number, CultureInfo.GetCultureInfo("tr-TR"), out sonuc))
            {
                bindingContext.Result = ModelBindingResult.Success(sonuc);
            }
            else
            {
                bindingContext.ModelState.TryAddModelError(modelName, "Geçerli bir sayı giriniz.");
            }

            return Task.CompletedTask;
        }
    }

    public class InvariantDecimalModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var modelType = Nullable.GetUnderlyingType(context.Metadata.ModelType) ?? context.Metadata.ModelType;
            return modelType == typeof(decimal) ? new InvariantDecimalModelBinder() : null;
        }
    }
}
