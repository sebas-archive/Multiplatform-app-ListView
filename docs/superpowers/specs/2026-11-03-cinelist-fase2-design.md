# CineList fase 2 — Diseño

Fecha: 2026-11-03 · Base: rama `feat/cinelist` (fase 1 terminada, ver `2026-11-03-cinelist-design.md`).

Dos subproyectos, en este orden (acordado con el estudiante):

- **A. APK:** filtros avanzados + orden + más animaciones visuales.
- **B. Web:** versión Blazor WebAssembly que se ve y funciona igual que el APK.

A va primero porque la lógica de filtros vive en `CineList.Core` y B la reutiliza.

---

## A. Filtros, orden y animaciones (APK)

### Intención

Los filtros actuales (búsqueda + Todas/Vistas/Pendientes) son "muy básicos". El estudiante quiere filtrar por **género, año y calificación**, **ordenar**, y que la app sea **muy visual**, con más animaciones, sobre todo al tocar una película.

### Decidido por el estudiante (mockup aprobado: `.superpowers/brainstorm/104252-*/content/filtros.html`)

- Botón **"Filtros"** junto a la búsqueda, con insignia del número de filtros activos.
- Panel inferior (bottom sheet) "Filtros y orden" con:
  - **Ordenar por:** Género (actual, por defecto) · Título A–Z · Año · Calificación · Recientes.
  - **Géneros:** chips de selección múltiple. Ninguno marcado = todos.
  - **Década:** Todas · Antes de 1980 · 1980s · 1990s · 2000s · 2010s · 2020s (selección única).
  - **Calificación mínima:** estrellas tocables (0 = sin filtro; "N o más").
  - **"Limpiar"** y botón **"Ver N películas"** con el conteo en vivo.
- Línea "Activos: …" bajo la búsqueda cuando hay filtros.
- Chips Todas/Vistas/Pendientes se mantienen.
- Agrupado por género **solo** con orden "Género"; con cualquier otro orden, lista plana (`IsGroupingEnabled` sigue en el `ListView`; la lista plana usa un único grupo sin encabezado, o se desactiva el agrupado — lo decide el builder).

### Decisiones mías (revisables)

- Los géneros del panel salen de las películas cargadas + la lista fija del formulario, ordenados alfabéticamente.
- Película con año 0 (sin año) no pasa ningún filtro de década salvo "Todas". Calificación 0 no pasa un mínimo ≥ 1.
- Orden "Año" y "Calificación": descendente (más nuevo / mejor primero); empate → título A–Z. "Recientes": `CreadaEn` descendente.
- El estado de filtros y orden se recuerda entre aperturas (`Preferences`).
- Lógica pura en Core: `CriteriosFiltro` (record: texto, estado, géneros, década, calificación mínima, orden) + `FiltroPeliculas.Aplicar(...)` → resultado agrupado o plano + `Resumen`. La web la reutiliza tal cual.
- Si los filtros dejan la lista vacía: estado "Sin resultados" con botón "Limpiar filtros".

### Animaciones nuevas (pedido explícito: "muy visuales")

Duraciones 150–450 ms (la transición del póster puede llegar a 450 ms), `CubicOut`/`SpringOut`; todas se omiten con "reducir movimiento".

1. **Tocar una película → póster que vuela al detalle.** Al tocar la fila, la celda se hunde (0.96) y su póster se "despega": un póster superpuesto viaja desde su posición en la lista hasta su lugar en el detalle, creciendo mientras el resto del detalle aparece escalonado. (MAUI no trae transición de elemento compartido: se simula con un `PosterView` superpuesto que parte del rectángulo del póster tocado — capturado en `ItemTapped` — y se anima a su posición final; la navegación va sin animación propia.)
2. **Detalle con fondo y parallax.** Arriba, un fondo degradado con los colores del póster (o de la paleta del póster generado) que se desvanece hacia el fondo de la app; al hacer scroll el póster se mueve más lento (parallax) y se encoge levemente.
3. **Estrellas que se llenan una a una** en el detalle (60 ms entre estrellas) y la etiqueta VISTA/PENDIENTE hace "pop".
4. **Marcar vista:** la etiqueta cambia con giro/rebote y salen 6–8 puntos de color acento en ráfaga desde el botón.
5. **Panel de filtros:** sube con efecto resorte, fondo oscurece con fade; los chips hacen "pop" al tocarlos; el número de "Ver N películas" cambia con un pequeño deslizamiento.
6. **Aplicar filtros/orden:** las filas salientes se desvanecen y las nuevas entran escalonadas (reutiliza la entrada escalonada existente); la línea "Activos" entra deslizando.
7. **Insignia del botón Filtros:** aparece con escala cuando pasa de 0 a ≥1.

### No-metas A

- Filtro por rango libre de años con slider (se usan décadas).
- Cambios en Firestore o en el modelo.

---

## B. Versión web (Blazor WebAssembly)

### Intención

Mostrarle al profe que CineList "se puede ver en web y funciona igual que el APK".

### Decidido

- Blazor WebAssembly (C#), proyecto nuevo `src/CineList.Web` en la misma solución, referenciando `CineList.Core` (modelo, filtros de la parte A, validación, póster generado, repositorio Firestore).
- Misma Firestore (`cinelist-7a4de`, colección `movies`).
- Mismas pantallas: lista (búsqueda, chips, panel de filtros y orden de la parte A, grupos, estrellas, etiqueta, botón +), detalle (marcar, editar, eliminar), formulario (póster subido desde un archivo del PC, reducido a 600 px / JPEG 70 como en el APK).
- Mismos colores (Sala oscura / Claro editorial), fuentes (Inter, Fraunces, Material Symbols) y botón de modo; animaciones equivalentes en CSS.
- Acciones de fila (marcar/eliminar) en un menú de tres puntos (en web no hay pulsación larga).
- Datos: se actualizan con "Recargar" o al recargar la página (no en vivo).
- Se ejecuta con `dotnet run` y se ve en `localhost`; publicar en Firebase Hosting es opcional y se documenta en el README.

### Decisiones mías

- Reducción del póster en el navegador con `<canvas>` vía JS interop (Blazor WASM no tiene `PlatformImage`); mismos límites de `PosterPolicy`.
- Ruteo: `/` lista, `/pelicula/{id}` detalle, `/nueva` y `/editar/{id}` formulario.
- `HttpClient` estándar del navegador (fetch) para el repositorio; el reintento y los mensajes de error ya están en Core.

### No-metas B

- Login, tiempo real, PWA/offline.

---

## Verificación

- Core: tests xUnit para `CriteriosFiltro`/`Aplicar` (cada filtro, combinaciones, orden con empates, década con año 0, plano vs agrupado, conteo).
- APK: emulador Pixel_8, ambos temas, cada animación revisada con capturas/video corto; "reducir movimiento" probado.
- Web: navegador (panel del navegador de la app), ambos temas, CRUD real con una película de prueba que se borra al final.
