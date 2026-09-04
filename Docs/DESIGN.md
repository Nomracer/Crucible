# Crucible — Tasarım Dokümanı

**Tür:** 2D falling-sand / hücresel otomat bulmaca
**Platform:** Android (orta segment hedef), portre
**Motor:** Unity 6000.3.10f1, URP 2D Renderer
**Amaç:** Mobil performans mühendisliğini gösteren portfolyo projesi

Karara bağlanan tasarım eksenleri:

| Eksen | Karar |
|---|---|
| Akış | Canlı simülasyon + duraklat / kare kare ilerlet / geri sar |
| Izgara | 288 hücre genişlik, yükseklik cihaz oranından türetilir (9:16'da 288×512) |
| Kaynak | Bölüm başına sınırlı madde bütçesi |
| Görsel | Koyu zemin, doygun madde renkleri, ateş ve lav için ek parlaklık |

---

## 1. Oyun

### Çekirdek fikir

Ekran, piksel piksel simüle edilen bir madde ızgarası. Kum akar ve yığılır, su yayılır ve seviye bulur, ateş yanıcı maddeye sıçrar, lav suya değince taşa döner, kum lavla buluşunca cama dönüşür.

Oyuncu parmağıyla ızgaraya madde döker. Her bölümde sınırlı bir madde bütçesi ve sabit bir hedef vardır.

### Bölüm yapısı

Bir bölüm şunlardan oluşur:

- **Statik geometri** — taş duvarlar, huniler, kanallar. Bölümün "makinesi".
- **Madde bütçesi** — örn. 400 birim su, 200 birim kum, 3 birim lav. Bütçe hücre sayısı cinsinden.
- **Hedef** — işaretli bölgeyi belirli bir maddeden N birimle doldur.
- **Kararlılık koşulu** — hedef 1 saniye (60 tick) boyunca kesintisiz sağlanmalı. Rastgele sıçrayan tek bir pikselle bölüm geçilmez; sistemin dengeye oturması gerekir.

Örnek hedefler:

| Bölüm | Hedef | Gereken keşif |
|---|---|---|
| Kalıp | Kalıp bölgesini camla doldur | Kum + lav = cam |
| Söndür | Tüm ateşi söndür | Su buhara dönüşür, buhar yükselir ve dağılır |
| Filtre | Yağı sudan ayır | Yağ suyun üstünde yüzer |
| Fide | Bitkiyi tavana ulaştır | Bitki nemli toprakta ve suda büyür |
| Oyma | Taş duvarı deleceksin, elinde lav var | Asit taşı çözer, lav çözmez — lavı taşa dönüştürüp merdiven yap |

Bölüm başı hedef süre: 1–3 dakika. Toplam 24 bölüm + serbest mod (sandbox, bütçesiz).

### Akış kontrolü

Simülasyon sürekli çalışır — oyuncu istediği an dökebilir. Üstüne üç kontrol:

- **Duraklat** — sim durur, dökmeye devam edilebilir. Dökülen madde durduğu yerde bekler, PLAY'e basınca hep birlikte düşer. Bu kendi başına bir mekanik: "aynı anda üç yerden dök" ancak duraklatarak kurulabilir.
- **Kare kare ilerlet** — tam bir tick ilerletir. Bir tepkimeyi tam olarak anlamak için, ayrıca geliştirici tarafında hata ayıklama aracı.
- **Geri sar** — son 6 saniye içinde geri gider (bkz. §2.5).

Bu üçü determinist simülasyonun bedava yan ürünü. Ayrıca kare kare ilerletme ve geri sarma, tanılama katmanıyla birlikte projenin "bu sistem gerçekten determinist" iddiasının kanıtı oluyor.

### Madde seti

14 madde. Az sayıda madde, çok sayıda etkileşim.

| Madde | Tip | Davranış |
|---|---|---|
| Boşluk | — | Hava |
| Kum | Toz | Düşer, yığılır, eğimde kayar |
| Toprak | Toz | Kum gibi ama daha az kayar; suyla temas edince ıslanır |
| Kül | Toz | Hafif toz, suda dağılır |
| Su | Sıvı | Düşer, yatay yayılır, seviye bulur |
| Yağ | Sıvı | Suyun üstünde yüzer, yanıcı |
| Asit | Sıvı | Taş ve ahşabı çözer, camı çözmez, kendi de tükenir |
| Lav | Sıvı | Ağır, yanıcıyı tutuşturur, suyla temasta taşa döner |
| Taş | Katı | Statik |
| Cam | Katı | Statik, saydam çizilir, asit geçirmez |
| Ahşap | Katı | Statik, yanıcı, yanınca küle döner |
| Bitki | Katı | Nemli toprakta ve suda büyür, yanıcı |
| Ateş | Gaz | Kısa ömürlü, yanıcıya sıçrar, suyu buharlaştırır |
| Buhar | Gaz | Yükselir, ömrü bitince suya döner |

Etkileşimler tek bir veri tablosunda tanımlanır (`ReactionTable`), koda gömülmez. Yeni madde eklemek = tablo satırı eklemek.

### Kontrol düzeni

- Alt kenarda madde paleti, her maddenin altında kalan bütçe.
- Izgaraya basılı tut ve sürükle: seçili maddeyi dök.
- Fırça yarıçapı: paletin üstünde küçük kaydırıcı (1 / 3 / 6 hücre).
- Üst şerit: duraklat, kare ilerlet, geri sar kaydırıcısı, bölümü sıfırla.

### Görsel dil

Zemin neredeyse siyah (`#0A0A0C`). Maddeler doygun, birbirinden net ayrılan renklerde. Ateş, lav ve közlenmiş ahşap 1.0'ın üstüne çıkan parlaklıkla çizilir; bloom yok, doğrudan parlak renk — mobilde post-processing maliyeti sıfır kalsın diye.

Her piksele `variant` baytından türeyen küçük bir renk sapması verilir. Bu, düz renk yerine granül bir doku yaratır — bedava ayrıntı, ek maliyet yok.

Sanat maliyeti sıfıra yakın: sprite yok, atlas yok, animasyon yok. Tüm oyun alanı tek doku.

---

## 2. Teknik tasarım

### 2.1 Veri düzeni

Izgara: `NativeArray<uint>` — hücre başına 4 bayt, satır-önce (row-major) düzen.

```
bit  0..7   element id      (0..255)
bit  8..15  variant         (renk sapması + madde başına durum, örn. yanma sayacı)
bit 16..23  lifetime        (ateş/buhar ömrü, asit tükenmesi)
bit 24..30  flags           (moved-this-tick, static, burning, wet ...)
bit    31   ayrılmış
```

Tek `uint` = tek okuma, hizalı erişim, Burst dostu. Hücre başına class ya da referans yok, dolayısıyla pointer takibi ve cache ıskası yok.

**Boyut:** genişlik cihaz sınıfına göre sabitlenir, yükseklik ekran oranından türetilir ve 32'ye yuvarlanır (chunk hizası zorunlu).

| Sınıf | Genişlik | 9:16 ekranda | Hücre | Bellek |
|---|---|---|---|---|
| Düşük | 192 | 192 × 352 | 67.584 | 270 KB |
| **Varsayılan** | **288** | **288 × 512** | **147.456** | **590 KB** |
| Yüksek | 384 | 384 × 704 | 270.336 | 1.08 MB |

Uzun ekranlarda (20:9) varsayılan sınıf 288 × 640 = 184.320 hücreye çıkar. Yükseklik ekranla büyür, bu yüzden bütçe en uzun ekrana göre doğrulanır.

### 2.2 Simülasyon

Hücresel otomat, aşağıdan yukarıya tarama, tick başına bir geçiş, 60 Hz.

- **Yön değişimi:** her tick'te yatay tarama yönü ters çevrilir. Aksi halde kum sağa doğru kayma eğilimi (directional bias) gösterir.
- **`moved` biti:** bir hücre tick içinde bir kez hareket eder. Tick sonunda toplu temizlenir.
- **Rastgelelik:** `(tick, cellIndex)` ile tohumlanmış konum-bağımsız hash. Global RNG durumu yok — paralel çalışırken thread sırasından bağımsız, dolayısıyla determinist.

Determinizm bu projede süs değil: geri sarma, kare kare ilerletme ve bölüm çözüm doğrulaması buna dayanıyor.

### 2.3 Chunk'lama ve dirty rect

Izgara **32 × 32'lik chunk**'lara bölünür (288 × 512 için 9 × 16 = 144 chunk).

Her chunk şunları tutar:

- `isActive` — bu tick simüle edilecek mi
- `dirtyRect` — chunk içinde gerçekten değişen minimum dikdörtgen
- `nextDirtyRect` — bir sonraki tick için biriktirilen alan

Bir hücre değişince kendi chunk'ının ve dokunduğu komşu chunk'ın `nextDirtyRect`'i genişletilir. Hiçbir şeyin hareket etmediği chunk uykuya dalar ve hiç dokunulmaz.

Tipik bir bulmaca sahnesinde maddenin çoğu dengeye oturur. Uyanık chunk oranı genelde **%10–25** arasında kalır. Projedeki tek en büyük kazanç bu.

### 2.4 Paralelleştirme

Komşu chunk'lar aynı anda simüle edilirse aynı sınır hücresine iki thread yazar. Çözüm: **dama tahtası (checkerboard) fazlandırma.**

Chunk'lar `(cx & 1, cy & 1)` değerine göre 4 gruba ayrılır. Her tick 4 faz çalışır; bir faz içindeki chunk'lar birbirine komşu değil, dolayısıyla güvenle paralel işlenir.

```
Faz 0: (çift, çift)   Faz 1: (tek, çift)
Faz 2: (çift, tek)    Faz 3: (tek, tek)
```

Her faz bir `IJobParallelFor`, `[BurstCompile]`. Fazlar arasında `JobHandle.Complete()`.

Kilit yok, atomik yok — sadece ayrık (disjoint) yazma bölgeleri.

### 2.5 Anlık görüntü halkası ve geri sarma

Geri sarma için her kareyi saklamak imkânsız: 6 saniye × 60 kare × 590 KB = 212 MB.

Bunun yerine **0.5 saniyede bir anlık görüntü**, 12 yuvalık halka tampon — 6 saniyelik pencere, 12 × 590 KB ≈ **7.1 MB**.

Geri sarma kaydırıcısı bu 12 nokta arasında gezer. Ara kareler gerekirse en yakın anlık görüntüden başlanıp en fazla 29 tick determinist olarak yeniden oynatılır — 29 tick ≈ 1 karelik iş, fark edilmez.

M7'de anlık görüntülere RLE sıkıştırma eklenecek: bir bulmaca sahnesinin çoğu boşluk, uzun tekrar dizileri var. Beklenen kazanç 7.1 MB → ~1 MB. Ölçülüp README'ye yazılacak.

### 2.6 Çizim

Izgara → tek `Texture2D` (RGBA32, point filtering, mipmap yok) → tek quad.

```
NativeArray<uint> grid  →  [Burst job]  →  NativeArray<Color32> pixels
                                              ↓ Texture2D.SetPixelData
                                              ↓ Apply(updateMipmaps: false, makeNoLongerReadable: false)
                                          tek unlit materyal, tek quad
```

Piksel dönüşümü yalnızca dirty chunk'lar için yapılır; tüm doku her kare yeniden yazılmaz.

Hedef: **oyun alanı için 1 draw call.** Toplam sahne (alan + UI) 10 draw call'ın altında.

### 2.7 Bellek ve çöp toplama

Tüm çalışma zamanı belleği `Allocator.Persistent` ile açılışta ayrılır:

- ızgara — çift tampon yok, yerinde güncelleme + `moved` biti (590 KB)
- chunk meta dizisi (144 chunk × 16 bayt ≈ 2.3 KB)
- piksel tamponu (590 KB)
- anlık görüntü halkası (7.1 MB, sıkıştırma sonrası ~1 MB hedef)

Kararlı durumda kare başına hedef: **0 bayt GC ayırma.** UI metinleri önceden ayrılmış `char[]` tamponlarına yazılır (`string` birleştirme yok), sayısal biçimleme elle yapılır.

### 2.8 Kare bütçesi

60 fps hedefi = 16.6 ms. Orta segment Android (Snapdragon 6 serisi sınıfı), 288 × 512 ızgara için hedef dağılım:

| Aşama | Bütçe |
|---|---|
| Simülasyon (4 faz, Burst, paralel) | ≤ 6.0 ms |
| Piksel dönüşümü + doku yükleme | ≤ 2.0 ms |
| Çizim (URP 2D, 1 quad + UI) | ≤ 2.0 ms |
| Oyun mantığı, girdi, UI | ≤ 1.5 ms |
| Pay | ~5 ms |

Kare bütçesi aşılırsa sim tick oranı 60 Hz'den 30 Hz'e düşürülür — görsel olarak kabul edilebilir, determinizm korunur. Izgara çözünürlüğü çalışma zamanında **düşürülmez**; sınıf açılışta cihaza göre bir kez seçilir.

---

## 3. Ölçüm ve gösterim

Bu proje için asıl ürün, ölçülebilir olması.

### Canlı tanılama katmanı

Ekranda açılıp kapanabilen katman:

- kare süresi, sim ms, yükleme ms, çizim ms (`ProfilerMarker` + `ProfilerRecorder`)
- uyanık chunk / toplam chunk
- hareketli hücre sayısı
- kare başına GC ayırma (`GC.Alloc` recorder)
- draw call ve SetPass sayısı

### A/B anahtarları

Tanılama katmanında çalışma zamanında açılıp kapanan anahtarlar:

- `Chunking` — açık / kapalı (tüm ızgarayı her tick tara)
- `Jobs` — çok thread / tek thread
- `Burst` — derlenmiş / yorumlanmış (`BurstCompiler.Options.EnableBurstCompilation`)

Optimizasyonun etkisini cihaz üzerinde canlı gösterir. Portfolyo açısından en değerli parça bu: "3 ms" demek yerine anahtarı kapatıp 3 ms'nin 22 ms'ye çıktığını gösterebiliyorsun.

### Belgelenecek tablo

Her kilometre taşında aynı sahne ve aynı cihazda ölçüm alınır, README'ye şu biçimde yazılır:

| Yapılandırma | Sim ms | Toplam ms | FPS |
|---|---|---|---|
| Naif, tek thread, chunk yok | | | |
| + chunk'lama | | | |
| + Burst | | | |
| + paralel job | | | |

Boş hücreler gerçek ölçümle doldurulur. Tahmini sayı yazılmaz.

Referans sahne sabit olmalı: "yarısına kadar kumla dolu kap, üstten sürekli su akıyor" gibi tekrarlanabilir ve determinist bir kurulum. Ölçüm sahnesi bölüm verisi olarak saklanır.

---

## 4. Proje yapısı

```
Assets/_Project/
  Scripts/
    Core/         Crucible.Core         — bit paketleme, hash, halka tampon, servis erişimi
    Sim/          Crucible.Sim          — ızgara, chunk yöneticisi, madde kuralları, job'lar
    Gameplay/     Crucible.Gameplay     — bölüm yükleme, hedef kontrolü, girdi, fırça, bütçe
    UI/           Crucible.UI           — palet, HUD, akış kontrolleri, bölüm akışı
    Diagnostics/  Crucible.Diagnostics  — sayaçlar, katman, A/B anahtarları
    Editor/       Crucible.Editor       — bölüm editörü, madde tablosu editörü
  Data/           ScriptableObject: madde tanımları, tepkime tablosu, bölümler
  Art/            materyal + shader (toplam iki dosya)
  Scenes/
```

Ayrı assembly definition'lar bilinçli: `Sim` üzerinde çalışırken sadece `Sim` ve ona bağlı olanlar yeniden derlenir. Alan üzerinde tekrar tekrar yineleme yaparken derleme süresi doğrudan iş hızını etkiler.

---

## 5. Yol haritası

| # | Kilometre taşı | Çıktı |
|---|---|---|
| M0 | İskele | Proje, assembly'ler, mobil ayarlar — **tamam** |
| M1 | Izgara + çizim + fırça | Kum düşüyor, parmakla dökülüyor. Tek thread, chunk yok. **Referans ölçüm alınır.** |
| M2 | Madde kuralları | Toz / sıvı / gaz / katı davranışları, tepkime tablosu |
| M3 | Chunk + dirty rect | Ölçüm #2 |
| M4 | Burst + paralel job | Ölçüm #3 |
| M5 | Tam madde seti | 14 madde, tüm tepkimeler |
| M6 | Akış kontrolü | Duraklat, kare ilerlet, anlık görüntü halkası, geri sarma |
| M7 | Bölüm sistemi | Bölüm verisi, bütçe, hedef kontrolü, editör aracı, 8 bölüm |
| M8 | UI + tanılama | Palet, HUD, katman, A/B anahtarları, RLE sıkıştırma ölçümü |
| M9 | Cihaz | Android derlemesi, cihaz üzerinde profilleme, README tabloları |
| M10 | İçerik | 24 bölüm + serbest mod |

M1–M4 teknik omurga; portfolyo değerinin çoğu orada. M5–M10 onu oynanabilir bir şeye dönüştürür.
