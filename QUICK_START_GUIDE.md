# 🚀 ARFish Projesi - Hızlı Başlangıç Rehberi

## 📌 5 Dakikada Projeyi Çalıştır!

Bu rehber, projeyi **minimum sürede** test edilebilir hale getirmek için hazırlanmıştır.

---

## ✅ ADIM 1: Unity Editor'ü Aç

1. Unity Hub'ı aç
2. `GuncelKonular` projesini aç
3. Unity Editor'ün tamamen yüklenmesini bekle

---

## ✅ ADIM 2: Placeholder Asset'leri Oluştur (2 dakika)

Unity Editor'de:

1. **Menü → Tools → ARFish → Setup Placeholder Assets**
2. Açılan pencerede **tüm checkbox'ları işaretle**
3. **"Tüm Placeholder'ları Oluştur"** butonuna tıkla
4. "Tamamlandı" mesajını bekle

### Ne Oldu?
✅ Ana balık modeli oluşturuldu  
✅ İskelet modeli oluşturuldu  
✅ Organ sistemleri oluşturuldu  
✅ Yiyecek modelleri oluşturuldu  
✅ Sürü balığı oluşturuldu  
✅ Avcı balık oluşturuldu  
✅ Portal objesi oluşturuldu  
✅ Tüm particle sistemleri oluşturuldu  

**Tüm prefab'lar `Assets/Prefabs/` klasöründe!**

---

## ✅ ADIM 3: Ana Balık GameObject'ini Oluştur (1 dakika)

1. **Hierarchy'de sağ tık → Create Empty**
2. İsim ver: **"MainFish"**
3. Position: **(0, 0, 0)**

### Ana Balık Modelini Ekle:

**Yöntem A - Prefab Kullan:**
1. `Assets/Prefabs/Fish/MainFish.prefab` dosyasını bul
2. Hierarchy'deki **MainFish** GameObject'inin **üzerine sürükle** (child olarak)

**Yöntem B - Manuel Oluştur:**
1. MainFish GameObject'ini seç
2. Sağ tık → 3D Object → Capsule
3. İsim ver: "Body"
4. Scale: (0.3, 0.5, 0.3)
5. Rotation: (0, 0, 90)

---

## ✅ ADIM 4: Modül Scriptlerini Ekle (1 dakika)

1. **Hierarchy'de MainFish GameObject'ini seç**
2. **Menü → Tools → ARFish → Auto-Connect Module References**
3. Açılan pencerede **"Eksik Modülleri Ekle"** butonuna tıkla
4. "Tamamlandı" mesajını bekle

### Ne Oldu?
✅ FishEntityController eklendi  
✅ AnatomyModule eklendi  
✅ FeedingModule eklendi  
✅ QuizModule eklendi  
✅ HabitatModule eklendi  
✅ InterspeciesRelationsModule eklendi  
✅ PredatorPreyModule eklendi  
✅ PortalModule eklendi  

---

## ✅ ADIM 5: Referansları Otomatik Bağla (30 saniye)

Aynı pencerede:

1. **"Ana Balık GameObject"** alanına **MainFish**'i sürükle
2. **"Referansları Otomatik Bağla"** butonuna tıkla
3. "X referans başarıyla bağlandı" mesajını bekle

### Ne Oldu?
✅ Tüm prefab'lar modüllere bağlandı  
✅ Particle sistemleri bağlandı  
✅ Camera referansları ayarlandı  
✅ Transform referansları ayarlandı  

---

## ✅ ADIM 6: SystemStateManager'ı Ekle (30 saniye)

1. **Hierarchy'de sağ tık → Create Empty**
2. İsim ver: **"GameManager"**
3. **Inspector → Add Component → System State Manager**

---

## ✅ ADIM 7: Test Et! (1 dakika)

### Play Butonuna Bas!

Unity Editor'de **Play** butonuna tıkla.

### Console'da Göreceğin Mesajlar:

```
[Gamification Engine] LEVEL 1: Which critical organ extracts dissolved oxygen...
✅ AnatomyModule: Skeleton bağlandı
✅ FeedingModule: Prey prefab bağlandı
✅ QuizModule: Confetti particle bağlandı
```

### Modülleri Test Et:

**Console'da şu komutları çalıştır:**

```csharp
// Anatomy modülünü aktif et
SystemStateManager.Instance.SwitchModule(ModuleType.Anatomy);

// Feeding modülünü aktif et
SystemStateManager.Instance.SwitchModule(ModuleType.Feeding);

// Quiz modülünü aktif et
SystemStateManager.Instance.SwitchModule(ModuleType.Quiz);
```

**VEYA**

Inspector'da **SystemStateManager** component'ini bul ve **Current Module** dropdown'ından modül seç!

---

## 🎮 MODÜL TESTLERİ

### 1. Anatomy Module Testi
```
SystemStateManager → Current Module → Anatomy
```
**Göreceğin:**
- İskelet modeli görünür hale gelir
- Organlar pulse (nabız) efekti yapar
- X-Ray scan animasyonu çalışır

### 2. Feeding Module Testi
```
SystemStateManager → Current Module → Feeding
```
**Göreceğin:**
- Yiyecek spawn olur
- Balık yiyeceğe doğru eğilir (IK)
- Yeme animasyonu çalışır
- Particle efekti patlar

### 3. Quiz Module Testi
```
SystemStateManager → Current Module → Quiz
```
**Göreceğin:**
- Console'da quiz sorusu görünür
- Organlara tıkla (Scene view'da)
- Doğru cevap → Konfeti patlar
- Yanlış cevap → Hata efekti

### 4. Interspecies Relations Testi
```
SystemStateManager → Current Module → InterspeciesRelations
```
**Göreceğin:**
- 20 adet küçük balık spawn olur
- Boids algoritması ile sürü hareketi
- Kameraya yaklaşınca kaçarlar

### 5. Predator Prey Testi
```
SystemStateManager → Current Module → PredatorPrey
```
**Göreceğin:**
- Büyük avcı balık spawn olur
- Ana balığı takip eder
- Yaklaşınca mürekkep bulutu patlar
- Balık rengi değişir (kamuflaj)

### 6. Portal Module Testi
```
SystemStateManager → Current Module → Portal
```
**Göreceğin:**
- Portal objesi aktif olur
- Kamera portal'dan geçince log mesajı

---

## 🐛 SORUN GİDERME

### "NullReferenceException" Hatası
**Çözüm:**
1. Inspector'da ilgili modülü aç
2. Kırmızı (missing) referansları kontrol et
3. `Assets/Prefabs/` klasöründen manuel olarak sürükle

### "Prefab bulunamadı" Hatası
**Çözüm:**
1. **Tools → ARFish → Setup Placeholder Assets** tekrar çalıştır
2. `Assets/Prefabs/` klasörünü kontrol et

### "Modül çalışmıyor"
**Çözüm:**
1. SystemStateManager sahneye eklendi mi?
2. MainFish GameObject'inde tüm modüller var mı?
3. Console'da hata var mı?

### "Particle görünmüyor"
**Çözüm:**
1. Scene view'da particle'ı seç
2. Inspector → Particle System → Play
3. Renderer → Material kontrol et

---

## 📊 PERFORMANS OPTİMİZASYONU

### Mobile AR için:

1. **Edit → Project Settings → Quality**
   - Quality Level: **Medium**
   - Anti Aliasing: **2x Multi Sampling**
   - Shadow Distance: **20**

2. **Boids Sayısını Azalt:**
   ```
   InterspeciesRelationsModule → School Size = 10 (varsayılan: 20)
   ```

3. **Habitat Obje Sayısını Azalt:**
   ```
   HabitatModule → Object Density = 15 (varsayılan: 25)
   ```

---

## 🎨 GERÇEK MODELLERE GEÇİŞ

Placeholder'lar çalıştıktan sonra:

1. **Asset Store'dan gerçek modelleri indir**
2. **Prefab'ları değiştir:**
   ```
   Assets/Prefabs/Fish/MainFish.prefab → Gerçek balık modeli
   Assets/Prefabs/Fish/ApexPredator.prefab → Gerçek köpekbalığı
   ```
3. **Inspector'da referansları güncelle**
4. **Test et!**

---

## 📚 SONRAKI ADIMLAR

✅ Tüm modüller çalışıyor → **AR Foundation Entegrasyonu**  
✅ AR çalışıyor → **UI Tasarımı** (MainUIManager)  
✅ UI hazır → **Audio Ekleme** (AudioInformationManager)  
✅ Audio hazır → **Network Entegrasyonu** (NetworkStateManager)  
✅ Network hazır → **Build & Deploy!**  

---

## 🎯 BAŞARI KRİTERLERİ

Projen hazır sayılır eğer:

- [x] Play'e bastığında hata yok
- [x] Her modül aktif edilebiliyor
- [x] Particle efektleri çalışıyor
- [x] Boids algoritması çalışıyor
- [x] Quiz sistemi tıklamalara cevap veriyor
- [x] Predator chase sistemi çalışıyor

**Hepsi ✅ ise → Projen çalışıyor! 🎉**

---

## 💡 İPUÇLARI

1. **Her zaman Console'u aç tut** - Debug mesajları çok bilgilendirici
2. **Scene view'da Gizmos'u aç** - Collider'ları ve referansları görebilirsin
3. **Inspector'da Debug mode'u kullan** - Private field'ları görebilirsin
4. **Profiler'ı kullan** - Performans sorunlarını tespit et

---

## 📞 YARDIM

Sorun yaşarsan:

1. **Console'u kontrol et** (hata mesajları)
2. **ASSET_REQUIREMENTS_GUIDE.md** dosyasını oku
3. **Inspector'da missing reference var mı bak**
4. **Her modülü tek tek test et**

**Başarılar! 🐟🌊🚀**
