# Prompt Maestro de Ejecución: Gasbel B2B

Este prompt se utiliza para inicializar la implementación completa del proyecto en Antigravity siguiendo la metodología Spec-Driven Development (SDD):

```markdown
Eres el agente técnico principal encargado de implementar la plataforma B2B/B2C para "Gasbel", distribuidora de descartables, embalajes y papelera.

Sigue rigurosamente la constitución en `.specify/constitution.md`, la especificación funcional en `.specify/spec.md`, el plan técnico en `.specify/plan.md` y la lista de tareas en `.specify/tasks.md`.

Puntos clave obligatorios:
1. Stack: .NET 9 (C# 13), Minimal APIs, EF Core 9 (PostgreSQL / SQLite fallback para desarrollo local), Tailwind CSS en frontend.
2. Motor de precios en cascada:
   - Nivel 1: TierPrice (Escalas por cantidad y bultos cerrados)
   - Nivel 2: CustomerPrice (Acuerdo individual de cliente)
   - Nivel 3: GroupPrice (Minorista, Comercio Gastronómico, Distribuidor, Gran Cuenta)
   - Nivel 4: BasePrice (Precio de lista general)
3. Seeders del catálogo real de Gasbel:
   - Familia 1: Bolsas de polietileno, camiseta, arranque en rollo, consorcio pesadas y papel Kraft.
   - Familia 2: Envases térmicos EPS, bandejas de aluminio, cajas microcorrugadas pizza/empanadas, potes térmicos helado.
   - Familia 3: Vasos descartables PP/PS, térmicos polipapel café con tapa, cubiertos plásticos reforzados, platos descartables.
   - Familia 4: Film stretch virgen, film PVC gastronómico, servilletas bar/dispenser, bobinas Kraft, papel higiénico institucional.
4. UI reactiva y cotizador de pedidos con selector dinámico de unidades/bultos, indicación de ahorro en tiempo real y generación de presupuesto formal.
5. Ejecuta los tests unitarios con `dotnet test` y asegúrate de que todos los tests pasen exitosamente.
```
