# Seslendirme (Audio Voiceover) Entegrasyon Planı

Kullanıcının yunus balığı (dolphin) için Habitat, Av-Avcı ve Beslenme modüllerinde seslendirme (voiceover) talebini karşılamak üzere aşağıdaki mimari değişiklikleri ve içerik üretimini gerçekleştireceğiz.

## Önerilen Değişiklikler (Proposed Changes)

Mevcut sistemde sesler `AudioInformationManager` içinde hardcoded (sabit) olarak tutuluyor ve balık türüne göre değişmiyor. "Single Source of Truth" prensibine uygun olarak ses dosyalarını `FishData` üzerinden veya dinamik kaynak yükleme (Resources) ile yöneteceğiz.

### Core Data & Audio Management

#### [MODIFY] [FishData.cs](file:///c:/Users/rumey/Desktop/GuncelKonular/Assets/Scripts/Data/FishData.cs)
- Tek bir `NarrationAudioClip` yerine her modül için ayrı ses alanı eklenecek:
  - `AnatomyAudioClip`
  - `HabitatAudioClip`
  - `FeedingAudioClip`
  - `InterspeciesAudioClip`
  - `PredatorPreyAudioClip`

#### [MODIFY] [AudioInformationManager.cs](file:///c:/Users/rumey/Desktop/GuncelKonular/Assets/Scripts/Core/AudioInformationManager.cs)
- Hardcoded ses klipleri kaldırılacak.
- Modül değiştiğinde (`PlayNarrationForState`), `FishSelectionManager.Instance.CurrentFish` üzerinden aktif balık verisine ulaşılacak.
- Aktif balığın `FishData` objesinde atanmış ses varsa o çalınacak.
- Eğer ses Unity Editor'de manuel atanmamışsa (null ise), otomatik olarak `Resources.Load<AudioClip>($"Audio/{fish.id}/{newType}")` üzerinden dinamik olarak ses dosyası yüklenecek. Bu sayede Editor'ü açıp atama yapmaya gerek kalmadan sadece klasöre ses dosyası koymak yeterli olacak.

### İçerik Üretimi (Audio File Generation)

#### [NEW] Assets/Resources/Audio/dolphin/
- PowerShell TTS (Text-to-Speech) kütüphanesi kullanılarak Yunus balığı için Türkçe seslendirme dosyaları oluşturulacak ve projeye eklenecek:
  - `Habitat.wav`: "Yunuslar denizlerde ve okyanuslarda geniş bir yayılım gösterirler..."
  - `Feeding.wav`: "Yunuslar etoburdur. Temel besinleri balıklar ve kalamarlardır..."
  - `PredatorPrey.wav`: "Yunuslar besin zincirinde üst sıralarda yer alsalar da, büyük köpekbalıkları ve katil balinalar tarafından avlanabilirler."
  - `Anatomy.wav`: "Yunusların pürüzsüz ve aerodinamik bir vücut yapıları vardır..."
  - `InterspeciesRelations.wav`: "Yunuslar oldukça sosyal canlılardır, sürüler halinde yaşarlar..."

## User Review Required

> [!IMPORTANT]
> - Mevcut `AudioInformationManager` inspector'ündeki eski ses atamaları bu değişiklikten sonra geçersiz olacaktır. Tüm sesler dinamik olarak aktif balığa göre `Resources` klasöründen veya `FishData` üzerinden okunacaktır.
> - Yunus balığı dışındaki balıklar için de ses ekleneceği zaman, sadece `Assets/Resources/Audio/<balik_id>/<ModulAdi>.wav` klasörüne ilgili sesleri koymanız yeterli olacaktır.
> - Bu plan sizin için uygun mudur? Onay verdiğinizde kodları güncelleyip yunus ses dosyalarını üreteceğim.

## Verification Plan
1. `FishData.cs` ve `AudioInformationManager.cs` güncellenecek.
2. PowerShell kullanılarak `.wav` ses dosyaları üretilip `Assets/Resources/Audio/dolphin/` altına kaydedilecek.
3. Kullanıcı, Unity'de projeyi başlatıp Yunus'u seçip modüller arasında (Habitat, Beslenme, vb.) geçiş yaptığında ilgili Türkçe seslendirmenin çaldığını doğrulayacak.
