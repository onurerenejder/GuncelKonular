# 🎨 ARFish Projesi - 3D Asset Gereksinimleri ve Kurulum Rehberi

## 📋 Genel Bakış
Bu dokümanda ARFish projesinin çalışması için gerekli tüm 3D modeller, prefab'lar ve asset'ler detaylıca açıklanmıştır.

---

## 🐟 1. ANA BALIK MODELİ (Ana Karakter)

### Gereksinimler:
- **Dosya Formatı**: FBX veya OBJ
- **Polygon Sayısı**: 2000-8000 (Mobile AR için optimize)
- **Texture**: Diffuse, Normal Map (opsiyonel: Specular)
- **Rigging**: Evet (Animasyon için)
- **Animasyonlar**: 
  - Idle (yüzme)
  - Bite/Eat (ısırma)
  - Swim Forward

### Kullanıldığı Modüller:
- ✅ **AnatomyModule** - Dış deri renderer'ı
- ✅ **FeedingModule** - Head bone ve jaw animator
- ✅ **QuizModule** - Hotspot'lar için ana model
- ✅ **PredatorPreyModule** - Prey skin renderer

### Unity'de Yapılacaklar:
1. Asset Store'dan indir: "Low Poly Fish" veya "Realistic Fish Pack"
2. `Assets/Models/MainFish/` klasörüne import et
3. Prefab oluştur: `MainFish.prefab`
4. **Rigging Kontrolü**:
   - Head bone'u bul ve işaretle
   - Jaw bone'u bul (varsa)
5. **Animator Controller Ekle**:
   - `Assets/Animations/FishAnimator.controller` oluştur
   - Bite trigger parametresi ekle
6. **Script Bağlantıları**:
   ```
   FishEntityController → fishDataConfig
   FeedingModule → headBone, jawAnimator, mouthSocket
   PredatorPreyModule → preySkinRenderer
   ```

### Önerilen Asset Store Paketleri:
- "Cartoon Fish Pack" (Ücretsiz)
- "Low Poly Fish" (Ücretsiz)
- "Aquatic Animals Pack" ($)

---

## 🦴 2. İSKELET MODELİ (Anatomy Module)

### Gereksinimler:
- **Dosya Formatı**: FBX
- **Polygon Sayısı**: 1000-3000
- **Renk**: Beyaz/Kemik rengi
- **Şeffaflık**: Hayır (Solid)
- **Ana balık ile aynı boyutta olmalı**

### Kullanıldığı Modül:
- ✅ **AnatomyModule** - X-Ray görünümü için

### Unity'de Yapılacaklar:
1. Asset Store'dan indir veya Blender'da kendin oluştur
2. `Assets/Models/Skeleton/` klasörüne import et
3. Prefab oluştur: `FishSkeleton.prefab`
4. **Material Ayarları**:
   - Shader: Standard veya Unlit
   - Color: Beyaz (#FFFFFF)
   - Emission: Hafif mavi (#00FFFF, 0.2 intensity)
5. **Script Bağlantısı**:
   ```
   AnatomyModule → skeletonModel = FishSkeleton.prefab
   ```

### Alternatif Çözüm:
Eğer hazır iskelet bulamazsan:
1. Ana balık modelini kopyala
2. Wireframe shader uygula
3. Rengi beyaz yap

---

## 🫀 3. ORGAN SİSTEMLERİ (Anatomy Module)

### Gereksinimler:
Her organ için ayrı GameObject:
- **Kalp (Heart)** - Kırmızı, küçük küre
- **Solungaçlar (Gills)** - Kırmızı/pembe, ince yapraklar
- **Yüzgeçler (Fins)** - Dorsal Fin, Pectoral Fin, Tail Fin
- **Mide (Stomach)** - Sarı/turuncu, oval
- **Karaciğer (Liver)** - Kahverengi

### Unity'de Yapılacaklar:
1. `Assets/Models/Organs/` klasörü oluştur
2. Her organ için primitive shape kullan veya basit model oluştur:
   ```
   Heart.prefab → Sphere (Scale: 0.1, Color: Red)
   Gills.prefab → Plane (Scale: 0.2, Color: Pink)
   DorsalFin.prefab → Quad (Scale: 0.3, Color: Gray)
   ```
3. **Her organa HotspotNode scripti ekle**:
   ```csharp
   organName = "Heart"
   infoDescription = "Kanı pompalar..."
   ```
4. **AnatomyModule'de OrganSystem listesini doldur**:
   ```
   System Name: "Circulatory"
   Organ Renderer: Heart.GetComponent<Renderer>()
   Is Pulsating: true
   Pulse Rate: 1.0
   ```

### Önerilen Yaklaşım:
Unity Primitive'leri kullan (Sphere, Cube, Cylinder) - Hızlı ve performanslı!

---

## 🍖 4. YİYECEK MODELLERİ (Feeding Module)

### A) Et/Av Balığı (Carnivore için)
- **Model**: Küçük balık (5-10cm)
- **Polygon**: 500-1000
- **Animasyon**: Yüzme (opsiyonel)
- **Prefab**: `SmallPreyFish.prefab`

### B) Bitki/Alg (Herbivore için)
- **Model**: Deniz yosunu veya alg
- **Polygon**: 200-500
- **Renk**: Yeşil/kahverengi
- **Prefab**: `SeaweedFood.prefab`

### Unity'de Yapılacaklar:
1. Asset Store'dan indir: "Underwater Plants" veya "Small Fish Pack"
2. `Assets/Models/Food/` klasörüne import et
3. **Script Bağlantısı**:
   ```
   FeedingModule → meatPreyPrefab = SmallPreyFish.prefab
   FeedingModule → vegetationPrefab = SeaweedFood.prefab
   ```

### Önerilen Asset Store Paketleri:
- "Low Poly Underwater Plants" (Ücretsiz)
- "Tiny Fish Pack" (Ücretsiz)

---

## 🐠 5. SÜRÜ BALIĞI (Interspecies Relations - Boids)

### Gereksinimler:
- **Model**: Çok basit balık (Low Poly)
- **Polygon**: 100-500 (20 adet spawn olacak!)
- **Texture**: Basit, tek renk
- **Boyut**: Ana balıktan küçük (0.3-0.5x)
- **Animasyon**: Gerekli değil (script hareket ettirecek)

### Unity'de Yapılacaklar:
1. Asset Store'dan indir: "Simple Fish" veya kendin oluştur
2. `Assets/Models/SchoolFish/` klasörüne import et
3. Prefab oluştur: `SchoolingFish.prefab`
4. **Optimizasyon**:
   - LOD Group ekle
   - Collider kaldır (gerekli değil)
   - Shadow Casting: Off
5. **Script Bağlantısı**:
   ```
   InterspeciesRelationsModule → schoolingFishPrefab = SchoolingFish.prefab
   InterspeciesRelationsModule → schoolSize = 20
   ```

### Alternatif Çözüm:
Unity Cube kullan + basit texture (Çok hızlı çözüm!)

---

## 🦈 6. AVCI BALIK (Predator Prey Module)

### Gereksinimler:
- **Model**: Büyük yırtıcı balık (Köpekbalığı, Barracuda, vb.)
- **Polygon**: 3000-6000
- **Boyut**: Ana balıktan 2-3x büyük
- **Animasyon**: Yüzme
- **Texture**: Karanlık renkler (gri, siyah)

### Unity'de Yapılacaklar:
1. Asset Store'dan indir: "Shark Pack" veya "Predator Fish"
2. `Assets/Models/Predator/` klasörüne import et
3. Prefab oluştur: `ApexPredator.prefab`
4. **Script Bağlantısı**:
   ```
   PredatorPreyModule → apexPredatorPrefab = ApexPredator.prefab
   PredatorPreyModule → chasePacingSpeed = 3.8
   ```

### Önerilen Asset Store Paketleri:
- "Low Poly Shark" (Ücretsiz)
- "Ocean Predators Pack" ($)

---

## 🪸 7. HABİTAT OBJE VE DEKORASYON (Habitat Module)

### Gereksinimler:
**Coral Reef için:**
- Mercan modelleri (5-10 çeşit)
- Deniz anemonları
- Kayalar
- Deniz yıldızları

**Deep Ocean için:**
- Karanlık kayalar
- Hidrotermal bacalar
- Derin deniz bitkileri

### Unity'de Yapılacaklar:
1. Asset Store'dan indir: "Coral Reef Pack" veya "Ocean Props"
2. `Assets/Models/HabitatProps/CoralReef/` klasörüne import et
3. `Assets/Models/HabitatProps/DeepOcean/` klasörüne import et
4. Her obje için prefab oluştur
5. **Script Bağlantısı**:
   ```
   HabitatModule → HabitatVisualProfile[0].propPrefabs = [Coral1, Coral2, Rock1, ...]
   HabitatModule → objectDensity = 25
   ```

### Önerilen Asset Store Paketleri:
- "Low Poly Underwater Environment" (Ücretsiz)
- "Coral Reef Pack" (Ücretsiz)
- "Ocean Floor Props" ($)

---

## 🌀 8. PORTAL OBJESİ (Portal Module)

### Gereksinimler:
- **Model**: Kapı çerçevesi veya portal halkası
- **Polygon**: 500-2000
- **Efekt**: Parıltılı/parlak shader
- **Boyut**: İnsan boyunda (2m yükseklik)

### Unity'de Yapılacaklar:
1. Asset Store'dan indir: "Portal Pack" veya kendin oluştur
2. `Assets/Models/Portal/` klasörüne import et
3. Prefab oluştur: `UnderwaterPortal.prefab`
4. **Shader Ayarları**:
   - Shader: Unlit veya Particle
   - Emission: Mavi (#00FFFF, 2.0 intensity)
   - Transparency: 0.5
5. **Particle System Ekle**:
   - Swirl effect
   - Blue particles
6. **Script Bağlantısı**:
   ```
   PortalModule → portalDoorway = UnderwaterPortal.transform
   ```

### Alternatif Çözüm:
1. Unity Cylinder kullan
2. Scale: (2, 0.1, 2)
3. Emissive material ekle

---

## 🤝 9. SİMBİYOTİK PARTNER (Interspecies Relations)

### Gereksinimler:
- **Örnek**: Palyaço balığı için anemon
- **Model**: Küçük organizma
- **Boyut**: Ana balık ile uyumlu
- **Animasyon**: İdeal (sallanma, vb.)

### Unity'de Yapılacaklar:
1. Asset Store'dan indir veya basit model oluştur
2. `Assets/Models/Symbiotic/` klasörüne import et
3. Prefab oluştur: `SymbioticPartner.prefab`
4. **Script Bağlantısı**:
   ```
   InterspeciesRelationsModule → symbioticPartnerPrefab = SymbioticPartner.prefab
   InterspeciesRelationsModule → symbioticAttachPoint = (Ana balığın yanı)
   ```

---

## 🎨 10. PARTİKL SİSTEMLERİ

### Gerekli Particle Sistemleri:

#### A) Kan Efekti (Feeding - Carnivore)
```
FeedingModule → hitBloodMuzzle
- Color: Kırmızı
- Duration: 0.5s
- Particle Count: 20-30
```

#### B) Alg Parçaları (Feeding - Herbivore)
```
FeedingModule → hitAlgaeMuzzle
- Color: Yeşil
- Duration: 0.5s
- Particle Count: 15-20
```

#### C) Mürekkep Bulutu (Predator Prey)
```
PredatorPreyModule → inkOpticJammerParticle
- Color: Siyah/Mor
- Duration: 3.5s
- Particle Count: 100+
- Shape: Sphere
```

#### D) Konfeti (Quiz - Doğru Cevap)
```
QuizModule → successConfettiParticle
- Color: Renkli (Multi)
- Duration: 1s
- Particle Count: 50+
```

#### E) Hata Efekti (Quiz - Yanlış Cevap)
```
QuizModule → errorBuzzerEmission
- Color: Kırmızı
- Duration: 0.3s
- Particle Count: 10-15
```

#### F) Ortam Partikülleri (Habitat)
```
HabitatModule → ambientFloatingParticles
- Color: Beyaz/Mavi
- Looping: true
- Particle Count: 50-100
- Movement: Yavaş yüzen
```

```
HabitatModule → currentDriftParticles
- Color: Beyaz
- Looping: true
- Velocity: Akıntı yönünde
```

### Unity'de Yapılacaklar:
1. Hierarchy'de sağ tık → Effects → Particle System
2. Her efekt için yukarıdaki ayarları yap
3. Prefab olarak kaydet: `Assets/Prefabs/Particles/`
4. Script'lere bağla

---

## 📦 KURULUM ADIMLARI (Öncelik Sırası)

### 1. ÖNCE BUNLARI YAP (Kritik):
1. ✅ Ana Balık Modeli + Rigging
2. ✅ İskelet Modeli
3. ✅ Organ Sistemleri (Primitive'lerle başla)
4. ✅ Particle Sistemleri (Hızlı oluşturulabilir)

### 2. SONRA BUNLARI YAP (Önemli):
5. ✅ Yiyecek Modelleri
6. ✅ Sürü Balığı
7. ✅ Avcı Balık

### 3. EN SON BUNLARI YAP (Opsiyonel):
8. ✅ Habitat Objeleri
9. ✅ Portal Objesi
10. ✅ Simbiyotik Partner

---

## 🎯 HIZLI BAŞLANGIÇ (Minimum Viable Product)

Eğer hızlı test etmek istiyorsan, Unity Primitive'leri kullan:

```
Ana Balık → Capsule (Scale: 1, 0.3, 0.5) + Sphere (baş)
İskelet → Wireframe Capsule
Organlar → Küçük Sphere'ler
Yiyecek → Küçük Cube
Sürü → Mini Capsule'ler
Avcı → Büyük Capsule
Portal → Cylinder (Emissive)
```

Bu şekilde **15 dakikada** tüm sistem çalışır hale gelir!

---

## 📚 ÖNERİLEN ASSET STORE PAKETLERİ (Toplam)

### Ücretsiz:
1. "Low Poly Fish Pack"
2. "Underwater Plants"
3. "Coral Reef Props"
4. "Simple Ocean Animals"

### Ücretli (Opsiyonel):
1. "Realistic Fish Collection" ($15-30)
2. "Complete Underwater Environment" ($20-40)
3. "Ocean Predators Pack" ($10-20)

---

## 🔗 SCRIPT REFERANS TABLOSU

| Script | Gerekli Asset | Prefab Adı |
|--------|---------------|------------|
| AnatomyModule | İskelet + Organlar | FishSkeleton.prefab |
| FeedingModule | Yiyecekler + Particle | SmallPreyFish.prefab, SeaweedFood.prefab |
| HabitatModule | Dekorasyon | Coral1.prefab, Rock1.prefab, ... |
| InterspeciesRelationsModule | Sürü + Partner | SchoolingFish.prefab, SymbioticPartner.prefab |
| PredatorPreyModule | Avcı + Mürekkep | ApexPredator.prefab, InkCloud.prefab |
| PortalModule | Portal | UnderwaterPortal.prefab |
| QuizModule | Particle | Confetti.prefab, ErrorBuzz.prefab |

---

## ✅ KONTROL LİSTESİ

Tüm asset'leri kurduktan sonra kontrol et:

- [ ] Ana balık modeli sahneye yerleştirildi
- [ ] FishEntityController script'i eklendi
- [ ] Tüm modüller GameObject'e component olarak eklendi
- [ ] Her modülün prefab referansları dolduruldu
- [ ] Particle sistemleri oluşturuldu ve bağlandı
- [ ] AR Camera referansı ayarlandı
- [ ] SystemStateManager sahneye eklendi
- [ ] Test: Her modül aktif edildiğinde asset'ler görünüyor mu?

---

## 🆘 SORUN GİDERME

**"Prefab referansı null" hatası:**
- Inspector'da ilgili modülü aç
- Prefab alanlarını manuel olarak doldur

**"Model görünmüyor":**
- Scale kontrolü yap (çok küçük olabilir)
- Layer kontrolü yap (AR Camera görebiliyor mu?)
- Renderer enabled kontrolü

**"Animasyon çalışmıyor":**
- Animator Controller bağlı mı?
- Animation clip import edildi mi?
- Trigger parametreleri doğru mu?

---

## 📞 YARDIM

Bu rehberi takip ederken sorun yaşarsan:
1. Unity Console'u kontrol et (hata mesajları)
2. Inspector'da missing reference var mı bak
3. Her modülü tek tek test et (SystemStateManager ile)

**Başarılar! 🐟🌊**
