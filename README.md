# 🔧 Arıza Takip · Doğal Gaz Arıza Yönetim Sistemi

Doğal gaz dağıtım şirketleri için geliştirilmiş, **harita tabanlı arıza bildirim ve ekip yönetim sistemi**.
Müşteri haritadan konumunu seçerek arıza bildirir, sistem en yakın müsait ekibi otomatik atar, personel arızayı gerçek yol rotasıyla takip eder.

Proje iki parçadan oluşur: **ASP.NET Core MVC web uygulaması** ve **.NET MAUI Android uygulaması**.

> 💼 Çorumgaz'a yazılım mühendisi başvurusu için hazırlanmış bağımsız bir portfolyo projesidir. Şirketle resmî bir bağı yoktur.
>
> ⚠️ Bu bir **demo** çalışmasıdır, resmî bir sistem değildir. Gerçek doğal gaz arızası veya gaz kokusu için **187**'yi arayın.

## 📱 Mobil uygulama (Android)

👉 **[APK'yı indir](https://github.com/furkantr1923/CorumgazArizaTakip/releases/latest)**

Kurulum: APK'yı indir → aç → "Bilinmeyen kaynaklara izin ver" → Yükle
Demo personel girişi: `admin` / `demo123`

- 📍 Haritaya dokunarak ya da GPS ile konum seçme, adresin otomatik dolması
- 🤖 Uzmanlık ve mesafeye göre **otomatik ekip atama**
- 🗺️ Gerçek yol rotası ve rotada **hareket eden ekip aracı**
- 📸 Kamerayla ya da galeriden arıza fotoğrafı
- 🎙️ Açıklamayı **sesle yazma** (Türkçe)
- 🔔 Telefon bildirimi: kayıt alındı, ekip yolda, arıza çözüldü
- 👆 **Parmak izi** ile personel girişi
- ⭐ Çözülen arızaya 1-5 yıldız ve yorum
- 📊 Grafikler: tür dağılımı, son 7 gün, durumlar, ekip performansı
- 🚨 Gaz kaçağında acil mod: siren, titreşim, kırmızı uyarı
- 🌙 Karanlık mod, açılış animasyonu, konfeti ve ses efektleri
- 💾 Veriler telefonda **SQLite** ile saklanır, sunucu gerekmez

## ✨ Web uygulaması özellikleri

**Müşteri tarafı (giriş gerektirmez)**
- 📍 Haritadan konum seçme, "Konumumu Bul" ve adresin otomatik dolması
- 🤖 Mesafe ve iş yüküne göre en uygun ekibin **otomatik atanması**
- 🚨 "Gaz kokusu, kaçak, sızıntı" içeren bildirimlerin otomatik **Acil** işaretlenmesi
- 🎫 Takip kodu ile durum sorgulama (ilerleme adımları ve harita)
- 🔒 KVKK aydınlatma metni ve onay kaydı

**Personel paneli (giriş gerektirir)**
- 📋 Arıza listesi: arama, filtre, sayfalama, CSV dışa aktarma
- 🗺️ Tüm arızaların ve ekiplerin durumunu gösteren harita
- 🚗 Ekipten arızaya **gerçek yol rotası**, km ve tahmini süre
- ⚡ Hızlı aksiyonlar: Yola Çık, Çözüldü, Yeniden Aç, Teknisyen Ata
- 📜 Durum geçmişi (kim, ne zaman, ne değiştirdi)
- 👷 Ekip yönetimi: ekip merkezi, bölge, Müsait / Görevde durumu
- 📈 Grafikler: aylık arıza, durum dağılımı, arıza türleri, ortalama çözüm süresi

**Güvenlik ve kalite**
- 🔐 Hash'lenmiş şifreler, 5 hatalı denemede geçici kilit
- 🛡️ CSRF koruması, over-posting koruması, benzersiz takip kodu
- 🌐 REST API (JSON), korumalı uçlarda 401 dönüşü
- 🧪 7 xUnit birim testi

## 🛠️ Teknolojiler

| Katman | Teknoloji |
|---|---|
| Backend | C#, ASP.NET Core MVC (.NET 10) |
| Mobil | .NET MAUI Blazor Hybrid, Android API'leri |
| Veri | Entity Framework Core, SQLite (web) · sqlite-net (mobil) |
| Ön yüz | Razor, Bootstrap 5, CSS animasyonları |
| Harita | Leaflet, OpenStreetMap, Nominatim, OSRM |
| Grafik | Chart.js |
| Test | xUnit, EF Core InMemory |

## 🌐 API

| Uç nokta | Açıklama | Yetki |
|---|---|---|
| `GET /api/arizalar` | Arıza listesi | 🔐 |
| `GET /api/arizalar/{id}` | Tek arıza detayı | 🔐 |
| `GET /api/arizalar/istatistik` | Özet istatistikler | 🔐 |
| `GET /api/arizalar/sorgu/{kod}` | Takip kodu sorgusu | 🌍 |

## 🚀 Çalıştırma (web)

```bash
git clone https://github.com/furkantr1923/CorumgazArizaTakip.git
cd CorumgazArizaTakip/CorumgazArizaTakip

dotnet user-secrets set "IlkAdmin:KullaniciAdi" "admin"
dotnet user-secrets set "IlkAdmin:Sifre" "KendiGucluSifren123!"

dotnet run --launch-profile https
```

Tarayıcıda `https://localhost:7216` adresini aç. Veritabanı ve örnek ekipler ilk çalıştırmada otomatik oluşur.

**Testleri çalıştırmak için:**

```bash
dotnet test
```

## 📁 Proje yapısı

```
CorumgazArizaTakip/          → ASP.NET Core MVC web uygulaması
CorumgazArizaTakip.Tests/    → xUnit birim testleri
ArizaTakipMobil/             → .NET MAUI Android uygulaması
```

## 👤 Geliştirici

**Furkan Baykal** · Yazılım Mühendisi
GitHub: [@furkantr1923](https://github.com/furkantr1923)
