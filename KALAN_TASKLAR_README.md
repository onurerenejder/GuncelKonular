# Kalan Tasklar ve Faz Planı — Güncel Kod Durumu

> Son güncelleme: 15 Haziran 2026
> ✅ = Tamamlandı | 🔶 = Kısmen yapılmış | ❌ = Henüz yapılmadı

---

## Yapılacaklar (Öncelik Sırasına Göre)

### Yüksek Öncelik

| # | Task | Neden Önemli |
|---|------|-------------|
| 1 | Ses dosyalarını FishData SO'ya bağla | 8 balık için `Resources/Audio/<id>/` altındaki klipleri Inspector'dan ScriptableObject'e atamak gerekiyor; olmadan tüm ses anlatımı sessiz çalışıyor |
| 2 | Boids O(n²) → Spatial Hashing veya Unity Jobs | `InterspeciesRelationsModule`'daki döngü her kare tüm balıkları karşılaştırıyor; 10+ balıkta mobilde FPS düşer |
| 3 | FoodChain[] SO alanlarını elle doldur | JSON'larda `predatorPrey` metni var ama FishData.FoodChain[] dizisi boş; besin zinciri görselleştiricisi fallback etikete düşüyor |

### Orta Öncelik

| # | Task | Neden Önemli |
|---|------|-------------|
| 4 | Play Mode testleri (Unity Test Framework) | Modül geçişleri ve state makinesinin doğru çalıştığını otomatik doğrulamak için; şu an manuel test gerekiyor |
| 5 | Model/texture yükleme hata recovery | `Resources.Load` başarısız olduğunda sessiz crash riski var; fallback görsel veya kullanıcı mesajı eksik |
| 6 | DynamicWaterSurface `[ExecuteAlways]` düzeltmesi | Editor'de sahne değişkenken vertex hesaplamaları çalışıyor; modül pasifken durdurulmalı |

### Düşük Öncelik

| # | Task | Notlar |
|---|------|--------|
| 7 | Quiz skoru PlayerPrefs ile kayıt | Oturum kapanınca skor sıfırlanıyor; kalıcı ilerleme takibi yok |
| 8 | FFT görsel EQ bar | `AudioInformationManager.AudioAmplitude` hesaplanıyor ama UI'da hiçbir görsel reaktivite yok |
| 9 | Portal görsel efekti | Algılama mantığı aktif; vortex/shimmer shader ve ses mufling eksik |
| 10 | Ses altyazısı (accessibility) | Her anlatım sesi için ekranda metin gösterimi; erişilebilirlik gereksinimi |

---

## Tamamlananlar

### Kod Değişiklikleri

- ✅ **Object Pooling — FeedingModule** — `SimplePool` iç sınıfı eklendi; `meatPreyPrefab` / `vegetationPrefab` ve `hitBloodMuzzle` / `hitAlgaeMuzzle` için `Instantiate`/`Destroy` yerine havuzlu `Get`/`Return`; `ReturnParticleToPool` coroutine ile partikül iadesi
- ✅ **Object Pooling — PredatorPreyModule** — `SimplePool` eklendi; `generatedApexPredator` ve `inkOpticJammerParticle` havuzlandı; `ReturnToPoolAfter(3.5f)` coroutine
- ✅ **QuizModule UI — runtime oluşturma** — `EnsureQuizUI()` ile Inspector bağımlılığı kaldırıldı; Canvas üzerinde quiz paneli, soru/skor/ilerleme etiketleri ve sonuç ekranı start'ta otomatik yaratılıyor
- ✅ **FishJsonDatabase hata toleransı** — JSON parse hatalarını sessiz çökmeden yakalamak için `try-catch (ArgumentException)` eklendi
- ✅ **FishData yeni alanlar** — `InterspeciesDescription`, `PredatorPreyDescription`, `PortalDescription` alanları eklendi

### JSON İçerikleri (8 Balık)

- ✅ **shark.json** — kıkırdak iskelet, elektro-almaç, 4 quiz sorusu
- ✅ **clownfish.json** — mukus tabakası, mutualizm, cinsiyet değişimi, 4 quiz sorusu
- ✅ **seabass.json** — 2 sırt yüzgeci, okyanus-kıyı adaptasyonu, 4 quiz sorusu
- ✅ **tuna.json** — kısmi endotermi, lunate kuyruk, sürü avı, 4 quiz sorusu
- ✅ **salmon.json** — homing içgüdüsü, kype, yağ depoları, 4 quiz sorusu
- ✅ **trout.json** — adipoz yüzgeç, 18°C eşiği, indikatör tür, 4 quiz sorusu
- ✅ **ray.json** — karın-ağzı, elektromanyetik algı, kanat yüzgeç, 4 quiz sorusu
- ✅ **dolphin.json** — ekolokasyon, akciğer, zaten doluydu

### Dokümanlar

- ✅ **docs/SWOT.pdf** — Gerçek kod analizine göre güçlü/zayıf yönler, fırsatlar, tehditler
- ✅ **docs/Requirements.pdf** — FR-01..FR-10 fonksiyonel gereksinimler + 6 NFR grubu
- ✅ **docs/RAMS.pdf** — Güvenilirlik, Erişilebilirlik, Bakım, Güvenlik risk matrisleri
- ✅ **docs/THS_report.pdf** — 7 TAM / 2 KISMI modül tablosu, hata tolerans matrisi HT-01..HT-10, performans metrikleri
- ✅ **docs/UserScenario.pdf** — 5 kullanıcı senaryosu (öğrenci, öğretmen, bireysel, akvaryum, geliştirici)
- ✅ **docs/Build_Release_Checklist.md** — Android/iOS derleme, imzalama, cihaz testi, performans kontrol listesi
- ✅ **docs/Trello_link.txt** — `https://trello.com/b/eJzzLIqG/guncelk`
- ✅ **README.md revize** — 9 modül durum tablosu, Photon PUN2 / FishJsonDatabase teknoloji yığını, güncel "Başlarken" bölümü

### Mevcut Kod (Baştan Doğrulandı)

- ✅ HotspotInputController — touch + AR raycast input
- ✅ DynamicWaterSurface — 3 katmanlı dalga (ripple / swell / turbulence)
- ✅ NetworkStateManager — Photon PUN2 RPC, öğretmen→öğrenci broadcast, late-join sync
- ✅ AudioInformationManager — FFT reaktivite, modül başına crossfade anlatım, Quiz/Portal ses haritalaması
- ✅ ARMarkerHandler — null güvenlik kontrolleri mevcut
- ✅ QuizModule — HotspotNode event bağlantısı, zaman bazlı puanlama, sonuç ekranı

---

## Hızlı Referans — Önemli Dosya Yolları

| Bileşen | Dosya |
|---------|-------|
| Balık JSON içerikleri | `Assets/Resources/FishJson/<id>.json` |
| Balık ScriptableObject | `Assets/Scripts/Data/FishData.cs` |
| Sistem durum makinesi | `Assets/Scripts/Core/SystemStateManager.cs` |
| Quiz modülü | `Assets/Scripts/Modules/QuizModule.cs` |
| Beslenme modülü (pool'lu) | `Assets/Scripts/Modules/FeedingModule.cs` |
| Av-Avcı modülü (pool'lu) | `Assets/Scripts/Modules/PredatorPreyModule.cs` |
| Ağ yöneticisi | `Assets/Scripts/Network/NetworkStateManager.cs` |
| Build kontrol listesi | `docs/Build_Release_Checklist.md` |
