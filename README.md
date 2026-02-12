# OrderS - Sistem za upravljanje narudžbama u kafiću
**Autor:** Merzuk Šišić (IB220060)  
**Predmet:** Razvoj softvera II  
**Akademska godina:** 2024/2025

---

## 📋 Sadržaj
1. [Opis projekta](#opis-projekta)
2. [Tehnologije](#tehnologije)
3. [Mikroservisna arhitektura](#mikroservisna-arhitektura)
4. [Pokretanje projekta](#pokretanje-projekta)
5. [Login podaci](#login-podaci)
6. [Build aplikacija](#build-aplikacija)
7. [Sistem preporuke](#sistem-preporuke)

---

## 🎯 Opis projekta
OrderS je kompletan informacioni sistem za upravljanje narudžbama u kafiću koji obuhvata:
- **Mobilnu aplikaciju** (Flutter) za konobare, šankere i administratore
- **Desktop aplikaciju** (Flutter) za administrativne funkcije
- **.NET 9 backend API** sa Clean Architecture
- **Worker servis** za asinhronu obradu narudžbi

### Ključne funkcionalnosti:
- ✅ Kreiranje narudžbi sa automatskim razdvajanjem (kuhinja/šank)
- ✅ Upravljanje proizvodima sa sastojcima i prilozima
- ✅ Automatsko smanjenje inventara nakon narudžbe
- ✅ Nabavka artikala sa Stripe plaćanjem
- ✅ Real-time notifikacije putem SignalR
- ✅ Dashboard sa statistikama i izvještajima
- ✅ Sistem preporuke proizvoda
- ✅ Generisanje računa za goste i interno

---

## 🛠️ Tehnologije

### Backend:
- **.NET 9** - Web API
- **Entity Framework Core** - ORM
- **SQL Server** - Baza podataka
- **MassTransit + RabbitMQ** - Messaging
- **AutoMapper** - Object mapping
- **JWT** - Autentifikacija
- **Stripe** - Payment processing

### Frontend:
- **Flutter 3.19+** - Mobile & Desktop
- **Provider** - State management
- **Dio** - HTTP client
- **shared_preferences** - Local storage

### Infrastructure:
- **Docker & Docker Compose**
- **RabbitMQ** - Message broker
- **SQL Server 2022**

---

## 🏗️ Mikroservisna arhitektura

Projekat implementira **mikroservisnu arhitekturu** sa:

### 1. **Glavni servis (API)** - `orders-api`
- REST API za frontend aplikacije
- Obrada HTTP zahtjeva
- JWT autentifikacija
- Slanje poruka na RabbitMQ

### 2. **Pomoćni servis (Worker)** - `orders-worker`
- **Odvojen kontejner/projekat** ✅
- Prima poruke iz RabbitMQ
- Logira detalje narudžbi
- Izvršava asinhrone zadatke
- Omogućava skaliranje

### Komunikacija:
```
Frontend → API → RabbitMQ → Worker
```

**VAŽNO:** Worker servis je potpuno odvojen projekat (`OrdersAPI.Worker`) sa vlastitim Dockerfile-om i kontejnerom, što zadovoljava zahtjeve za mikroservisnom arhitekturom.

---

## 🚀 Pokretanje projekta

### Preduvjeti:
- Docker Desktop
- Android Studio (za AVD emulator)
- Git

### Koraci:

#### 1. Clone repozitorija
```bash
git clone https://github.com/YOUR_USERNAME/OrderS.git
cd OrderS
```

#### 2. Pokretanje backend servisa
```bash
docker-compose up --build
```

**Ovo pokreće:**
- ✅ SQL Server (port 1433)
- ✅ RabbitMQ (port 5672, Management UI: 15672)
- ✅ OrderS API (port 5220)
- ✅ OrderS Worker (background servis)

**Provjerite da li su servisi pokrenuti:**
```bash
docker ps
```

Trebate vidjeti 4 kontejnera:
- `orders_sqlserver`
- `orders_rabbitmq`
- `orders_api`
- `orders_worker`

#### 3. Pokretanje Desktop aplikacije
```bash
cd OrdersFlutterDesktop
# Ekstraktujte build ako je zipovan (šifra: fit)
unzip fit-build-*.zip

# Pokrenite .exe (Windows)
cd build/windows/x64/runner/Release/
./orders_flutter_desktop.exe
```

#### 4. Pokretanje Mobile aplikacije (Android)
```bash
# Otvorite Android Studio → Device Manager → Start AVD emulator

# Instalirajte APK
cd OrdersFlutterMobile/build/app/outputs/flutter-apk/
adb install app-release.apk

# Ili drag & drop APK fajl u emulator
```

---

## 🔐 Login podaci

### Desktop aplikacija:
```
Username: desktop
Password: test
```

### Mobile aplikacija:

**Admin:**
```
Username: admin
Password: test
Role: Admin
```

**Konobar:**
```
Username: mobile
Password: test
Role: Waiter
```

**Šanker:**
```
Username: bartender
Password: test
Role: Bartender
```

---

## 📦 Build aplikacija

### Android APK (Mobile):
```bash
flutter clean
flutter build apk --release --dart-define=API_BASE_URL=http://10.0.2.2:5220/api
```
**Lokacija:** `build/app/outputs/flutter-apk/app-release.apk`

### Windows EXE (Desktop):
```bash
flutter clean
flutter build windows --release
```
**Lokacija:** `build/windows/x64/runner/Release/`

**ZIP arhiva:** Ako je build folder veći od 100MB, zipovan je sa split opcijom (90MB chunks) i šifrom **"fit"**.

Ekstraktovanje:
```bash
# Windows (7-Zip)
7z x fit-build-2025-02-12.zip

# Linux/Mac
7z x fit-build-2025-02-12.zip
```

---

## 🤖 Sistem preporuke

Projekat implementira **hybrid recommendation system** koji kombinuje:
1. **Collaborative Filtering** - preporuke na osnovu historije korisnika
2. **Content-Based Filtering** - preporuke sličnih proizvoda
3. **Popularity-Based** - najprodavaniji proizvodi

### Lokacija implementacije:
- **Backend:** `OrdersAPI/Infrastructure/Services/RecommendationService.cs`
- **Dokumentacija:** `recommender-dokumentacija.pdf` (root folder)

### Endpoint:
```
GET /api/Recommendations?userId={guid}
```

**Detalji implementacije:**
Pogledajte `recommender-dokumentacija.pdf` za:
- Opis algoritma
- Screenshots koda
- Screenshots iz aplikacije

---

## 📁 Struktura projekta

```
OrderS/
├── OrdersAPI/                    # Glavni API servis
│   ├── Controllers/
│   ├── Application/
│   ├── Domain/
│   ├── Infrastructure/
│   ├── Dockerfile
│   └── appsettings.json
├── OrdersAPI.Worker/             # Worker mikroservis (ODVOJEN!)
│   ├── Consumers/
│   ├── Events/
│   ├── Dockerfile
│   └── appsettings.json
├── OrdersFlutterMobile/          # Flutter mobile app
│   └── build/app/outputs/flutter-apk/app-release.apk
├── OrdersFlutterDesktop/         # Flutter desktop app
│   └── build/windows/x64/runner/Release/
├── docker-compose.yml            # Orchestracija svih servisa
├── recommender-dokumentacija.pdf # Dokumentacija sistema preporuke
└── README.md                     # Ovaj fajl
```

---

## 🎨 UI/UX Features

- ✅ Moderan, konzistentan dark theme dizajn
- ✅ Intuitivna navigacija
- ✅ Real-time order status updates
- ✅ Receipt generation (PDF)
- ✅ Advanced filtering i sorting
- ✅ Responsive layouts
- ✅ Error handling sa jasnim porukama

---

## 📊 Baza podataka

**Tabele (15 poslovnih):**
1. Users
2. Orders
3. OrderItems
4. OrderItemAccompaniments
5. Products
6. ProductIngredients
7. Categories
8. Tables (CafeTable)
9. AccompanimentGroups
10. Accompaniments
11. StoreProducts
12. Stores
13. ProcurementOrders
14. ProcurementOrderItems
15. InventoryLog
16. Notifications

**Referentne tabele nisu uračunate.**

---

## 🔍 Testiranje

### API Endpoints:
Swagger UI dostupan na:
```
http://localhost:5220/swagger
```

### RabbitMQ Management:
```
http://localhost:15672
Username: guest
Password: guest
```

---

## 📝 Napomene

### Konfiguracijski podaci:
- ✅ Svi konfiguracijski podaci su u `appsettings.json` i `.env` fajlovima
- ✅ **NEMA** hardkodiranih stringova u kodu
- ✅ Flutter API adresa konfigurisana putem `Environment.apiBaseUrl`

### Worker servis:
- ✅ Potpuno odvojen projekat (`OrdersAPI.Worker`)
- ✅ Zasebni Dockerfile
- ✅ Vlastiti kontejner u docker-compose
- ✅ Prima poruke iz RabbitMQ
- ✅ Izvršava asinhrone zadatke

### Build fajlovi:
- ✅ Windows: `fit-build-2025-02-12.zip` (split arhiva, šifra: "fit")
- ✅ Android: `app-release.apk`
- ✅ Svi build fajlovi commitovani u repozitorij

---

## 👨‍💻 Autor

**Merzuk Šišić**  
Broj indeksa: IB220060  
Email: merzuk.sisic@edu.fit.ba

---

## 📄 Licenca

Ovaj projekat je kreiran za potrebe kolegija Razvoj softvera II na Fakultetu informacijskih tehnologija (FIT), Univerzitet u Mostaru.

---

## 🙏 Zahvalnice

Zahvaljujem se profesorima i asistentima na FIT-u na podršci i smjernicama tokom razvoja ovog projekta.

---

**Napomena:** Za dodatna pitanja ili probleme, kontaktirajte autora putem email-a ili preko DL sistema.
