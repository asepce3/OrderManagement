# OrderManagement API

Web API sederhana untuk manajemen order dengan autentikasi JWT, idempotency key, dan logging ke database.

## Tech Stack

- **.NET 10** (ASP.NET Core Web API)
- **Entity Framework Core 9** dengan **PostgreSQL** (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- **Redis** (`StackExchange.Redis`) untuk idempotency key
- **JWT Bearer** untuk autentikasi & otorisasi role-based
- **Serilog** dengan custom sink ke tabel `LogEntries`
- **Swagger** (Swashbuckle) untuk dokumentasi API

## API List

### Auth

| Method | Endpoint | Auth | Keterangan |
|--------|----------|------|------------|
| POST | `/api/auth/register` | Public | Registrasi user baru |
| POST | `/api/auth/login` | Public | Login dan dapatkan JWT |
| POST | `/api/auth/admin` | Admin | Buat akun admin |

### Orders

| Method | Endpoint | Auth | Keterangan |
|--------|----------|------|------------|
| GET | `/api/orders` | User/Admin | List order |
| GET | `/api/orders/{id}` | User/Admin | Detail order berdasarkan ID |
| POST | `/api/orders` | User | Buat order baru (bulk items). Wajib header `Idempotency-Key` |
| PUT | `/api/orders/status/{id}` | Admin | Update status order |

### Header Create Order

Pada header create order wajib menyertakan `Idempotency-Key` dan jwt token.
```http
POST /api/orders
Idempotency-Key: <uuid>
Authorization: Bearer <jwt-token>
Content-Type: application/json
```

## Penanganan Race Condition

### 1. Create Order (`POST /api/orders`)

- Untuk menangani request berulang digunakan idempotency-key.
- **Idempotency-Key** disimpan di Redis dengan TTL 1 jam. Request berulang dengan key & payload sama akan mengembalikan response yang sama tanpa diproses ulang.
- Jika key sama tapi payload berbeda, akan mengembalikan `409 Conflict`.
- Stok produk dikunci menggunakan **`SELECT ... FOR UPDATE`** (`GetManyByIdForUpdateAsync`) agar concurrent request hanya mengurangi stok dari request yang valid.
- Seluruh proses (kurangi stok, buat order & order detail) berjalan dalam **transaction**.

### 2. Update Status (`PUT /api/orders/status/{id}`)

- Order yang akan diupdate dikunci dengan **`SELECT ... FOR UPDATE`** (`GetByIdForUpdateAsync`).
- Hanya role **Admin** yang boleh mengubah status.
- Transisi status dibatasi oleh:
  - `Pending` → `Confirmed` / `Cancelled`
  - `Confirmed` → `Shipped` / `Cancelled`
  - `Shipped` → `Delivered`
  - `Delivered` dan `Cancelled` adalah terminal state
- Jika status `Cancelled`, stok produk dikembalikan dalam transaction yang sama.

### 3. Register (`POST /api/auth/register`)

- Melakukan pengecekan apakah email sudah terdaftar sebelum insert.
- Menggunakan **unique index** di kolom `Users.Email` sebagai pengaman di database level.
- Proses insert berjalan dalam **transaction** jika terjadi duplikat akan di-rollback dan mengembalikan `409 Conflict`.

## Logging

- `CorrelationId` di ambil dari header `X-Correlation-ID` request HTTP dan akan digenerate otomatis jika tidak ditemukan.
- `CorrelationId` disimpan ke `LogContext` untuk kemudian di simpan ke tabel `LogEntries`.

## Validasi & Error Handling

- Menggunakan pattern `Result<T>` di service layer untuk response sukses/gagal beserta HTTP status code.
- Controller membungkus response dengan `ApiResponse<T>` menjadi format: `{ success, data, message }`.
- Validasi input dilakukan di service.

## Persistensi Data

- **PostgreSQL** sebagai database utama untuk tabel `Users`, `Products`, `Orders`, `OrderDetails`, dan `LogEntries`. Dipilih karena open source dan gratis serta mendukung transaction.
- **Redis** digunakan untuk menyimpan idempotency key sementara (TTL 1 jam).
- **EF Core Migrations** digunakan untuk mengelola skema database.

## Testing

- Terdapat dua project test:
  - `OrderManagement.Tests` — berisi unit test dengan **xUnit** dan **Moq** untuk service layer.
  - `OrderManagement.IntegrationTests` — berisi integration test dengan **xUnit** dan EF Core PostgreSQL.
