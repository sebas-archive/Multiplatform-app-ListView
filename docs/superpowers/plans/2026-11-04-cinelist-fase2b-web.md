# CineList fase 2B — Versión web (Blazor WebAssembly) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Una versión web de CineList en Blazor WebAssembly que se ve y funciona igual que el APK, sobre la misma Firestore.

**Architecture:** Proyecto nuevo `src/CineList.Web` (Blazor WASM standalone, net9.0) que referencia `CineList.Core` y reutiliza modelo, filtros/orden, validación, póster generado, `DatosFormulario` y `PeliculasRepository` (HttpClient del navegador). Toda la UI es Razor + CSS con los mismos tokens; JS mínimo por interop (reducir póster con `<canvas>`, View Transitions, scroll/parallax).

**Tech Stack:** .NET 9, Blazor WebAssembly, CSS (variables), JS interop mínimo, xUnit (Core).

**Spec:** `docs/superpowers/specs/2026-11-03-cinelist-fase2-design.md` (parte B). Apariencia/animaciones de referencia: parte A del mismo spec y la app MAUI actual (`src/CineList`).

## Global Constraints

- Rama `feat/cinelist`. No cambiar Firestore, el modelo ni el comportamiento de la app MAUI (solo mover helpers puros a Core en Task 1).
- Firestore: `FirebaseConfig` de Core (proyecto `cinelist-7a4de`, colección `movies`).
- Rutas: `/` lista, `/pelicula/{id}` detalle, `/nueva` y `/editar/{id}` formulario.
- Mismas reglas de filtros/orden que el APK (vía Core); filtros guardados en `localStorage` clave `"filtros"` con `CriteriosFiltro.AJson/DesdeJson` (sin texto de búsqueda).
- Tokens de color exactos de `src/CineList/Resources/Styles/Colors.xaml` como variables CSS (`--fondo`, `--superficie`, `--borde-superficie`, `--campo`, `--borde-campo`, `--texto`, `--texto-secundario`, `--acento`, `--acento-texto`, `--vista-texto`, `--vista-fondo`, `--pendiente-texto`, `--pendiente-fondo`), oscuro "Sala oscura" y claro "Claro editorial".
- Tema: primera visita sigue `prefers-color-scheme`; botón sol/luna alterna; elección en `localStorage` clave `"tema"` (`"claro"`/`"oscuro"`).
- Fuentes: copiar las de `src/CineList/Resources/Fonts/` (Inter, Fraunces Bold, Material Symbols subset — si faltan glifos web, regenerar con `tools/generar-subset-iconos.sh` añadiendo constantes; título con Fraunces solo en tema claro).
- Pósters: subida desde archivo → `<canvas>` lado mayor 600 px JPEG 0.70; si no cabe (`PosterPolicy.CabeEnDocumento`) reintento 450 px / 0.50; si no, mensaje "La imagen es demasiado grande".
- Animaciones CSS 150–450 ms; todas desactivadas con `@media (prefers-reduced-motion: reduce)`.
- Textos en español con tildes, sin emojis, íconos de la fuente.
- Datos: se refrescan con botón "Recargar" o recargando la página (sin tiempo real).
- Comandos: `dotnet run --project src/CineList.Web` (puerto fijo en `Properties/launchSettings.json`, p. ej. http://localhost:5180). Verificación visual con el panel del navegador.
- Pruebas reales: como mucho 1 película de prueba creada y borrada al final; las 4 existentes quedan iguales.
- Commits terminan con línea en blanco + `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

## Review Focus

1. **CORS / red en el navegador**: Firestore REST responde desde el navegador; sin red → estado de error con "Reintentar", nunca pantalla en blanco. → verificación Task 2.
2. **Globalización en WASM**: búsqueda sin tildes y orden es-CO funcionan (Core tiene fallback). → verificación Task 2 ("interestelar", "chihiro").
3. **Archivo no imagen / imagen enorme / HEIC** en el formulario → mensaje claro, no se guarda basura. → verificación Task 5.
4. **Ruta directa a `/pelicula/{id}` inexistente o recarga en una ruta interna** → mensaje "Película no encontrada" y volver; `dotnet run` sirve el fallback a `index.html`. → verificación Task 4.
5. **Pantalla angosta (móvil) y ancha (escritorio)**: layout centrado con ancho máx. ~720 px; sin scroll horizontal a 375 px. → verificación en cada task de UI.

---

### Task 1: Proyecto web base, helpers compartidos, tema y tokens

**Files:**
- Create: `src/CineList.Core/Lista/TextoPelicula.cs` (mover `Meta`/`Etiqueta` desde `src/CineList/Helpers/TextoPelicula.cs`; la app MAUI pasa a usar el de Core y se borra el suyo) + tests `tests/CineList.Core.Tests/TextoPeliculaTests.cs`.
- Modify: `src/CineList.Core/Poster/PosterGenerado.cs` → `static IReadOnlyList<(string Inicio, string Fin)> PaletaHex` con los 6 pares de `PosterView.cs`; `PosterView` (MAUI) usa `PaletaHex` (test: 6 pares, hex válidos).
- Create: `src/CineList.Web/` (`dotnet new blazorwasm --empty`), añadido al sln, referencia Core; `wwwroot/css/tokens.css`, `wwwroot/css/app.css`, `wwwroot/fonts/*`, `wwwroot/js/cinelist.js`; `Services/TemaService.cs`, `Services/AlmacenLocal.cs` (localStorage vía interop), `Layout/MainLayout.razor`; `Program.cs` registra `HttpClient` + `IPeliculasRepository`.

**Interfaces (Produces):**
- `CineList.Core.Lista.TextoPelicula.Meta(Pelicula)`, `.Etiqueta(bool)`.
- `PosterGenerado.PaletaHex`.
- Web: `TemaService { bool Oscuro; event Action? Cambio; Task InicializarAsync(); Task AlternarAsync(); }` (pone `data-tema="oscuro|claro"` en `<html>`); `AlmacenLocal { Task<string?> LeerAsync(string clave); Task GuardarAsync(string clave, string valor); }`.
- [ ] Step 1: tests Core (TextoPelicula, PaletaHex) RED→GREEN; MAUI compila (Android build 0 errores) usando los de Core.
- [ ] Step 2: proyecto web, tokens CSS para ambos temas, fuentes, layout centrado (máx. 720 px), página `/` provisional con título "CineList" (Fraunces en claro) y botón de tema; `launchSettings` puerto fijo.
- [ ] Step 3: `dotnet build src/CineList.Web` 0 errores; `dotnet run` + panel del navegador: ambos temas con colores correctos, preferencia recordada al recargar, sin scroll horizontal a 375 px.
- [ ] Step 4: Commit `feat: proyecto web Blazor con tema y tokens compartidos`.

### Task 2: Lista web (búsqueda, chips, grupos/plano, filas, acciones, estados)

**Files:** `src/CineList.Web/Pages/Lista.razor(.css)`, `Components/FilaPelicula.razor`, `Components/PosterImagen.razor`, `Components/Estrellas.razor`, `Components/Dialogo.razor` (confirmación), `Services/EstadoLista.cs` (equivalente del VM: todas, criterios, aplicar, upsert, quitar, cargar/recargar, persistencia de filtros).

**Interfaces:** `EstadoLista` (scoped/singleton) con `IReadOnlyList<Pelicula> Todas`, `CriteriosFiltro Criterios`, `ResultadoFiltro Resultado`, `bool Cargando`, `string? Error`, `event Action? Cambio`, `Task CargarAsync()`, `void AplicarCriterios(CriteriosFiltro)`, `void Upsert(Pelicula)`, `void Quitar(string id)`, `Pelicula? Buscar(string id)`, `Task AlternarVistaAsync(Pelicula)`, `Task EliminarAsync(Pelicula)`. Clona antes de mutar (nunca muta instancias de `Todas`).
- [ ] Step 1: encabezado (título, tema, botón "Recargar"), búsqueda, chips Todas/Vistas/Pendientes, botón "Filtros" con insignia `CuantosActivos` y línea "Activos" (`TextoActivos`) — el panel se conecta en Task 3.
- [ ] Step 2: lista agrupada (encabezado de género en mayúsculas color acento) o plana; fila como la celda del APK (póster 44×64 foto/degradado+iniciales, título, meta, estrellas, etiqueta VISTA/PENDIENTE), menú de tres puntos con "Marcar vista/pendiente" y "Eliminar" (diálogo "¿Eliminar "…"?"); clic en fila → `/pelicula/{id}`; footer `Resumen`; botón flotante "+" → `/nueva`.
- [ ] Step 3: estados: esqueleto shimmer cargando; vacío con "Cargar ejemplos" (`CargarEjemplosAsync`); error con mensaje de `RepositorioException` + "Reintentar"; sin resultados (con filtros de contenido → "Ninguna película coincide…" + "Limpiar filtros" según `TieneFiltrosDeContenido`/`SinFiltrosDeContenido`; si no, "No hay películas que coincidan").
- [ ] Step 4: animaciones: entrada escalonada de filas (40 ms, máx. 10), fade de salida al cambiar chip/orden, giro de etiqueta + ráfaga (puntos acento) al marcar vista desde el menú.
- [ ] Step 5: navegador: las 4 películas reales, búsqueda "interestelar"/"chihiro", chips, marcar vista y revertir, ambos temas, 375 px y escritorio; DevTools offline (o URL inválida temporal NO commiteada) → estado de error y Reintentar.
- [ ] Step 6: Commit `feat: lista web con filtros basicos, grupos y acciones`.

### Task 3: Panel "Filtros y orden" web

**Files:** `Components/PanelFiltros.razor(.css)`.
- [ ] Step 1: hoja inferior igual al APK/mockup (`.superpowers/brainstorm/104252-1793759044/content/filtros.html`): velo, asa, "Filtros y orden" + "Limpiar", "Ordenar por" (5 segmentos), géneros (chips multiselección de `GenerosDisponibles`), década (selección única), calificación mínima (estrellas, "Cualquiera"/"N o más"), botón "Ver N películas" (conteo en vivo con `Contar`, habilitado en 0); borrador aislado hasta "Ver"; Esc y clic en velo cierran sin aplicar; en escritorio se muestra como hoja centrada de ancho máx. 560 px.
- [ ] Step 2: animaciones CSS: velo fade 200 ms, hoja sube con curva tipo resorte (`cubic-bezier(.2,1.4,.4,1)`) ~380 ms, chip pop, número del botón desliza al cambiar.
- [ ] Step 3: navegador: cada orden/filtro, combinación sin resultados, filtros recordados al recargar, ambos temas, 375 px.
- [ ] Step 4: Commit `feat: panel de filtros y orden web`.

### Task 4: Detalle web

**Files:** `Pages/Detalle.razor(.css)`, ampliar `wwwroot/js/cinelist.js`.
- [ ] Step 1: `/pelicula/{id}`: botón atrás, fondo con el póster desenfocado (`filter: blur(24px)`, opacidad 0.35, degradado hacia `--fondo`), póster 220×320, género, título, meta, estrellas, etiqueta, sinopsis, botones Marcar vista/pendiente, Editar (→ `/editar/{id}`), Eliminar (diálogo, vuelve a `/`). Id inexistente → "Película no encontrada" + volver.
- [ ] Step 2: animaciones: transición del póster lista→detalle con View Transitions API (`document.startViewTransition` + `view-transition-name: poster-{id}` en la fila tocada y en el detalle; si el navegador no la soporta, entrada normal); entrada escalonada; estrellas que se llenan una a una (60 ms); parallax al hacer scroll (póster `translateY(min(y*0.5,16px))` + opacidad `1 - min(y/220,1)*0.7`, fondo `y*0.3`); giro de etiqueta + ráfaga hacia arriba desde el borde superior del botón al marcar vista.
- [ ] Step 3: navegador: transición desde distintas filas, recarga directa en `/pelicula/{id}`, id falso, parallax, marcar y revertir, ambos temas, 375 px.
- [ ] Step 4: Commit `feat: detalle web con transicion de poster y parallax`.

### Task 5: Formulario web con subida de póster

**Files:** `Pages/Formulario.razor(.css)`, `wwwroot/js/cinelist.js` (función `reducirImagen(stream/bytes, ladoMax, calidad) → base64`).
- [ ] Step 1: `/nueva` y `/editar/{id}`: título, director, género (select `Generos.Fijos`), año, duración, sinopsis, "Ya la vi", calificación editable, póster (vista previa, "Subir imagen" con `<InputFile accept="image/*">`, "Quitar"); validación `ValidadorPelicula`; cambios detectados con `DatosFormulario`; "Cancelar" con cambios → diálogo "Descartar cambios"; guardar → `CrearAsync`/`ActualizarAsync` → `EstadoLista.Upsert` → volver; error de red → mensaje y conserva datos; doble clic protegido.
- [ ] Step 2: póster: canvas 600 px JPEG 0.70 → reintento 450/0.50 → "La imagen es demasiado grande"; archivo no imagen → "El archivo no es una imagen válida".
- [ ] Step 3: navegador: crear 1 película de prueba con imagen grande (generar una local), verla en lista y detalle (y en el APK si el emulador está disponible), editarla, borrarla; validaciones; archivo .txt; ambos temas.
- [ ] Step 4: Commit `feat: formulario web con subida de poster`.

### Task 6: README y verificación final

- [ ] Step 1: README: sección "Opción C" reescrita → cómo abrir la versión web (`dotnet run --project src/CineList.Web`, URL), qué comparte con el APK, publicación opcional en Firebase Hosting (`dotnet publish src/CineList.Web -c Release` → `firebase deploy` con `public` = `bin/Release/net9.0/publish/wwwroot` y rewrite a `/index.html`; no se ejecuta, solo se documenta).
- [ ] Step 2: `dotnet test tests/CineList.Core.Tests` PASS; build web y Android 0 errores; recorrido completo en el navegador en ambos temas.
- [ ] Step 3: Commit `docs: README con la version web`.
