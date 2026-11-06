# 🚀 InventoryHub: .NET 7 Web API + MSSQL Stored Procedures

[![.NET](https://img.shields.io/badge/.NET-7.0-blue.svg)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-11.0-purple.svg)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![Database](https://img.shields.io/badge/MSSQL-2022-red.svg)](https://www.microsoft.com/en-us/sql-server)
[![ORM](https://img.shields.io/badge/Micro--ORM-Dapper-orange.svg)](https://github.com/DapperLib/Dapper)
[![Tests](https://img.shields.io/badge/Tests-xUnit%20%7C%20Moq%20%7C%20FluentAssertions-green.svg)](https://github.com/fluentassertions/fluentassertions)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED.svg)](https://www.docker.com/)

A production-grade, enterprise-ready **ASP.NET Core Web API** demonstration showcasing high-performance **Microsoft SQL Server Stored Procedures** orchestrated via **Dapper** and modern C# best practices.

Designed specifically to demonstrate clean architecture, robust transactional integrity, table-valued parameters (TVP), and multi-result set querying.

---

## 🌟 Key Architecture & Technical Highlights

- **Clean / N-Tier Architecture**: Strict separation of concerns between `Core` (Entities, DTOs, Interfaces), `Infrastructure` (Dapper, SQL Clients, Stored Procedures), and `Api` (Controllers, Middlewares, OpenAPI).
- **Stored Procedures with Dapper**:
  - **Table-Valued Parameters (TVP)**: Ingests entire collections in a single round-trip using `dbo.OrderItemTableType`.
  - **Atomic Transactions (`BEGIN TRAN` / `COMMIT` / `ROLLBACK`)**: Guarantees ACID compliance when creating orders and decrementing stock quantities.
  - **Multiple Result Sets**: Executes multi-table queries (e.g., Order Header + Line Items, Dashboard KPI aggregates) via Dapper's `QueryMultipleAsync` in 1 network trip.
  - **Dynamic Pagination**: Server-side pagination with `OFFSET ... ROWS FETCH NEXT` and `OUTPUT` parameters for total record counts.
- **Enterprise Web API Hygiene**:
  - Global Exception Handling Middleware mapping domain exceptions and SQL error codes (e.g. `50001`, `50012`) to standard RFC 7807 responses.
  - Consistent envelope model (`ApiResponse<T>`).
  - Swagger / OpenAPI with custom documentation, available directly at root (`/`).
  - Health check endpoints (`/health`).
- **One-Command Setup**: Includes `docker-compose.yml` that boots MSSQL Server 2022 and automatically initializes schema, stored procedures, and seed data.

---

## 🏗 Solution Structure

```
dotnet-mssql-storedproc-api/
├── database/
│   ├── 01_init_schema.sql         # Tables, indexes, constraints, and Table-Valued Types
│   ├── 02_stored_procedures.sql   # Production-grade SPs (Transactions, TVP, Multi-results)
│   └── 03_seed_data.sql           # Realistic demo dataset
├── src/
│   ├── InventoryHub.Core/         # Domain entities, DTOs, interfaces, custom exceptions
│   ├── InventoryHub.Infrastructure/ # Dapper repositories, DbConnectionFactory, SP bindings
│   └── InventoryHub.Api/          # Controllers, global middleware, Swagger, HealthChecks
├── tests/
│   └── InventoryHub.Tests/        # xUnit unit test suite with Moq and FluentAssertions
├── docker-compose.yml             # Single-command SQL Server 2022 + Auto-migration container
└── README.md
```

---

## 🔌 API Endpoints & Stored Procedure Mapping

| Method | Endpoint | Stored Procedure | Description |
|---|---|---|---|
| `GET` | `/api/v1/products` | `dbo.sp_GetProductsPaged` | Paginated product list with search and category filters. |
| `GET` | `/api/v1/products/{id}` | `dbo.sp_GetProductById` | Fetches single product with joined Category details. |
| `POST` | `/api/v1/products` | `dbo.sp_CreateProduct` | Creates product, checks SKU uniqueness, returns generated ID. |
| `PUT` | `/api/v1/products/{id}` | `dbo.sp_UpdateProduct` | Updates existing product details and updatedAt timestamp. |
| `DELETE` | `/api/v1/products/{id}` | `dbo.sp_DeleteProduct` | Performs soft-delete (`IsActive = 0`). |
| `POST` | `/api/v1/orders` | `dbo.sp_CreateOrderWithItems` | **Transactional Order Creation**: Validates stock, writes order, inserts lines via TVP, deducts stock. |
| `GET` | `/api/v1/orders/{id}` | `dbo.sp_GetOrderById` | Reads Order Header + Items in a single trip using `QueryMultipleAsync`. |
| `GET` | `/api/v1/dashboard/summary` | `dbo.sp_GetDashboardSummary` | Reads KPIs, Top 5 selling products, and Low-stock alerts in 1 trip. |
| `GET` | `/health` | Built-in | Application health status. |

---

## ⚡ Quickstart: Running Locally

### Prerequisites
- [.NET 7.0 SDK](https://dotnet.microsoft.com/download)
- [Docker & Docker Compose](https://www.docker.com/)

### 1. Start MSSQL & Auto-Migrate Database
Run the following command from the root directory to spin up SQL Server and automatically run the schema, stored procedures, and seed data:

```bash
docker compose up -d
```

### 2. Run the Web API
```bash
dotnet run --project src/InventoryHub.Api
```

Once running, navigate to:
- **Swagger UI**: [http://localhost:5000](http://localhost:5000) or [https://localhost:5001](https://localhost:5001)
- **Health Check**: [http://localhost:5000/health](http://localhost:5000/health)

### 3. Run Unit Tests
```bash
dotnet test
```

---

## 💡 Technical Interview Q&A (Why this project is designed this way)

### Q: Why use Stored Procedures instead of EF Core with LINQ?
1. **Performance & Optimization**: Complex queries can utilize exact execution plans, index hints, and table variables tuned by DBAs without relying on ORM-generated SQL.
2. **Network Round-Trip Reduction**: Business operations like creating an order, inserting line items, and updating stock quantities are bundled into a single database execution instead of multiple client-server trips.
3. **Fine-Grained Security**: Grants users/applications `EXECUTE` permissions on specific procedures without granting direct `SELECT`/`INSERT`/`UPDATE` permissions on underlying tables, guarding against data breaches.

### Q: Why Dapper instead of EF Core for Stored Procedures?
- Dapper is a lightweight micro-ORM that offers near-native ADO.NET performance with clean object mapping.
- It provides first-class support for **Table-Valued Parameters (TVP)**, **Output Parameters**, and **Multiple Result Sets** via `QueryMultipleAsync`, which is significantly more straightforward and performant than configuring stored procedure bindings in EF Core.

### Q: How is concurrency and consistency handled during Order Creation?
- `sp_CreateOrderWithItems` uses `SET XACT_ABORT ON` and an explicit `BEGIN TRANSACTION`.
- It performs an atomic stock check: if any requested item has insufficient inventory, it raises error `50012` and rolls back immediately, preventing partial orders or race-condition negative stock.

---

## 👤 Author
- GitHub: [@ekrmcakir](https://github.com/ekrmcakir)

## 📄 License
This project is open-source and available under the [MIT License](LICENSE).

