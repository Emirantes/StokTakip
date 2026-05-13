# Stok Takip Sistemi

ASP.NET Core MVC kullanılarak geliştirilmiş modern ve kullanıcı bazlı çalışan bir stok takip otomasyonudur.

## Özellikler

- Kullanıcı kayıt ve giriş sistemi
- SHA256 ile şifre hashleme
- Session tabanlı kullanıcı doğrulama
- Kullanıcı bazlı ürün izolasyonu
- Ürün ekleme / düzenleme / silme
- Kategori yönetimi
- Kritik stok takibi
- Dashboard istatistikleri
- İşlem log sistemi
- Responsive yönetim paneli
- SQL Server veritabanı desteği

---

# Kullanılan Teknolojiler

- ASP.NET Core MVC
- Entity Framework Core
- SQL Server
- Bootstrap 5
- jQuery
- LINQ

---

# Ekran Görüntüleri

## Dashboard
- Ürün istatistikleri
- Kritik stok takibi
- Son işlemler paneli

## Ürün Yönetimi
- Ürün listeleme
- Arama sistemi
- Kategori filtreleme
- Stok kontrolü

## Kullanıcı Sistemi
- Giriş / kayıt sistemi
- Profil güncelleme
- Güvenli oturum yönetimi

---

# Kurulum

## 1. Repoyu Klonla

```bash
git clone https://github.com/KULLANICI_ADIN/stok-takip-sistemi.git
```

## 2. Veritabanını Yapılandır

`appsettings.json` dosyası içerisindeki bağlantı ayarını kendi SQL Server adına göre düzenleyin.

Örnek:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=StokTakipDb;
  Trusted_Connection=True;TrustServerCertificate=True;"
}
```

## 3. Migration Çalıştır
```powershell
Add-Migration InitialCreate
Update-Database
```

## 4. Projeyi Başlat
```bash
dotnet run
```

---

# Güvenlik Özellikleri

- SHA256 ile şifre hashleme
- Session tabanlı kullanıcı doğrulama
- Kullanıcı bazlı veri izolasyonu
- Unique email kontrolü
- Unique kullanıcı adı kontrolü
- Anti-forgery token desteği
- Entity Framework Core ile SQL Injection koruması
- Yetkisiz erişime karşı oturum kontrolü
- Güvenli çıkış (Session temizleme)
- Kritik işlemler için kullanıcı doğrulama

---

# Geliştirilebilecek Özellikler

- Admin / Personel rol sistemi
- JWT Authentication sistemi
- Grafik destekli raporlama paneli
- Ürün görsel yükleme sistemi
- Dark mode desteği
- Soft delete sistemi
- Mail doğrulama sistemi
- Şifre sıfırlama sistemi
- Gerçek zamanlı bildirim sistemi
- Excel / PDF dışa aktarma
- Cloud deployment (Azure / AWS)
- Mobil uyumlu gelişmiş arayüz
- Aktivite kayıt sistemi
- Çoklu kullanıcı yetkilendirmesi
- Gelişmiş dashboard analizleri