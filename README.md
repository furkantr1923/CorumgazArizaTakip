# 🔧 Çorumgaz Arıza Takip

Doğal gaz dağıtım şirketleri için geliştirilmiş, **harita tabanlı arıza bildirim ve ekip yönetim sistemi**.
Müşteri haritadan konumunu seçerek arıza bildirir, sistem en yakın müsait ekibi otomatik atar, personel arızayı gerçek yol rotasıyla takip eder.

> ⚠️ Bu proje bir **portfolyo / demo** çalışmasıdır. Çorumgaz'ın resmi sistemi değildir.

## ✨ Özellikler

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
| Veri | Entity Framework Core, SQLite |
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

## 🚀 Çalıştırma

```bash
git clone https://github.com/furkantr1923/CorumgazArizaTakip.git
cd CorumgazArizaTakip/CorumgazArizaTakip

dotnet user-secrets set "IlkAdmin:KullaniciAdi" "admin"
dotnet user-secrets set "IlkAdmin:Sifre" "KendiGucluSifren123!"

dotnet run --launch-profile https
```

Tarayıcıda `https://localhost:7216` adresini aç. Veritabanı ve örnek ekipler ilk çalıştırmada otomatik oluşur.