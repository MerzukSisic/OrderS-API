# Dokumentacija sistema preporuke — OrderS

## Pregled

OrderS implementira **hibridni sistem preporuke** koji kombinuje tri algoritma za personalizovane preporuke proizvoda korisnicima kafića.

---

## Algoritmi

### 1. Time-Based Filtering (Filtriranje po vremenu)

Preporučuje proizvode na osnovu trenutnog dijela dana:

| Dio dana | Sati | Kategorije |
|---|---|---|
| Doručak | 06:00 – 11:00 | Kafa, čaj, peciva |
| Ručak | 11:00 – 15:00 | Topla hrana, sendviči |
| Poslijepodne | 15:00 – 18:00 | Kafa, kolači, napici |
| Večer | 18:00 – 23:00 | Kokteli, pića, deserti |

**Implementacija:** `RecommendationService.GetTimeBasedRecommendationsAsync()`

### 2. Popularity-Based Filtering (Filtriranje po popularnosti)

Preporučuje najprodavanije proizvode u posljednjih **30 dana**, rangirane po ukupnoj prodanoj količini.

- Uzima u obzir samo `OrderStatus.Completed` narudžbe
- Vraća top N proizvoda (default: 10)
- Javno dostupan endpoint (ne zahtijeva prijavu)

**Implementacija:** `RecommendationService.GetPopularProductsAsync()`

### 3. User-Based Collaborative Filtering (Kolaborativno filtriranje)

Personalizovane preporuke na osnovu historije narudžbi sličnih korisnika:

1. Učitava historiju narudžbi trenutnog korisnika
2. Pronalazi korisnike sa sličnim preferencijama (Jaccard sličnost na skupu naručenih proizvoda)
3. Preporučuje proizvode koje su slični korisnici naručivali, a trenutni korisnik još nije

**Implementacija:** `RecommendationService.GetCollaborativeRecommendationsAsync(Guid userId)`

---

## Hibridni pristup

Finalne preporuke kombinuju sva tri algoritma:

```
HybridScore = (0.4 × CollaborativeScore) + (0.35 × PopularityScore) + (0.25 × TimeScore)
```

Težine su konfigurisane u servisu i mogu se prilagoditi bez izmjene interfejsa.

---

## Objašnjive preporuke

Svaka preporuka sadrži polje `reason` koje korisniku objašnjava zašto se određeni sadržaj preporučuje:

- `"Popular choice — ordered 47 times this month"`
- `"Customers like you also ordered this"`
- `"Perfect for this time of day"`

---

## Korišteni signali

| Signal | Izvor | Korišten u |
|---|---|---|
| Historija narudžbi | `OrderItems` tabela | Collaborative filtering |
| Broj prodanih komada | `OrderItems.Quantity` | Popularity scoring |
| Trenutno vrijeme | `DateTime.UtcNow` | Time-based filtering |
| Status narudžbe | `Order.Status == Completed` | Popularity (samo završene) |

---

## API endpointi

| Endpoint | Auth | Opis |
|---|---|---|
| `GET /api/Recommendations` | JWT required | Hibridne personalizovane preporuke |
| `GET /api/Recommendations/popular` | Javno | Top 10 najpopularnijih proizvoda |
| `GET /api/Recommendations/time-based` | Javno | Preporuke po trenutnom dijelu dana |

---

## Lokacija implementacije

- `OrdersAPI.Infrastructure/Services/RecommendationService.cs`
- `OrdersAPI.Application/Interfaces/IRecommendationService.cs`
- `OrdersAPI.API/Controllers/RecommendationsController.cs`
