# Especificación Funcional: Catálogo B2B y Motor de Cotización Gasbel

**Documento**: `.specify/spec.md` | **Versión**: 1.0.0 | **Estado**: Aprobado para Implementación

---

## 1. Visión General y Objetivos
Desarrollar un catálogo interactivo B2B/B2C para **Gasbel** (distribuidora mayorista y minorista de descartables, embalaje y papelera). La plataforma permite la visualización del catálogo completo de productos con precios diferenciados en tiempo real según el tipo de cliente (consumidor minorista, comercio gastronómico, distribuidor o gran cuenta) y según la cantidad solicitada (paquete individual vs. bulto cerrado vs. escalas por volumen).

---

## 2. Grupos de Clientes (Segmentación Comercial)
El sistema reconoce 4 segmentos con políticas de precio diferenciadas:
1. **Minorista (Público General):**
   - Acceso sin autenticación requerida.
   - Aplica `Product.BasePrice`.
   - Puede acceder a descuentos por volumen si adquiere bultos cerrados (`TierPrice`).
2. **Comercio Gastronómico:**
   - Restaurantes, rotiserías, casas de empanadas, pizzerías, cafeterías.
   - Precios preferenciales de lista por grupo (`GroupPrice`) en packaging de comidas, cajas y bolsas delivery.
3. **Distribuidor / Reventa:**
   - Comerciantes mayoristas y papeleras de reventa local.
   - Precios mayoristas con márgenes optimizados para reventa y umbrales de escala reducidos.
4. **Gran Cuenta:**
   - Franquicias, cadenas gastronómicas e industrias con acuerdos contractuales específicos (`CustomerPrice`).

---

## 3. Familias de Productos del Catálogo Gasbel

### Familia 1: Bolsas de Polietileno, Papel y Residuos
- **Bolsas Camiseta:** Alta y baja densidad, medidas estándar (30x40, 40x50, 45x60, 50x70), paquete x 100 u. y bulto cerrado x 10 o 20 paquetes.
- **Bolsas de Arranque en Rollo:** Medidas 15x20, 20x30, 25x35, 30x40, 40x50. Venta por rollo individual o bulto cerrado.
- **Bolsas de Consorcio y Residuos Pesadas:** 45x55, 60x90, 80x110 (micrones reforzados).
- **Bolsas Kraft para Delivery:** Bolsas de papel con y sin manija para viandas y entregas gastronómicas.

### Familia 2: Envases, Bandejas Térmicas y Packaging Gastronómico
- **Bandejas de Poliespuma (EPS):** Tamaños estándar 102, 103, 105, 107, 108. Paquetes x 100 u. y bultos cerrados.
- **Bandejas Descartables de Aluminio con Tapa:** Medidas para rotisería y congelados, aptas para horno.
- **Cajas Microcorrugadas:** Cajas para pizza (octogonales / cuadradas) y cajas para docena de empanadas.
- **Potes Térmicos y Salseros:** Potes térmicos para helado (1/4 kg, 1/2 kg, 1 kg) y salseros descartables con tapa hermética (1 oz, 2 oz, 4 oz).

### Familia 3: Vasos, Copas, Cubiertos y Artículos de Mesa
- **Vasos Descartables:** Polipropileno (PP) y poliestireno (PS) en 180cc, 220cc, 330cc, 500cc (tragos / cerveza).
- **Vasos Térmicos de Polipapel:** Para café y bebidas calientes con tapa viajera (4 oz, 8 oz, 12 oz).
- **Cubiertos Reforzados:** Tenedores, cuchillos, cucharas y cucharitas plásticas en paquetes x 50 / 100 u. y cajas mayoristas.
- **Platos Descartables:** Platos hondos y llanos para eventos y catering.

### Familia 4: Papelera Comercial, Films de Embalaje e Higiene
- **Film Stretch Virgen:** Rollos para paletizado manual (con o sin mango) y automático.
- **Film PVC Autoadherente Gastronómico:** Bobinas de 300m y 600m (30cm y 38cm) con caja dispensadora y guillotina.
- **Servilletas y Bobinas:** Servilletas bar / dispenser, servilletas tissue y bobinas de papel kraft para embalaje.
- **Higiene Institucional:** Rollos de papel higiénico institucional de 300m/400m y toallas intercaladas en caja cerrada.

---

## 4. Reglas del Modelo de Precios y Cotización

### 4.1. Unidades de Venta y Factor de Conversión
- Cada producto define:
  - `BaseUnit`: Unidad de medida de referencia (Unidad, Paquete, Rollo, Kilo).
  - `PackagingType`: Paquete, Caja / Bulto Cerrado, Fardo, Pallet.
  - `UnitsPerBox`: Número entero que indica cuántas unidades base componen un bulto cerrado (ej. 1 bulto = 10 rollos o 20 paquetes).

### 4.2. Cascada de Resolución de Precios (Determinista)
Para un producto `P`, cliente `C` (perteneciente al grupo `G`) y cantidad de compra `Q`:
```text
IF Existe TierPrice para (P, G o NULL) con MinQuantity <= Q:
    PrecioUnitario = TierPrice.UnitPrice (el de mayor MinQuantity que cumpla)
ELSE IF C está autenticado Y Existe CustomerPrice para (P, C):
    PrecioUnitario = CustomerPrice.UnitPrice
ELSE IF C está autenticado Y Existe GroupPrice para (P, G):
    PrecioUnitario = GroupPrice.UnitPrice
ELSE:
    PrecioUnitario = P.BasePrice
```

### 4.3. Reglas de Descuento por Bulto Cerrado
- Cuando un cliente solicita una cantidad `Q` que equivale a múltiplos de `UnitsPerBox`, el sistema aplica automáticamente la escala `TierPrice` de bulto cerrado.
- Si el usuario selecciona comprar por "Bulto", la cantidad se ajusta automáticamente a múltiplos de `UnitsPerBox`.
- El sistema muestra un indicador proactivo de ahorro:
  - *"Agrega X unidades para completar el bulto cerrado y ahorrar un Y%"*.

---

## 5. Requerimientos de Usuario y Criterios de Aceptación

### Historia 1: Catálogo y Búsqueda Interactiva (Prioridad: P1)
- **Como** cliente (comercial o minorista),
- **Quiero** filtrar productos por familia, buscar por texto/SKU y ver los precios disponibles,
- **Para** encontrar rápidamente los insumos descartables que necesito.
- **Criterios de Aceptación:**
  1. Filtros reactivos por familia (Bolsas, Envases, Vajilla, Embalaje/Papelera).
  2. Búsqueda instantánea por nombre o SKU.
  3. Visualización clara de la unidad mínima de venta y el tamaño del bulto cerrado.

### Historia 2: Motor de Precios Dinámico y Selector de Cantidad (Prioridad: P1)
- **Como** usuario comercial autenticado,
- **Quiero** ver reflejado mi precio de grupo y el descuento automático al aumentar la cantidad o elegir bultos cerrados,
- **Para** evaluar el costo exacto antes de emitir un pedido.
- **Criterios de Aceptación:**
  1. El precio unitario y el subtotal se recalculan en tiempo real según la cascada de 4 niveles.
  2. Al superar el umbral de bulto cerrado, el precio unitario disminuye visiblemente resaltando el porcentaje de ahorro.

### Historia 3: Cotizador de Pedidos B2B (Prioridad: P1)
- **Como** comprador gastronómico o distribuidor,
- **Quiero** armar un carrito / cotización con múltiples ítems y generar un presupuesto formal con desglose,
- **Para** gestionar las compras de mi negocio o solicitar confirmación de stock a Gasbel.
- **Criterios de Aceptación:**
  1. Resumen de pedido con desglose de ítems, cantidades, tipo de empaque (paquetes vs. bultos), precio unitario aplicado, subtotales y total general.
  2. Posibilidad de descargar o imprimir la cotización en formato orden de pedido.
  3. Opción para enviar el pedido vía WhatsApp / Email directamente a ventas de Gasbel.

---

## 6. Requerimientos Comerciales Dinámicos y Panel Administrativo (Reunión Octubre 2026)

### 6.1. Indicadores de Stock y Acción de Compra en Catálogo
- Se elimina la visualización de cantidades numéricas de stock para usuarios finales.
- Se implementa un indicador visual simplificado:
  - **Verde ("Hay stock")**: Si `StockUnits > 0`.
  - **Gris ("Sin stock")**: Si `StockUnits == 0`.
- El cliente puede añadir artículos y cotizar independientemente de la disponibilidad actual.
- El botón de acción de cada tarjeta se estandariza con el texto **"Añadir"**.

### 6.2. Reglas Comerciales Dinámicas y Umbrales Configurables
Desde la pestaña de administración **Reglas Comerciales & WhatsApp**, el administrador puede configurar:
1. **Compra Mínima para Descuento Mayorista (Lista 2)**:
   - Valor inicial: **$ 50.000,00 ARS**.
   - Si el pedido supera este monto, se desbloquean los precios mayoristas o el descuento específico en el comprobante final.
2. **Compra Mínima para Envío Gratis y Leyenda Personalizada**:
   - Valor inicial: **$ 100.000,00 ARS**.
   - Leyenda personalizable (ej. *"El envío es gratis a partir de "*).
3. **Enrutamiento y Modo de Pruebas de WhatsApp**:
   - Número de pruebas: `2613890532`.
   - Número de producción: `2616799084`.
   - Switch de activación de modo de pruebas para alternar el destino de las cotizaciones emitidas.

### 6.3. Clientes con Descuento Especial y Enlaces Personalizados
- **Grilla y ABM Administrativo**: Gestión de clientes con porcentaje preferencial exclusivo (ej: 25%, 30%).
- **Enlaces de Acceso Personalizados (`?cliente={id}`)**:
  - Cada cliente cuenta con un enlace exclusivo generado desde la grilla administrativa.
  - Botón de copiado con un clic y botón de envío directo con mensaje redactado por WhatsApp.
- **Experiencia del Cliente con Enlace**:
  - Detección automática al cargar la plataforma.
  - Banner VIP de bienvenida destacando su beneficio exclusivo.
  - En cada tarjeta de producto se visualiza su precio final con el descuento aplicado.
  - Pre-llenado de datos del cliente (nombre, dirección, teléfono) en el formulario de cotización.

### 6.4. Rediseño del Cotizador Lateral (Drawer) y Modo Pantalla Completa
- Estructura vertical con scroll independiente (`flex-1 min-h-0 overflow-y-auto`) para evitar colapsos visuales.
- **Sticky Footer Fijo**: Barra inferior con total a pagar y botones de **"Presupuesto"** y **"WhatsApp"** siempre visibles.
- **Modo Pantalla Completa**: Botón de maximizar (`maximize-2` / `minimize-2`) que expande el panel a pantalla completa en 2 columnas (artículos a la izquierda, formulario y totales a la derecha).
- Formulario de entrega integrado con dirección, switch de envío a domicilio y notas.

---

## 7. Reglas de Persistencia de Imágenes, Base de Datos y Despliegues Inmutables

### 7.1. Ubicación Persistente de la Base de Datos SQLite
- En entornos Docker y de desarrollo, la base de datos SQLite DEBE alojarse obligatoriamente dentro del volumen persistente `./data` (`Data Source=data/gasbel_b2b.db`, mapeado a `/app/data/gasbel_b2b.db` en el contenedor).
- **Prohibición estricta**: Nunca debe alojarse `gasbel_b2b.db` en la raíz efímera `/app/` del contenedor, para evitar que `docker compose up -d --build` destruya los artículos creados, ediciones manuales o vínculos de imágenes en cada despliegue.

### 7.2. Estándar de Nombrado de Imágenes por SKU y Auto-Vinculación
- Toda imagen de producto subida desde el ABM (`POST /api/admin/products/{id}/image`) o descargada desde URL externa DEBE almacenarse físicamente en el volumen persistente `wwwroot/uploads/products/` utilizando el **SKU sanitizado** como nombre de archivo (ej. `1415.jpg`, `1415.png`, `1415.webp`), en lugar de depender exclusivamente de un `Guid` aleatorio.
- Al subir una nueva imagen para un SKU, el sistema limpia cualquier archivo previo con otra extensión para ese mismo SKU o ID, evitando archivos duplicados o huérfanos.
- **Auto-vinculación en arranque e importación (`SyncProductImagesFromDiskAsync`)**:
  - En cada inicio de la aplicación y tras cada importación de CSV, el sistema escanea `wwwroot/uploads/products/` y vincula automáticamente cualquier imagen física cuyo nombre coincida con el `Sku` (o `Id`) del artículo si su `ImageUrl` está vacío o apunta a un archivo inexistente.

### 7.3. Regla de Sincronización No-Destructiva en Startup vs. Importación Manual CSV
- **En el arranque del contenedor (`Program.cs`)**: El importador CSV se ejecuta con `overwriteExisting: false`. Únicamente inserta SKUs que aún no existan en la base de datos y **jamás pisa** nombres, medidas, precios, estados ni fotos de artículos existentes o creados manualmente en el ABM.
- **En la importación manual desde el ABM (`POST /api/admin/import-csv`)**: Se ejecuta con `overwriteExisting: true`, actualizando precios y stock desde el nuevo archivo CSV, persistiendo una copia en `data/articulos_gasbel.csv`, pero **preservando siempre `ImageUrl` e `IsActive`** y ejecutando a continuación la revinculación automática de imágenes en disco.
- **En el script de despliegue (`deploy.ps1`)**: Antes de recrear el contenedor, garantiza la existencia de los directorios `data/` y `uploads/products/` en el servidor host y rescata cualquier base de datos previa si aún no fue migrada al volumen `./data`.


