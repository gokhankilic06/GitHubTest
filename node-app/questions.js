// Anket soruları. Her soruda iki şık vardır:
//   a / b      : veliye gösterilen tam metin
//   shortA/B   : yönetici panelindeki "Soruya Göre Filtrelenmiş Sonuçlar" tablosunda görünen kısa metin

const SCHOOL_NAME = 'Yavuz Sultan Selim İlkokulu';
const SURVEY_NAME = 'Riba Veli Anket Sistemi';
const QUESTION_TEXT = 'Aşağıdakilerden hangisi çocuğunuz için daha önemlidir?';

const CLASSES = ['Anasınıfı-A', 'Anasınıfı-B'];
for (let grade = 1; grade <= 4; grade++) {
  for (const section of ['A', 'B', 'C', 'D', 'E']) CLASSES.push(`${grade}-${section}`);
}

const QUESTIONS = [
  {
    a: 'Özgüvenini geliştirme',
    b: 'Sağlıklı yaşam becerilerini (spor yapmak, sağlıklı beslenmek gibi) kazanma',
    shortA: 'Özgüvenini geliştirme',
    shortB: 'Sağlıklı yaşam becerilerini (spor yapmak, sağlıklı beslenmek gibi) kazanma'
  },
  {
    a: 'Başarılı olmak için hedefler belirleme',
    b: 'İlgilerini (kodlama, spor, resim, müzik gibi) keşfetme',
    shortA: 'Başarılı olmak için hedefler belirleme',
    shortB: 'İlgilerini (kodlama, spor, resim, müzik gibi) keşfetme'
  },
  {
    a: 'Hem ilişkilerini hem de haklarını koruyacak şekilde çatışmalarını çözmeyi öğrenme',
    b: 'Sorumluluklarının (ör., ödevlerini tamamlama, odasını, eşyalarını düzenleme ve temiz tutma) bilincinde olma',
    shortA: 'Hem ilişkilerini hem de haklarını koruyacak şekilde çatışmalarını çözmeyi öğrenme',
    shortB: 'Sorumluluklarının bilincinde olma'
  },
  {
    a: 'Rehber öğretmen/psikolojik danışmandan hangi konularda yardım alabileceğini öğrenme',
    b: 'Aile üyeleriyle iletişimini güçlendirme',
    shortA: 'Rehber öğretmen/psikolojik danışmandan yardım alma',
    shortB: 'Aile üyeleriyle iletişimini güçlendirme'
  },
  {
    a: 'Arkadaş edinme ve arkadaşlık ilişkilerini sürdürme',
    b: 'Okula devam etme isteğini arttırma',
    shortA: 'Arkadaş edinme ve arkadaşlık ilişkilerini sürdürme',
    shortB: 'Okula devam etme isteğini arttırma'
  },
  {
    a: 'Tehlikeli olabilecek durumlarda (merdivenden inip çıkarken ya da koridorda koşma, servis kurallarına uyma gibi) dikkatli davranma',
    b: 'Ders çalıştığı sırada silgiyle oynama, resim çizme gibi dikkatini dağıtan davranışlardan uzak durmayı öğrenme',
    shortA: 'Tehlikeli durumlarda dikkatli davranma',
    shortB: 'Dikkatini dağıtan davranışlardan uzak durma'
  },
  {
    a: 'Ders çalışma ortamını (odasını, masasını) nasıl düzenleyeceğini öğrenme',
    b: 'Ortaokullar hakkında bilgi edinme',
    shortA: 'Ders çalışma ortamını düzenleme',
    shortB: 'Ortaokullar hakkında bilgi edinme'
  },
  {
    a: 'Duygularını ve isteklerini saygılı bir şekilde ifade etme',
    b: 'Zorbalıkla karşılaştığında (ör., kötü söz söyleme, vurma) ne yapması gerektiğini bilme',
    shortA: 'Duygularını ifade etme',
    shortB: 'Zorbalıkla başa çıkma'
  },
  {
    a: 'Bilgisayar, cep telefonu, tablet veya televizyonu kullanırken uygun içerik seçme ve kullanma süresini belirleme',
    b: 'Yeteneklerini (neleri iyi yapabildiğini) tanıma',
    shortA: 'Uygun içerik seçimi ve süre belirleme',
    shortB: 'Yeteneklerini tanıma'
  },
  {
    a: 'Okuldaki kulüpler (spor, satranç, tiyatro vb.) ve yarışmalar gibi etkinlikler hakkında bilgilenme',
    b: 'Verimli ders çalışmayı öğrenme',
    shortA: 'Kulüp ve etkinlik bilgisi',
    shortB: 'Verimli ders çalışmayı öğrenme'
  },
  {
    a: 'İstismardan korunmayı öğrenme',
    b: 'Derslerde zorlandığında bile çalışarak başarılı olacağına inanma',
    shortA: 'İstismardan korunma',
    shortB: 'Zorlansa da başarılı olacağına inanma'
  },
  {
    a: 'Kendini korumak için "HAYIR" diyebilmeyi öğrenme',
    b: 'Meslek edinmenin önemini anlama',
    shortA: '"HAYIR" diyebilme',
    shortB: 'Meslek edinmenin önemini anlama'
  },
  {
    a: 'Ders çalışmak ve oyun oynamak için zamanını planlama',
    b: 'Zorlandığı konularda doğru kişilerden yardım isteme (ör., zorbalığa uğradığında bir yetişkinden yardım isteme; dersi anlamadığında öğretmeninden yardım isteme)',
    shortA: 'Zamanını planlama',
    shortB: 'Doğru kişilerden yardım isteme'
  }
];

module.exports = { SCHOOL_NAME, SURVEY_NAME, QUESTION_TEXT, CLASSES, QUESTIONS };
