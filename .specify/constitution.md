# Gasbel B2B Constitution

## Core Principles

### I. Spec-Driven Development (SDD)
El desarrollo se rige estrictamente por especificaciones formales versionadas bajo GitHub Spec Kit. Cada cambio arquitectónico, regla de negocio o módulo debe derivar de un documento de especificación (`spec.md`), plan técnico (`plan.md`) y desglose de tareas (`tasks.md`). Ningún código entra al sistema sin trazabilidad frente a la especificación.

### II. Arquitectura Limpia y Minimal APIs (.NET 9 / C# 13)
El backend se estructura bajo Clean Architecture / Modular Monolith con proyectos bien definidos:
- `Gasbel.Domain`: Entidades puras, objetos de valor, enumeraciones y lógica de dominio sin dependencias externas.
- `Gasbel.Application`: Casos de uso, contratos, DTOs, validaciones FluentValidation y servicios de aplicación.
- `Gasbel.Infrastructure`: Implementaciones de acceso a datos con EF Core 9, Npgsql, migraciones y seeders.
- `Gasbel.Web`: Minimal APIs de alto rendimiento, OpenAPI/Swagger y presentación interactiva.

### III. Motor de Precios B2B Determinista y Auditable
El cálculo de precios comerciales para descartables debe seguir de forma estricta e inmutable la siguiente cascada de resolución:
1. **TierPrice (Escala por volumen / Bulto cerrado):** Aplica automáticamente si la cantidad adquirida supera los umbrales estipulados (ej. >= 5 bultos, >= 20 bultos o fardo completo).
2. **CustomerPrice (Precio contractual individual):** Precio acordado específicamente para un cliente particular.
3. **GroupPrice (Precio por grupo comercial):** Según el segmento del cliente (Minorista, Comercio Gastronómico, Distribuidor / Reventa, Gran Cuenta).
4. **BasePrice (Precio de lista minorista):** Precio base del producto cuando no aplica ninguna condición preferencial.

Toda la lógica del motor de precios debe estar aislada en servicios de dominio o aplicación y respaldada por pruebas unitarias automatizadas con cobertura del 100% sobre los casos límite de cantidad.

### IV. Persistencia Robusta con PostgreSQL & EF Core 9
- PostgreSQL 16+ como motor relacional primario.
- Uso exclusivo del proveedor `Npgsql.EntityFrameworkCore.PostgreSQL`.
- Nombres normalizados, tipos de datos monetarios exactos (`numeric(18,2)` / `decimal`), índices optimizados para filtrado por categoría y SKU.
- Migraciones reproducibles y seeders iniciales fieles al catálogo real de Gasbel (familias de polietileno, envases térmicos, descartables gastronómicos, papelera e higiene).

### V. Frontend Interactivo con Tailwind CSS y Experiencia B2B Fluida
- HTML5 semántico con Tailwind CSS moderno.
- Componentes interactivos optimizados para compras mayoristas: selector de paquetes vs. bultos cerrados, visualizador instantáneo del ahorro por escala de volumen (tiering) y cotizador dinámico de pedidos en tiempo real.
- Diseño responsivo y rápido sin dependencias pesadas innecesarias.

### VI. Seguridad e Identidad B2B Basada en Claims
- ASP.NET Identity con autenticación basada en cookies seguras.
- Claims esenciales en el contexto del usuario: `CustomerId` y `CustomerGroupId`.
- Rutas públicas para consulta de catálogo base minorista y rutas autenticadas con precios personalizados para clientes mayoristas verificados.

## Estándares Tecnológicos y Convenciones

- **Framework:** .NET 9 (C# 13).
- **ORM:** Entity Framework Core 9 con consultas tipadas y separación de modelos de lectura y escritura si el volumen lo requiere.
- **Validación:** FluentValidation para contratos de entrada.
- **Documentación API:** OpenAPI / Swagger integrado con endpoints descriptivos.
- **Manejo de Errores:** ProblemDetails (RFC 7807) para todas las respuestas de error en la API.
- **Unidades de Venta Específicas de Descartables:** Nomenclatura explícita de `PackagingType` (Unidad, Paquete, Bulto, Fardo, Pallet) y factor de conversión `UnitsPerBox`.

## Gobernanza y Calidad

- Todas las tareas deben completarse de acuerdo a la lista de verificación de `tasks.md`.
- No se tolerará lógica de precios embebida en controladores o vistas.
- Toda regla de negocio debe contar con pruebas unitarias en `Gasbel.Tests` antes de considerarse completa.

**Version**: 1.0.0 | **Ratified**: 2026-10-13 | **Project**: Gasbel-B2B
