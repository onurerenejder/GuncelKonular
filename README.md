# 🐟 ARFish - Eğitici AR Balık Uygulaması

## 📖 Proje Hakkında

ARFish, Augmented Reality (AR) teknolojisi kullanarak balıkların anatomisi, davranışları ve ekosistemleri hakkında interaktif eğitim sağlayan bir mobil uygulamadır.

### ✨ Özellikler

- 🦴 **Anatomy Module**: X-Ray görünümü ile balık anatomisi
- 🍖 **Feeding Module**: Beslenme davranışları ve besin zinciri
- 🎯 **Quiz Module**: Interaktif anatomi quizi
- 🏞️ **Habitat Module**: Dinamik su yüzeyi ve habitat simülasyonu
- 🐠 **Interspecies Relations**: Boids algoritması ile sürü davranışı
- 🦈 **Predator Prey**: Av-avcı ilişkileri ve kamuflaj
- 🌀 **Portal Module**: AR portal ile sualtı dünyasına geçiş

---

## 🚀 Hızlı Başlangıç

### Gereksinimler

- Unity 2021.3 LTS veya üzeri
- AR Foundation package
- ARCore/ARKit support

### Kurulum

1. **Projeyi Unity'de aç**
2. **Placeholder asset'leri oluştur:**
   ```
   Menu → Tools → ARFish → Setup Placeholder Assets
   ```
3. **Ana balık GameObject'i oluştur ve modülleri ekle:**
   ```
   Menu → Tools → ARFish → Auto-Connect Module References
   ```
4. **Detaylı adımlar için:** `QUICK_START_GUIDE.md` dosyasını oku

---

## 📁 Proje Yapısı

```
Assets/
├── Scripts/
│   ├── AR/              # AR marker ve tracking
│   ├── Core/            # Ana sistem (FishEntityController, SystemStateManager)
│   ├── Data/            # Veri modelleri (FishData)
│   ├── Interaction/     # Hotspot ve etkileşim
│   ├── Modules/         # Tüm eğitim modülleri
│   ├── Network/         # Network yönetimi
│   ├── UI/              # UI yönetimi
│   └── Editor/          # Unity Editor araçları
├── Prefabs/             # Tüm prefab'lar
│   ├── Fish/            # Balık modelleri
│   ├── Organs/          # Organ modelleri
│   ├── Food/            # Yiyecek modelleri
│   ├── Environment/     # Çevre objeleri
│   └── Particles/       # Particle sistemleri
└── Materials/           # Tüm materyaller
```

---

## 🎮 Modüller

### 1. Anatomy Module
X-Ray tarama ile balık anatomisini keşfet:
- İskelet görünümü
- Organ sistemleri
- Pulse (nabız) animasyonları
- Shader-based clipping

### 2. Feeding Module
Beslenme davranışları:
- IK-based head tracking
- Besin zinciri görselleştirmesi
- Enerji akışı animasyonları
- Carnivore/Herbivore modları

### 3. Quiz Module
Interaktif anatomi quizi:
- Hotspot-based etkileşim
- Zaman bazlı skorlama
- Particle feedback efektleri
- Dinamik soru sistemi

### 4. Habitat Module
Dinamik habitat simülasyonu:
- Prosedürel su yüzeyi (wave simulation)
- Çevre objeleri scatter algoritması
- Biome profilleri (Coral Reef, Deep Ocean)
- Dinamik lighting ve fog

### 5. Interspecies Relations
Türler arası ilişkiler:
- Boids algoritması (sürü davranışı)
- Simbiyotik ilişkiler
- Kamera avoidance
- Cohesion, separation, alignment

### 6. Predator Prey
Av-avcı dinamikleri:
- AI-based chase sistemi
- Field of View (FoV) tracking
- Kamuflaj sistemi
- Mürekkep bulutu savunması

### 7. Portal Module
AR portal mekanikleri:
- Fiziksel geçiş algılama
- Dimension switching
- Underwater environment toggle

---

## 🛠️ Geliştirici Araçları

### Unity Editor Menüsü

**Tools → ARFish → Setup Placeholder Assets**
- Tüm placeholder asset'leri otomatik oluşturur
- Primitive'lerden prefab'lar üretir
- Material'leri otomatik atar

**Tools → ARFish → Auto-Connect Module References**
- Prefab referanslarını otomatik bağlar
- Eksik modülleri ekler
- Inspector ayarlarını yapar

---

## 📚 Dokümantasyon

- **QUICK_START_GUIDE.md** - 5 dakikada projeyi çalıştır
- **ASSET_REQUIREMENTS_GUIDE.md** - Detaylı asset gereksinimleri
- **API Documentation** - Code comments içinde

---

## 🎨 Asset Gereksinimleri

### Minimum (Placeholder ile):
✅ Unity Primitive'ler  
✅ Built-in Particle System  
✅ Standard Shader  

### Önerilen (Gerçek modeller):
- Ana balık modeli (rigged, animated)
- İskelet modeli
- Organ modelleri
- Yiyecek modelleri (et, bitki)
- Sürü balığı (low poly)
- Avcı balık (köpekbalığı, vb.)
- Habitat objeleri (mercan, kayalar)
- Portal objesi

**Detaylar için:** `ASSET_REQUIREMENTS_GUIDE.md`

---

## 🔧 Teknik Detaylar

### Mimari

```
SystemStateManager (Singleton)
    ↓
FishEntityController
    ↓
Modules (IModule interface)
    ├── AnatomyModule
    ├── FeedingModule
    ├── QuizModule
    ├── HabitatModule
    ├── InterspeciesRelationsModule
    ├── PredatorPreyModule
    └── PortalModule
```

### Event Sistemi

```csharp
// State değişimi
SystemStateManager.OnStateChanged += HandleStateChanged;

// Hotspot tıklama
HotspotNode.OnAnyHotspotTapped += ValidateHotspotTap;
```

### Modül Lifecycle

```csharp
public interface IModule
{
    ModuleType GetModuleType();
    void OnModuleActivated();
    void OnModuleDeactivated();
}
```

---

## 🐛 Bilinen Sorunlar ve Çözümler

### Quiz → Hotspot Bağlantı Bug'ı
**Durum:** ✅ Düzeltildi  
**Çözüm:** Event subscription eklendi

### 3D Model Eksikliği
**Durum:** ✅ Çözüldü  
**Çözüm:** Placeholder asset generator eklendi

### Prefab Referans Hatası
**Durum:** ✅ Çözüldü  
**Çözüm:** Auto-connect tool eklendi

---

## 📊 Performans

### Hedef Platform: Mobile AR

- **Target FPS:** 60
- **Polygon Budget:** 50K per frame
- **Draw Calls:** <100
- **Texture Memory:** <200MB

### Optimizasyon İpuçları

1. Boids sayısını azalt (schoolSize = 10)
2. Habitat obje yoğunluğunu azalt (objectDensity = 15)
3. LOD kullan (gerçek modellerde)
4. Occlusion culling aktif et
5. Particle count'u düşür

---

## 🧪 Test Etme

### Unity Editor'de Test

```csharp
// Console'da çalıştır
SystemStateManager.Instance.SwitchModule(ModuleType.Anatomy);
SystemStateManager.Instance.SwitchModule(ModuleType.Feeding);
SystemStateManager.Instance.SwitchModule(ModuleType.Quiz);
```

### AR Test (Mobil Cihaz)

1. Build Settings → Android/iOS
2. AR Foundation ayarlarını kontrol et
3. Build & Run
4. Marker'ı tara
5. Modülleri test et

---

## 🤝 Katkıda Bulunma

1. Fork yapın
2. Feature branch oluşturun (`git checkout -b feature/AmazingFeature`)
3. Commit yapın (`git commit -m 'Add some AmazingFeature'`)
4. Push yapın (`git push origin feature/AmazingFeature`)
5. Pull Request açın

---

## 📝 Lisans

Bu proje eğitim amaçlı geliştirilmiştir.

---

## 👥 Ekip

- **Geliştirici:** ARFish Team
- **Unity Version:** 2021.3 LTS
- **AR Framework:** AR Foundation

---

## 📞 İletişim

Sorularınız için:
- GitHub Issues
- Project Wiki
- Documentation

---

## 🎯 Roadmap

### ✅ Tamamlanan
- [x] Tüm modül scriptleri
- [x] Event sistemi
- [x] Placeholder asset generator
- [x] Auto-connect tool
- [x] Quiz → Hotspot bug fix

### 🚧 Devam Eden
- [ ] UI tasarımı (MainUIManager)
- [ ] Audio sistemi (AudioInformationManager)
- [ ] Network entegrasyonu (NetworkStateManager)

### 📅 Planlanan
- [ ] Gerçek 3D modeller
- [ ] Multiplayer support
- [ ] Cloud database entegrasyonu
- [ ] Analytics sistemi
- [ ] Localization (TR/EN)

---

## 🌟 Özellikler (Detaylı)

### Anatomy Module
- ✅ X-Ray shader clipping
- ✅ Skeletal animation
- ✅ Organ pulse simulation
- ✅ Smooth transitions

### Feeding Module
- ✅ IK head tracking
- ✅ Food chain visualization
- ✅ Energy flow animation
- ✅ Diet type support (Carnivore/Herbivore)

### Quiz Module
- ✅ Time-based scoring
- ✅ Hotspot validation
- ✅ Particle feedback
- ✅ Cloud question database

### Habitat Module
- ✅ Dynamic water surface (wave simulation)
- ✅ Procedural object scattering
- ✅ Biome profiles
- ✅ Environmental effects (fog, lighting)

### Interspecies Relations
- ✅ Boids algorithm (3 rules)
- ✅ Camera avoidance
- ✅ Symbiotic relationships
- ✅ School size configuration

### Predator Prey
- ✅ AI chase system
- ✅ Field of View tracking
- ✅ Camouflage system
- ✅ Ink cloud defense

### Portal Module
- ✅ Physical crossing detection
- ✅ Dimension switching
- ✅ Audio/visual transitions

---

## 💻 Sistem Gereksinimleri

### Geliştirme
- **OS:** Windows 10/11, macOS 10.15+
- **RAM:** 8GB minimum, 16GB önerilen
- **GPU:** DirectX 11 uyumlu
- **Disk:** 5GB boş alan

### Çalıştırma (Mobile)
- **Android:** 7.0+ (ARCore support)
- **iOS:** 11.0+ (ARKit support)
- **RAM:** 2GB minimum
- **Camera:** AR uyumlu

---

**Başarılar! 🐟🌊🚀**
