# OrderS — Backend API
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
6. [Sistem preporuke](#sistem-preporuke)
7. [Baza podataka](#baza-podataka)
8. [Struktura projekta](#struktura-projekta)

---

## 🎯 Opis projekta

OrderS je kompletan informacioni sistem za upravljanje narudžbama u kafiću. Ovaj repozitorij sadrži **.NET 9 backend** koji se sastoji od dva servisa — glavnog API servisa i Worker mikroservisa.

### Ključne funkcionalnosti:
- ✅ REST API sa Clean Architecture i CQRS (MediatR)
- ✅ Automatsko smanjenje inventara nakon narudžbe
- ✅ Nabavka artikala sa Stripe plaćanjem i webhook podrškom
- ✅ Real-time notifikacije putem SignalR
- ✅ Event-driven komunikacija putem RabbitMQ
- ✅ Sistem preporuke proizvoda (Hybrid Recommender)
- ✅ Generisanje računa za goste, kuhinju i šank
- ✅ Worker mikroservis kreira Notification zapise u bazi pri svakoj narudžbi
- ✅ Nabavka podržava cross-store transfer (SourceStoreId) — narudžba iz eksternog skladišta
- ✅ Stores imaju `isExternal` flag za razlikovanje internih i eksternih skladišta
- ✅ InventoryLogType proširen s `Addition` i `Subtraction` tipovima
- ✅ OrdersController zaštićen s `[Authorize]` atributom

### Povezani repozitoriji:
- 📱 **Mobile aplikacija:** [orders_mobile repo]
- 🖥️ **Desktop aplikacija:** [rs2-desktop repo]

---

## 🛠️ Tehnologije

- **.NET 9** — Web API, Clean Architecture, CQRS (MediatR)
- **Entity Framework Core** — ORM, Code First
- **SQL Server 2022** — Baza podataka
- **MassTransit + RabbitMQ** — Event-driven messaging
- **SignalR** — Real-time komunikacija
- **JWT** — Autentifikacija s role-based access control
- **Stripe** — Payment processing + webhook
- **Docker & Docker Compose** — Orkestracija servisa
- **BCrypt** — Hashovanje lozinki
- **FluentValidation** — Validacija DTO-ova
- **AutoMapper** — Object mapping

---

## 🏗️ Mikroservisna arhitektura

Projekat implementira event-driven mikroservisnu arhitekturu s četiri Docker kontejnera:

| Kontejner | Opis | Port |
|---|---|---|
| `orders_api` | Glavni REST API servis | 5220 |
| `orders_worker` | Worker mikroservis — prima OrderCreatedEvent iz RabbitMQ | — |
| `orders_sqlserver` | SQL Server 2022 | 1433 |
| `orders_rabbitmq` | RabbitMQ message broker | 5672 / 15672 |

### Tok poruka:
```
Flutter App → API → RabbitMQ → Worker
```

**Worker servis** (`OrdersAPI.Worker`) je potpuno odvojen projekat s vlastitim Dockerfile-om i kontejnerom. Prima `OrderCreatedEvent` poruke, vrši logiranje i asinhrone zadatke.

---

## 🚀 Pokretanje projekta

### Preduvjeti:
- Docker Desktop

### Koraci:

#### 1. Clone repozitorija
```bash
git clone <URL_OVOG_REPOA>
cd OrdersAPI
```

#### 2. Konfiguracija
Zipovani `.env.zip` fajl se nalazi u root folderu. Ekstraktovati s šifrom `fit`:
```bash
7z x .env.zip
```

#### 3. Pokretanje
```bash
docker-compose up --build
```

Pokretanjem se automatski:
- Kreira SQL Server baza `OrdersDB`
- Seeduju testni podaci (korisnici, proizvodi, stolovi)
- Pokreće RabbitMQ s management UI-om
- Pokreće API i Worker servis

#### 4. Provjera
```bash
docker ps
```
Trebaju biti vidljiva 4 kontejnera: `orders_sqlserver`, `orders_rabbitmq`, `orders_api`, `orders_worker`.

---

## 🔐 Login podaci

| Email | Lozinka | Uloga |
|---|---|---|
| admin@orders.com | password123 | Admin |
| marko@orders.com | password123 | Waiter |
| ana@orders.com | password123 | Bartender |
| kuhar@orders.com | password123 | Kitchen |

---

## 🤖 Sistem preporuke

Implementiran je **hibridni sistem preporuke** koji kombinuje tri algoritma:

1. **Time-Based Filtering** — preporuke prema trenutnom dijelu dana (doručak/ručak/poslijepodne/večer)
2. **Popularity-Based Filtering** — najprodavaniji proizvodi u posljednjih 30 dana
3. **User-Based Collaborative Filtering** — preporuke na osnovu historije sličnih korisnika

### Lokacija implementacije:
- `OrdersAPI/Infrastructure/Services/RecommendationService.cs`
- `OrdersAPI/API/Controllers/RecommendationsController.cs`

### API endpointi:
```
GET /api/Recommendations            # Hibridne personalizirane preporuke (JWT)
GET /api/Recommendations/popular    # Top 10 najpopularnijih (javni)
GET /api/Recommendations/time-based # Preporuke po vremenu (javni)
```

Detaljna dokumentacija algoritma: `recommender-dokumentacija.pdf` (root folder).

---

## 📊 Baza podataka

SQL Server 2022 s 15 tabela:

`Users`, `Orders`, `OrderItems`, `OrderItemAccompaniments`, `Products`, `ProductIngredients`, `Categories`, `CafeTables`, `AccompanimentGroups`, `Accompaniments`, `StoreProducts`, `Stores`, `ProcurementOrders`, `ProcurementOrderItems`, `InventoryLogs`, `Notifications`

Migracije baze podataka nalaze se u `OrdersAPI.Infrastructure/Database/` i automatski se primjenjuju pri pokretanju.

Baza se kreira i seeduje automatski pri prvom pokretanju putem `DbInitializer`.

---

## 📁 Struktura projekta

```
OrdersAPI/
├── OrdersAPI.API/                  # Presentation layer — Controllers, Middleware
│   └── Controllers/
├── OrdersAPI.Application/          # Application layer — DTOs, Interfaces, Validators
│   ├── DTOs/
│   ├── Interfaces/
│   └── Validators/
├── OrdersAPI.Domain/               # Domain layer — Entities, Enums
│   ├── Entities/
│   └── Enums/
├── OrdersAPI.Infrastructure/       # Infrastructure layer — Services, DbContext
│   ├── Data/
│   └── Services/
├── OrdersAPI.Worker/               # Worker mikroservis (odvojen projekat)
│   ├── Consumers/
│   └── Events/
├── docker-compose.yml
├── recommender-dokumentacija.pdf
└── .env.zip                        # Konfiguracijski fajl (šifra: fit)
```

---

## 🔍 Testiranje

**Swagger UI:**
```
http://localhost:5220/swagger
```

**RabbitMQ Management:**
```
http://localhost:15672
Username: guest / Password: guest
```

---

*OrderS — RS2 2024/2025 — Merzuk Šišić — IB220060*