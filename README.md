# 📦 Gasbel B2B — Catálogo Interactivo, Motor de Precios y Cotizador

Plataforma web comercial B2B/B2C desarrollada a medida para **Gasbel**, distribuidora mayorista y minorista de envases descartables, packaging gastronómico, bolsas, papelera comercial e higiene institucional en Mendoza, Argentina.

---

## 🚀 Características Principales

### 🛒 1. Catálogo B2B y Experiencia de Compra
- **Base Real de Artículos (341 productos)** cargados con atributos inteligentes: dimensiones (📏), capacidades en cc/ml (💧), packs de presentación (📦) y proveedor.
- **Visualización Clara de Precios**: Cada artículo exhibe de forma transparente la **Lista 1 (Minorista)** y la **Lista 2 (Mayorista)**.
- **Indicador de Stock Visual**: Badge simplificado (verde = *Hay stock*, gris = *Sin stock*) sin exponer cantidades numéricas, permitiendo cotizar cualquier artículo.
- **Búsqueda Instantánea y Filtros**: Búsqueda reactiva por nombre, medidas o SKU, y selector de categorías con conteo dinámico.

### 💰 2. Motor de Precios, Umbrales Comerciales y Descuentos
- **Descuento Mayorista por Monto Mínimo**: El beneficio de Lista 2 (o porcentaje asignado) se desbloquea automáticamente a partir de un monto mínimo de compra (configurable desde el panel, por defecto **$ 50.000,00 ARS**).
- **Política de Envío Gratis Dinámica**: Umbral configurable (por defecto **$ 100.000,00 ARS**) con leyenda automática *"El envío es gratis a partir de $..."*.
- **Desglose Formal en Comprobante**: Subtotal Lista 1, Descuento aplicado (Mayorista o Especial), Subtotal Neto, IVA (21%) y Total Estimado.

### 🌟 3. Clientes con Descuento Especial y Enlaces Personalizados
- **Gestión de Cuentas Preferenciales**: Grilla administrativa para registrar clientes con porcentajes específicos (ej: 25%, 30%).
- **Enlaces Únicos de Acceso (`?cliente={id}`)**:
  - Generación de link con un solo clic para copiar o compartir directamente por WhatsApp.
- **Experiencia Personalizada para el Cliente**:
  - Al ingresar por su enlace, el cliente es recibido con un **Banner VIP de Bienvenida** con su nombre y porcentaje asignado.
  - En cada tarjeta de producto se visualiza su **Precio Especial Exclusivo** en tiempo real.
  - Al abrir el cotizador, sus datos (nombre, dirección, teléfono) y su descuento se encuentran pre-cargados automáticamente.

### 📱 4. Cotizador Lateral (Drawer) y Modo Pantalla Completa
- **Diseño Ergonómico sin Desbordes**: Cuerpo desplazable unificado con pie fijo (*sticky footer*) que mantiene el total y los botones **"Presupuesto"** y **"WhatsApp"** siempre visibles.
- **Modo Pantalla Completa (Maximizar / Reducir)**: Botón en la esquina superior que expande el cotizador en 2 columnas (lista de artículos a la izquierda; promociones, desglose y datos a la derecha).
- **Formulario de Entrega**: Campo de dirección, switch interactivo de solicitud de entrega a domicilio y autocompletado de clientes.
- **Enrutamiento Inteligente de WhatsApp**: Envío de pedidos formateados con selector de destino entre **Modo Pruebas** (`2613890532`) y **Modo Producción** (`2616799084`).

### ⚙️ 5. Tablero de Administración (ABM)
- **Sub-Tabs de Navegación**:
  1. **Artículos & ABM**: Gestión de catálogo, alta y edición de artículos, subida de fotos al servidor, asistente mágico de extracción de atributos y filtros por estado (Activos, Perfumería, Sin Foto, Sin Medidas, Inactivos).
  2. **Clientes con Descuento Especial**: Grilla CRUD con porcentajes, enlaces de acceso y acciones rápidas.
  3. **Reglas Comerciales & WhatsApp**: Configuración en tiempo real de umbrales mínimos de compra, leyendas y teléfonos de contacto.

---

## 🏗️ Arquitectura Técnica

La solución está construida siguiendo principios de Arquitectura Limpia modular en **.NET 9**:

```
Gasbel-B2B/
├── src/
│   ├── Gasbel.Domain/              # Entidades puras, enums y lógica de dominio
│   │   ├── Entities/               # Product, Category, CommercialSettings, SpecialDiscountClient, Quote...
│   │   └── Enums/                  # PackagingType, UnitOfMeasure, CustomerGroupCode
│   ├── Gasbel.Application/         # Lógica de aplicación, DTOs e interfaces de repositorio
│   │   ├── DTOs/                   # CatalogDtos, QuoteDtos, CommercialDtos
│   │   ├── Interfaces/             # ICatalogRepository, IPricingService
│   │   ├── Pricing/                # PricingService (cascada determinista de precios)
│   │   └── Services/               # CommercialSettingsService (persistencia JSON concurrente)
│   ├── Gasbel.Infrastructure/      # Acceso a datos, persistencia y seeders
│   │   ├── Persistence/            # GasbelDbContext (EF Core 9 con SQLite / PostgreSQL)
│   │   ├── Repositories/           # CatalogRepository
│   │   └── Seeders/                # GasbelCsvImporter (importación de 341 artículos reales)
│   └── Gasbel.Web/                 # Minimal APIs y Frontend Single-Page
│       ├── Endpoints/              # CatalogEndpoints (OpenAPI / Scalar)
│       └── wwwroot/                # Frontend Vanilla JS ES2024 + Tailwind CSS + Lucide
├── tests/
│   └── Gasbel.Tests/               # Suite de 26 pruebas unitarias e integración xUnit
├── data/                           # CSV maestro y configuraciones comerciales JSON
└── specs/                          # Especificaciones y roadmap SDD (Spec-Driven Development)
```

---

## 🛠️ Requisitos e Instalación

### Requisitos Previos
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) instalado.
- Navegador web moderno (Chrome, Edge, Firefox, Safari).

### Ejecución Local

1. **Clonar el repositorio**:
   ```bash
   git clone https://github.com/lucasramirez32/Gasbel-B2B.git
   cd Gasbel-B2B
   ```

2. **Ejecutar la aplicación**:
   ```bash
   dotnet run --project src/Gasbel.Web
   ```

3. **Abrir en el navegador**:
   - Catálogo B2B: [http://localhost:5000](http://localhost:5000) o [https://localhost:5001](https://localhost:5001)
   - Documentación OpenAPI (Scalar): [http://localhost:5000/scalar/v1](http://localhost:5000/scalar/v1)

### Acceso a Administración
- Haz clic en el botón **"Admin"** en la barra superior o ingresa con el parámetro `?admin`.
- Clave de acceso predeterminada: `gasbel2026` o `admin123`.

---

## 🧪 Pruebas Automatizadas

La solución cuenta con una suite completa de pruebas unitarias que validan la cascada de precios, reglas comerciales y persistencia:

```bash
dotnet test
```

**Resultado:** 26/26 pruebas aprobadas con 0 errores.

---

## 📄 Licencia y Créditos

Desarrollado para **Gasbel** © 2026. Todos los derechos reservados.
