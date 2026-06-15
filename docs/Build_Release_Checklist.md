# Build / Release Checklist — AR Balık Uygulaması

> Unity 2022.3 LTS | AR Foundation | Photon PUN2  
> Son güncelleme: Haziran 2026

---

## 1. Ön Koşullar

- [ ] Unity Hub kurulu ve Unity 2022.3.x LTS lisansı aktif
- [ ] **Android Build Support** modülü Unity'ye eklenmiş (`Unity Hub > Installs > Add modules`)
- [ ] **iOS Build Support** modülü eklenmiş (macOS zorunlu)
- [ ] **Android SDK** ve **JDK** yolları `Edit > Preferences > External Tools` bölümünde doğrulanmış
- [ ] Photon PUN2 paketi Import edilmiş (`Assets > Import Package`) ve **AppId** `PhotonServerSettings`'e girilmiş
- [ ] AR Foundation + ARCore XR Plugin / ARKit XR Plugin paketleri Package Manager'da güncel

---

## 2. Proje Ayarları (Project Settings)

### Genel
- [ ] `Edit > Project Settings > Player > Company Name` doldurulmuş
- [ ] `Product Name` ve `Version` (örn. `1.0.0`) ayarlanmış
- [ ] `Bundle Identifier` tanımlanmış (örn. `com.siateknoloji.arfish`)

### Android
- [ ] Minimum API Level: **Android 8.0 (API 26)** (ARCore gereksinimi)
- [ ] Target API Level: en güncel stabil (örn. API 34)
- [ ] `Scripting Backend`: **IL2CPP**
- [ ] `Target Architectures`: ARM64 işaretli (ARM7 kaldırılabilir — küçük APK)
- [ ] `Internet Access`: **Require** (Photon için)
- [ ] `Write Permission`: **External (SDCard)` — log dosyaları için

### iOS
- [ ] Minimum iOS Version: **14.0**
- [ ] `Camera Usage Description` açıklaması girilmiş (App Store zorunluluğu)
- [ ] `Microphone Usage Description` (varsa)
- [ ] `Scripting Backend`: **IL2CPP**

---

## 3. Sahne ve İçerik Doğrulama

- [ ] `Assets/Scenes/MainScene.unity` açılıp Play Mode'da hatasız çalışıyor
- [ ] 8 balık için JSON dosyaları mevcut: `Assets/Resources/FishJson/*.json`
  - [ ] shark.json, clownfish.json, seabass.json, tuna.json
  - [ ] salmon.json, trout.json, ray.json, dolphin.json
- [ ] `FishSelectionManager` sahnede var ve Fish Options listesi dolu
- [ ] `SystemStateManager` singleton sahnede var
- [ ] `ARMarkerHandler` veya AR Session Origin sahnede bağlı
- [ ] `Canvas` sahnede mevcut (QuizModule EnsureQuizUI() için)
- [ ] Ses dosyaları `Resources/Audio/<id>/` altında yerleştirilmiş

---

## 4. Android APK Derleme

```
File > Build Settings > Android seç > Switch Platform
```

1. [ ] `Build Settings`'te yalnızca `MainScene` listelenmiş
2. [ ] **Build** (sadece APK) veya **Build And Run** (bağlı cihaza doğrudan yükle)
3. [ ] APK adı: `ARFishApp_v1.0.0_<tarih>.apk`
4. [ ] Oluşan APK `Builds/Android/` klasörüne taşındı

### İmzalama (Release için)
- [ ] `Player Settings > Publishing Settings > Keystore Manager` ile keystore oluşturulmuş/seçilmiş
- [ ] Keystore şifresi güvenli bir yerde saklandı (sürüm kontrol sistemine ekleme!)
- [ ] `Build > Release` modunda tekrar build alındı

---

## 5. iOS IPA Derleme (macOS gerekli)

```
File > Build Settings > iOS seç > Switch Platform > Build
```

1. [ ] Xcode projesi `Builds/iOS/` klasörüne oluşturuldu
2. [ ] Xcode'da açıp `Signing & Capabilities` bölümünde Apple Developer hesabı seçildi
3. [ ] `Bundle Identifier` Apple Developer portalındaki App ID ile eşleşiyor
4. [ ] `Product > Archive` ile arşiv oluşturuldu
5. [ ] `Distribute App > Ad Hoc` veya `App Store Connect` seçildi

---

## 6. Cihaz Testi (Yayın Öncesi)

### Android Fiziksel Cihaz Testi
- [ ] ARCore destekli cihazda çalışıyor (Pixel, Samsung Galaxy vb.)
- [ ] Kamera izni alındığında AR Surface Detection çalışıyor
- [ ] Balık seçim butonları görünüyor ve tıklanabiliyor
- [ ] En az 2 modül (Anatomi + Quiz) uçtan uca test edildi
- [ ] Photon bağlantısı: Öğretmen → Öğrenci modül geçişi çalışıyor
- [ ] FPS > 30 stabil (Android Profiler ile doğrula)
- [ ] Bellek kullanımı < 400 MB (Unity Profiler Memory bölümü)

### iOS Fiziksel Cihaz Testi
- [ ] ARKit destekli iPhone/iPad'de çalışıyor (iPhone XS ve üzeri)
- [ ] Kamera kullanım açıklaması ekranda görünüyor
- [ ] Tüm modüller crash vermeden açılıyor
- [ ] Uygulama arka plana alınıp geri dönünce AR oturumu devam ediyor

---

## 7. Performans Kontrol Listesi

| Metrik | Hedef | Kontrol |
|--------|-------|---------|
| FPS (cihaz üzeri) | >= 30 FPS | [ ] |
| Başlangıç süresi | <= 5 saniye | [ ] |
| Bellek kullanımı | <= 400 MB | [ ] |
| Batarya ısısı (30 dk) | Kritik ısınma yok | [ ] |
| Photon ping süresi | <= 150 ms | [ ] |
| JSON yükleme süresi | Fark edilmez (< 0.5 s) | [ ] |

---

## 8. Hata Toleransı Kontrolleri

- [ ] JSON dosyası bozuk → uygulama çökmüyor, hata loglanıyor (FishJsonDatabase try-catch)
- [ ] AR marker algılanmıyor → kullanıcıya bilgi mesajı gösteriliyor
- [ ] Photon bağlantısı kesildi → lokal mod devreye giriyor
- [ ] Ses dosyası yüklenemedi → sessiz devam ediyor (null check mevcut)
- [ ] Sahne referansları eksik → NullReferenceException görülmüyor

---

## 9. Demo / Sunum Hazırlığı

- [ ] Demo video çekildi (`docs/Demo_video.mp4` yerine konuldu)
- [ ] Trello panosu güncel (`https://trello.com/b/eJzzLIqG/guncelk`)
- [ ] `docs/` klasörü tam: SWOT.pdf, Requirements.pdf, RAMS.pdf, THS_report.pdf, UserScenario.pdf
- [ ] README.md son halinde incelendi ve güncel
- [ ] APK test cihazına yüklendi ve sunum öncesi açılıp kapatıldı

---

## 10. Sürüm Notları Şablonu

```
Sürüm: v1.0.0
Tarih: <YYYY-AA-GG>
Platform: Android (<min API>) / iOS (<min iOS>)
Build No: <Unity Build Number>

Değişiklikler:
- [YENİ] Quiz Modülü — zaman bazlı puanlama, sonuç ekranı
- [YENİ] Tüm 8 balık için JSON içerikleri tamamlandı
- [YENİ] Network Modülü — Photon PUN2 öğretmen/öğrenci sync
- [DÜZELTİLDİ] JSON parse hatası try-catch ile yönetildi
- [İYİLEŞTİRME] QuizModule runtime UI oluşturma (Inspector bağımlılığı kaldırıldı)
```
