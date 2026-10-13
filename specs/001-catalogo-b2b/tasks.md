# Desglose de Tareas de Implementación: Gasbel B2B

**Documento**: `.specify/tasks.md` | **Versión**: 1.0.0 | **Estado**: Completado

---

## Roadmap de Tareas y Dependencias

```mermaid
flowchart TD
    F1[Fase 1: Setup de Proyectos .NET 9] --> F2[Fase 2: Dominio y EF Core 9]
    F2 --> F3[Fase 3: Motor de Precios y Pruebas Unitarias]
    F3 --> F4[Fase 4: Minimal APIs y Endpoints OpenAPI]
    F4 --> F5[Fase 5: Frontend Tailwind y Cotizador Interactivo]
    F5 --> F6[Fase 6: Verificación y Convergencia SDD]
```

---

### Fase 1: Setup de la Solución e Infraestructura Base
- [x] **T001**: Crear solución .NET 9 (`Gasbel.slnx`) y estructurar los proyectos modulares:
  - `src/Gasbel.Domain` (Class Library .NET 9)
  - `src/Gasbel.Application` (Class Library .NET 9)
  - `src/Gasbel.Infrastructure` (Class Library .NET 9)
  - `src/Gasbel.Web` (ASP.NET Core Web / Minimal API .NET 9)
  - `tests/Gasbel.Tests` (xUnit .NET 9)
- [x] **T002**: Instalar paquetes NuGet requeridos:
  - `Npgsql.EntityFrameworkCore.PostgreSQL` (v9.x)
  - `Microsoft.EntityFrameworkCore.Design` (v9.x)
  - `Microsoft.EntityFrameworkCore.Sqlite` (v9.x)
  - `FluentValidation.DependencyInjectionExtensions`
  - `Microsoft.AspNetCore.OpenApi` y `Scalar.AspNetCore`
- [x] **T003**: Configurar `appsettings.json` con cadena de conexión a PostgreSQL y soporte para SQLite en desarrollo local y pruebas automáticas.

---

### Fase 2: Modelo de Dominio, Persistencia y Seeders de Descartables
- [x] **T004**: Crear entidades y enumeraciones en `Gasbel.Domain`:
  - Enums: `PackagingType` (Paquete, Bulto, Fardo, Pallet), `UnitOfMeasure`, `CustomerGroupCode`.
  - Entidades: `Category`, `Product`, `CustomerGroup`, `Customer`, `TierPrice`, `GroupPrice`, `CustomerPrice`, `Quote`, `QuoteItem`.
- [x] **T005**: Configurar `GasbelDbContext` y mappings Fluent API con tipos de datos exactos (`decimal(18,2)`) e índices por SKU y Categoría.
- [x] **T006**: Crear seeder de datos con el catálogo real de las 4 familias de Gasbel:
  - 1: Bolsas (camiseta alta/baja densidad, arranque en rollo, consorcio pesadas, Kraft delivery).
  - 2: Envases térmicos y packaging (bandejas poliespuma EPS, aluminio con tapa, cajas pizza/empanadas, potes térmicos helado, salseros).
  - 3: Vasos, copas y cubiertos (vasos PP/PS, térmicos polipapel café con tapa, cubiertos plásticos reforzados, platos).
  - 4: Papelera comercial e higiene (film stretch virgen, film PVC gastronómico, servilletas bar/dispenser, bobinas Kraft, papel higiénico institucional).
  - Grupos de clientes predeterminados (Minorista, Comercio Gastronómico, Distribuidor / Reventa, Gran Cuenta) con matrices de precios y escalas de bulto cerrado.

---

### Fase 3: Motor de Precios B2B y Suite de Tests Unitarios
- [x] **T007**: Implementar `IPricingService` y `PricingService` con la cascada de resolución estricta:
  1. `TierPrice` (escala por cantidad / bultos)
  2. `CustomerPrice` (acuerdo individual)
  3. `GroupPrice` (grupo del cliente)
  4. `BasePrice` (lista minorista)
- [x] **T008**: Implementar cálculo de ahorro por escala y sugerencia de compra de bulto cerrado.
- [x] **T009**: Desarrollar suite completa de pruebas unitarias en `Gasbel.Tests`:
  - Test de resolución para cada escalón de la cascada.
  - Test de solapamiento entre grupo y volumen.
  - Test de validación de cantidades y redondeos monetarios.

---

### Fase 4: Endpoints Minimal APIs & OpenAPI
- [x] **T010**: Implementar endpoints de catálogo:
  - `GET /api/categories`: Listado de categorías con conteo de productos.
  - `GET /api/products`: Catálogo con filtros por categoría, búsqueda y resolución de precios por contexto de usuario.
  - `GET /api/products/{id}`: Detalle completo con matriz de precios por volumen.
- [x] **T011**: Implementar endpoint de cotización en tiempo real:
  - `POST /api/pricing/calculate`: Recibe producto, cantidad y cliente; retorna precio unitario, subtotal, fuente de precio aplicada y oportunidad de ahorro.
- [x] **T012**: Implementar endpoints de gestión de presupuestos:
  - `POST /api/quotes`: Creación de orden/cotización con desglose de ítems.
  - `GET /api/quotes/{id}`: Consulta y exportación de cotización generada.

---

### Fase 5: Frontend Web Interactivo con Tailwind CSS
- [x] **T013**: Crear interfaz principal de catálogo (`src/Gasbel.Web/wwwroot`):
  - Barra superior con selector de perfil comercial (Minorista, Comercio Gastronómico, Distribuidor, Gran Cuenta).
  - Filtros interactivos por familias de descartables y barra de búsqueda en tiempo real.
- [x] **T014**: Implementar tarjetas de producto interactivas:
  - Selector de unidades (Paquete vs. Bulto cerrado).
  - Recálculo dinámico de precio unitario y subtotal según cantidad ingresada.
  - Badge visual de ahorro cuando se activa una escala de precio mayorista.
- [x] **T015**: Implementar cotizador de pedidos:
  - Carrito / presupuesto lateral con detalle de ítems agregados.
  - Generación de resumen de cotización formal con IVA desglosado.
  - Botón de envío directo con mensaje preformateado para WhatsApp de ventas de Gasbel.

---

### Fase 6: Verificación, Pruebas y Convergencia SDD
- [x] **T016**: Compilación completa de la solución y ejecución de todos los tests automatizados (`dotnet test` pasó con 5/5 superados).
- [x] **T017**: Validación de correspondencia entre el catálogo de descartables del documento y los seeders generados.
- [x] **T018**: Verificación final de convergencia con GitHub Spec Kit.
