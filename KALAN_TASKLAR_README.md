# 🗂️ Kalan Tasklar ve Faz Planı (15 Task)

Bu doküman yalnızca kalan 15 task'ı içerir ve Trello'ya kart olarak girilecek şekilde düzenlenmiştir.

## Faz 1 - Çekirdek Entegrasyon
- [x] `QuizModule ile HotspotNode Event Bağlantılarını Tamamla`
- [x] `FishData Verisini UI ve Modüllere Gerçekten Bind Et`
- [x] `ARMarkerHandler İçinde Instance Null Güvenlik Kontrollerini Güçlendir`

## Faz 2 - Girdi ve Etkileşim
- [x] `OnMouseDown Yerine Mobil Uyumlu Touch + AR Raycast Input Sistemi Kur`
- [ ] `Null Reference ve Hatalı Prefab Atamaları İçin Runtime Guard Katmanı Ekle` - Kısmi: AR/UI/Quiz tarafı güçlendirildi, tüm prefab/modül atamaları için merkezi guard hâlâ yok.
- [x] `Audio Manager'a Quiz ve Portal İçin Ses Haritalaması Ekle`

## Faz 3 - Ekosistem ve Performans
- [x] `Habitat Modülü için Dinamik Su Simülasyonunu Uygula`
- [ ] `Boids Sürü Simülasyonunda Mobil Performans Optimizasyonu Yap` - Eksik: Boids/sürü simülasyonu kodu görünmüyor.
- [ ] `Cihaz Üzerinde Profiling ve FPS/Batarya Testlerini Planla ve Uygula` - Eksik: profiling/test planı veya sonuç dosyası yok.

## Faz 4 - Network ve Test
- [ ] `Photon PUN Entegrasyonunu Aktifleştir ve RPC Akışını Tamamla` - Kısmi: RPC kodu `PUN_2_OR_NEWER` ile hazır, Photon PUN paketi manifest'te yok.
- [ ] `Play Mode Test Senaryolarını Yaz (State Geçişleri ve Modül Yaşam Döngüsü)` - Eksik: test klasörü/senaryosu bulunmuyor.
- [ ] `ScriptableObject İçeriklerini Tür Bazında Doldur (Anatomi/Habitat/Beslenme)` - Kısmi: `FishData` alanları var, tür bazlı dolu ScriptableObject seti görünmüyor.

## Faz 5 - Dokümantasyon ve Yayın
- [ ] `Ecosystem/Predator-Prey Yapısını Kod-Doküman Açısından Tutarlı Hale Getir` - Eksik: README hâlâ bazı eski modül durumlarını söylüyor.
- [ ] `README'yi Güncel Kod Durumuna Göre Revize Et (Quiz/Portal dahil)` - Eksik: Quiz/Portal ve dinamik su durumu güncel değil.
- [ ] `Build/Release Checklist Dokümanını Oluştur (Android/iOS)` - Eksik: ayrı checklist dokümanı bulunmuyor.

## Öncelikli Detaylandırılmış Tasklar (Ek)
- [x] `Habitat modülüne dinamik su simülasyonu eklenmesi` - `DynamicWaterSurface` ve `HabitatModule` entegrasyonu eklendi.
- [ ] `Predator-Prey modülündeki hata ve eksiklerin tamamlanması` - Kısmi: avcı takibi/kaçış davranışı var, modül hâlâ veri ve test açısından tamamlanmış sayılmıyor.
- [x] `Feeding modülüne gerçek besin zinciri görselleştirmesi eklenmesi`
- [x] `FishData veri yapısının genişletilmesi`
- [x] `FishEntityController veri bağlama sisteminin tamamlanması`
- [ ] `Quiz sistemi için gerçek UI ve skor ekranı yapılması` - Mantık var, oyuncu ekranı eksik.
- [x] `Hotspot ile quiz entegrasyonunun tamamlanması`
- [ ] `Photon/network öğretmen-öğrenci senkronizasyonunun aktif hale getirilmesi` - Kod iskeleti var, canlı entegrasyon kapalı.
