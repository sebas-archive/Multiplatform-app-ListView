# CineList fase 2A — Filtros, orden y animaciones — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Añadir al APK un panel de filtros (géneros, década, calificación mínima) y orden, y animaciones muy visuales (póster que vuela al detalle, fondo con parallax, estrellas que se llenan, ráfaga al marcar vista).

**Architecture:** La lógica de filtrado/orden/persistencia va en `CineList.Core` (pura, testeada con xUnit) para que la web (fase 2B) la reutilice. La app MAUI solo agrega UI: botón e insignia, panel inferior, línea de filtros activos, y helpers de animación.

**Tech Stack:** .NET 9, .NET MAUI 9, CommunityToolkit.Mvvm 8.4.0, System.Text.Json, xUnit.

**Spec:** `docs/superpowers/specs/2026-11-03-cinelist-fase2-design.md` (parte A). Diseño base: `docs/superpowers/specs/2026-11-03-cinelist-design.md`. Mockup aprobado: `.superpowers/brainstorm/104252-1793759044/content/filtros.html`.

## Global Constraints

- Rama `feat/cinelist`; no cambiar Firestore ni el modelo `Pelicula`.
- El `ListView` + `PeliculaViewCell` + `OnPeliculaTapped` (evidencias de la Guía 8) se conservan; el README debe seguir apuntando a la línea correcta de `OnPeliculaTapped`.
- Opciones de orden exactas: Género (por defecto) · Título A–Z · Año · Calificación · Recientes.
- Décadas exactas: Todas · Antes de 1980 · 1980s · 1990s · 2000s · 2010s · 2020s (2020s incluye ≥ 2020).
- Año y Calificación descendentes; empate → título A–Z (comparación es-CO sin tildes). Recientes = `CreadaEn` descendente.
- Agrupado por género solo con orden Género; con los demás, lista plana.
- Año 0 solo pasa con década Todas; calificación 0 no pasa un mínimo ≥ 1.
- Filtros y orden se recuerdan entre aperturas (`Preferences`, clave `"filtros"`); el texto de búsqueda no se guarda.
- Animaciones 150–450 ms, `Easing.CubicOut`/`SpringOut`; todas se omiten si `Animaciones.ReducirMovimiento`.
- Colores solo por tokens `AppThemeBinding` existentes (`Colors.xaml`); sin emojis; íconos de la fuente subset (para glifos nuevos: añadir constante a `Iconos.cs` y correr `tools/generar-subset-iconos.sh`).
- Textos de UI en español con tildes.
- Nunca mutar la instancia `Pelicula` de `PeliculasViewModel` fuera de la excepción comentada en `AlternarVistaAsync`.
- Build: `dotnet build src/CineList -f net9.0-android -p:JavaSdkDirectory=/usr/lib/jvm/java-17-openjdk -p:AndroidSdkDirectory=$HOME/Android/Sdk`. Emulador `Pixel_8`; nunca `pm clear`.
- Commits terminan con línea en blanco + `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

## Review Focus

1. **Datos incompletos con filtros** (año 0, calificación 0, género vacío): reglas de arriba; "Sin género" aparece como chip si alguna película no tiene género y lo selecciona. → tests Task 1.
2. **Géneros con mayúsculas/tildes distintas** ("ciencia ficcion" vs "Ciencia ficción"): se tratan como el mismo género en chips y filtro. → tests Task 1.
3. **Preferencia guardada corrupta o de versión vieja** → `CriteriosFiltro.Vacio`, sin crash. → tests Task 1.
4. **Filtros que dejan 0 resultados** → estado "Sin resultados" con "Limpiar filtros"; el contador "Ver 0 películas" sigue habilitado y cierra el panel. → verificación Task 3.
5. **Tocar dos películas muy rápido / volver atrás durante la transición del póster** → una sola navegación, sin póster superpuesto huérfano. → verificación Task 4.

---

### Task 1: Core — criterios, filtro avanzado, orden y persistencia

**Files:**
- Create: `src/CineList.Core/Lista/OrdenPeliculas.cs`, `src/CineList.Core/Lista/Decada.cs`, `src/CineList.Core/Lista/CriteriosFiltro.cs`, `src/CineList.Core/Lista/ResultadoFiltro.cs`, `src/CineList.Core/Lista/Generos.cs`
- Modify: `src/CineList.Core/Lista/FiltroPeliculas.cs` (añadir; no romper `Filtrar/Agrupar/Resumen`)
- Modify: `src/CineList/ViewModels/FormularioViewModel.cs` (usar `Generos.Fijos` en vez de su lista local)
- Test: `tests/CineList.Core.Tests/FiltroAvanzadoTests.cs`, `tests/CineList.Core.Tests/CriteriosFiltroTests.cs`

**Interfaces (Produces):**
- `public enum OrdenPeliculas { Genero, Titulo, Anio, Calificacion, Recientes }` + `public static class OrdenPeliculasTexto { string Etiqueta(OrdenPeliculas o); }` → "Género","Título","Año","Calificación","Recientes".
- `public enum Decada { Todas, Antes1980, D1980s, D1990s, D2000s, D2010s, D2020s }` + `public static class DecadaTexto { string Etiqueta(Decada d); bool Contiene(Decada d, int anio); }` → etiquetas "Todas","Antes de 1980","1980s",…,"2020s".
- `public static class Generos { IReadOnlyList<string> Fijos }` = Acción, Animación, Ciencia ficción, Comedia, Documental, Drama, Fantasía, Romance, Suspenso, Terror.
- `public sealed record CriteriosFiltro(string Texto, FiltroEstado Estado, IReadOnlySet<string> Generos, Decada Decada, int CalificacionMinima, OrdenPeliculas Orden)` con: `static CriteriosFiltro Vacio`; `int CuantosActivos` (cuenta 1 por cada uno de: géneros no vacío, década ≠ Todas, calificación > 0, orden ≠ Género; texto y estado no cuentan); `Equals/GetHashCode` que comparan `Generos` por contenido (SetEquals, sin distinguir mayúsculas/tildes); `string AJson()` (sin `Texto`); `static CriteriosFiltro DesdeJson(string? json)` (null/vacío/corrupto/valores enum fuera de rango → `Vacio`; calificación se recorta a 0–5).
- `public sealed record ResultadoFiltro(IReadOnlyList<GrupoPeliculas> Grupos, bool Agrupado, int Total, string Resumen)` — plano = un único `GrupoPeliculas("", items)`.
- En `FiltroPeliculas`: `ResultadoFiltro Aplicar(IEnumerable<Pelicula> todas, CriteriosFiltro c)`; `int Contar(IEnumerable<Pelicula> todas, CriteriosFiltro c)`; `IReadOnlyList<string> GenerosDisponibles(IEnumerable<Pelicula> todas)` (fijos ∪ géneros de las películas, deduplicados sin mayúsculas/tildes conservando la primera grafía, orden alfabético es-CO, + `SinGenero` al final si alguna película tiene género vacío); `string TextoActivos(CriteriosFiltro c)` → p. ej. `"Animación, Ciencia ficción · 2000s · 4+ estrellas · orden Calificación"` (partes omitidas si inactivas; `""` si nada activo; géneros en orden alfabético).

- [ ] **Step 1: Failing tests** (`FiltroAvanzadoTests`), con un set de 6–8 películas fijas:
  - `Aplicar_Vacio_EquivaleAAgruparActual` (Agrupado=true, mismos grupos que `Agrupar(Filtrar(..))`).
  - `Aplicar_Generos_MultiplesYSinTildes` ("ciencia ficcion" en criterios encuentra "Ciencia ficción").
  - `Aplicar_SinGenero_SeleccionaVacios`.
  - `Aplicar_Decada_Bordes` (1979 → Antes1980; 1980 → D1980s; 2019 → D2010s; 2020 y 2031 → D2020s; año 0 solo con Todas).
  - `Aplicar_CalificacionMinima` (min 4 deja 4 y 5; 0 deja todas; calificación 0 no pasa min 1).
  - `Aplicar_OrdenTitulo_Plano`, `Aplicar_OrdenAnio_DescEmpateTitulo`, `Aplicar_OrdenCalificacion_DescEmpateTitulo`, `Aplicar_OrdenRecientes_PorCreadaEn` (Agrupado=false, un grupo con Genero "").
  - `Aplicar_CombinaTextoEstadoYFiltros`; `Contar_IgualATotal`; `Resumen_SeCalculaSobreFiltradas`.
  - `GenerosDisponibles_UneFijosYDatos_SinDuplicados_SinGeneroAlFinal`.
  - `TextoActivos_Formato` (caso completo exacto de arriba; `Vacio` → `""`; solo orden → `"orden Título"`).
- [ ] **Step 2: Failing tests** (`CriteriosFiltroTests`): `CuantosActivos` (0 para Vacio; 4 con todo activo; texto/estado no cuentan); igualdad por contenido de géneros; `AJson`/`DesdeJson` ida y vuelta (sin texto); `DesdeJson(null|""|"{basura"|"{\"Orden\":99}")` → `Vacio`; calificación 9 → 5.
- [ ] **Step 3:** `dotnet test tests/CineList.Core.Tests` → FAIL (tipos no existen).
- [ ] **Step 4:** Implementar. Reutilizar la normalización y el comparador existentes de `FiltroPeliculas` (no duplicar).
- [ ] **Step 5:** `dotnet test tests/CineList.Core.Tests` → PASS (77 previos + nuevos); build Android 0 errores (FormularioViewModel usa `Generos.Fijos`).
- [ ] **Step 6:** Commit `feat: criterios de filtro, orden y decadas en Core`.

### Task 2: Lista — integración de criterios, botón con insignia, línea "Activos", lista plana

**Files:**
- Modify: `src/CineList/ViewModels/PeliculasViewModel.cs`, `src/CineList/Views/PeliculasPage.xaml(.cs)`
- Create: `src/CineList/Services/FiltrosGuardados.cs`

**Interfaces:**
- Consumes: Task 1 (`CriteriosFiltro`, `FiltroPeliculas.Aplicar/TextoActivos`, `ResultadoFiltro`).
- Produces:
  - `public class FiltrosGuardados { CriteriosFiltro Cargar(); void Guardar(CriteriosFiltro c); }` (singleton; `Preferences` clave `"filtros"`).
  - En `PeliculasViewModel`: `[ObservableProperty] CriteriosFiltro Criterios` (Texto y Estado siguen viniendo de `TextoBusqueda`/`Estado`, que se sincronizan dentro de `Criterios`); `bool Agrupado`; `int FiltrosActivos`; `string TextoActivos`; `bool HayFiltrosActivos`; `IReadOnlyList<Pelicula> Todas` (solo lectura, para el panel); `void AplicarCriterios(CriteriosFiltro nuevos)` (guarda, re-filtra con entrada escalonada); `[RelayCommand] LimpiarFiltros`.
  - `Aplicar` usa `FiltroPeliculas.Aplicar`; las rutas de quitar/actualizar en sitio (sin reconstruir) siguen funcionando en modo plano (un solo grupo).
- [ ] **Step 1:** VM: cargar criterios guardados al construir; `Aplicar` con `ResultadoFiltro`; `SinResultados` cuando `Total == 0` con datos cargados.
- [ ] **Step 2:** Página: botón "Filtros" (ícono `tune` — añadir a `Iconos` y regenerar subset) a la derecha del `SearchBar`, con insignia acento con `FiltrosActivos` (oculta en 0; aparece con `AparecerEscala` al pasar de 0 a ≥1); línea "Activos: …" bajo los chips (entra deslizando 12 px + fade); encabezado de grupo oculto (alto 0) cuando `!Agrupado`; estado "Sin resultados" con botón "Limpiar filtros" cuando hay filtros activos. El botón abre el panel de Task 3 — por ahora llama a un método `AbrirPanelFiltros()` vacío en la página.
- [ ] **Step 3:** Build 0 errores; tests Core pasan.
- [ ] **Step 4:** Emulador: con criterios vacíos la lista es idéntica a fase 1 (agrupada, insignia oculta, sin línea "Activos"); búsqueda, chips, acciones contextuales, eliminar y pull-to-refresh siguen funcionando. (Filtros reales se verifican en Task 3, cuando exista el panel.)
- [ ] **Step 5:** Commit `feat: lista con criterios de filtro, insignia y linea de activos`.

### Task 3: Panel inferior "Filtros y orden"

**Files:**
- Create: `src/CineList/Controls/PanelFiltros.cs` (o `.xaml` + `.xaml.cs`), `src/CineList/ViewModels/PanelFiltrosViewModel.cs`
- Modify: `src/CineList/Views/PeliculasPage.xaml(.cs)` (`AbrirPanelFiltros`), `src/CineList/MauiProgram.cs`

**Interfaces:**
- Consumes: Task 1, Task 2 (`PeliculasViewModel.Todas`, `Criterios`, `AplicarCriterios`).
- Produces: `PanelFiltrosViewModel` con borrador de criterios: `OrdenSeleccionado`, `ObservableCollection<OpcionChip> Generos` (`Texto`, `Seleccionado`), `Decada`, `CalificacionMinima`, `int Conteo` (vía `FiltroPeliculas.Contar` sobre `Todas` con texto/estado actuales), `string TextoBoton` ("Ver 1 película" / "Ver N películas"), comandos `Limpiar`, `Ver` (aplica y cierra), `Cerrar` (descarta borrador).
- [ ] **Step 1:** UI como el mockup: superposición con velo (fade 0→0.55, 200 ms) y hoja que sube desde abajo con `Easing.SpringOut` (~380 ms); asa; título "Filtros y orden" + "Limpiar"; secciones con etiqueta acento en mayúsculas: "Ordenar por" (control segmentado de 5), "Géneros" (chips multiselección, con "pop" 1→1.15→1 al tocar), "Década" (chips selección única), "Calificación mínima" (`EstrellasView` editable + texto "N o más" / "Cualquiera"); botón primario "Ver N películas" cuyo número cambia deslizando (salida hacia arriba, entrada desde abajo, 160 ms). Tocar el velo o el botón atrás de Android cierra sin aplicar. Altura máx. ~85 % con scroll interno.
- [ ] **Step 2:** Build 0 errores.
- [ ] **Step 3:** Emulador (4 películas reales): cada filtro y orden cambia la lista como se espera (Año/Calificación planos y ordenados; Género agrupado); insignia y línea "Activos" correctas; "Limpiar" restablece; combinación sin resultados → "Ver 0 películas" → estado "Sin resultados" → "Limpiar filtros" restablece; cerrar y reabrir la app conserva filtros y orden; botón atrás cierra el panel; ambos temas; capturas.
- [ ] **Step 4:** Commit `feat: panel de filtros y orden`.

### Task 4: Póster que vuela al detalle + fondo con parallax + estrellas secuenciales

**Files:**
- Create: `src/CineList/Services/TransicionPoster.cs`
- Modify: `src/CineList/Views/PeliculasPage.xaml.cs` (`OnPeliculaTapped`: capturar rectángulo del póster de la celda tocada y navegar con `animate: false`), `src/CineList/Views/Celdas/PeliculaViewCell.cs` (exponer el `PosterView` o su rect en ventana), `src/CineList/Views/DetallePage.xaml(.cs)`, `src/CineList/Controls/EstrellasView.cs`, `src/CineList/Helpers/Animaciones.cs`

**Interfaces:**
- Produces:
  - `public static class TransicionPoster { void Preparar(Pelicula p, Rect origenEnVentana); bool Tomar(string id, out Rect origen); }` (un solo uso; `Tomar` limpia; caduca a 2 s).
  - `EstrellasView.AnimarLlenado(uint retrasoMs = 60)`: estrellas de 0 a escala 1 una por una con pop.
  - `Animaciones.VolarPoster(VisualElement superpuesto, Rect origen, Rect destino, uint ms = 420)`.
- [ ] **Step 1:** Lista: en `OnPeliculaTapped` (mantener el comentario Guía 8 y que siga siendo corto) pulso de la celda (0.96) + `TransicionPoster.Preparar` con el rect del póster en coordenadas de ventana, luego `GoToAsync($"detalle?id=…", animate: false)`. Guardas actuales contra doble toque se mantienen.
- [ ] **Step 2:** Detalle: capa superior (`Grid` raíz con overlay) con un `PosterView` superpuesto. En la primera aparición, si `TransicionPoster.Tomar` da un origen: ocultar el póster real, colocar el superpuesto en el origen (convertido a coordenadas de la página), animar traslación+escala hasta el rect real (420 ms CubicOut) mientras el resto entra escalonado; al terminar mostrar el real y ocultar el superpuesto. Sin origen (o reducir movimiento) → entrada actual. Si la página se cierra a mitad, el superpuesto se oculta (sin huérfanos).
- [ ] **Step 3:** Fondo: arriba del detalle, la imagen del póster (o el degradado del póster generado) a todo el ancho, ~360 px, opacidad 0.35, con degradado hacia el color `Fondo` del tema; desenfoque con `RenderEffect` en Android 12+ (si no, sin desenfoque). Parallax: en `ScrollView.Scrolled`, póster `TranslationY = y*0.5` y escala `1 - min(y/1200, 0.08)`, fondo `TranslationY = y*0.3`; overscroll no rompe nada.
- [ ] **Step 4:** Estrellas del detalle con `AnimarLlenado` tras la entrada; etiqueta VISTA/PENDIENTE con pop.
- [ ] **Step 5:** Build 0 errores; emulador: transición fluida desde filas arriba/abajo/scrolleadas, en ambos temas, con póster foto y generado; doble toque rápido → una navegación; atrás inmediato → sin póster huérfano en la lista ni en el detalle; parallax al hacer scroll; reducir movimiento (`adb shell settings put global animator_duration_scale 0`, restaurar a 1 después) → sin animaciones y todo visible. Capturas (y, si es posible, `adb shell screenrecord --time-limit 5` del vuelo).
- [ ] **Step 6:** Commit `feat: poster que vuela al detalle, fondo con parallax y estrellas animadas`.

### Task 5: Ráfaga y giro al marcar vista/pendiente

**Files:**
- Create: `src/CineList/Controls/Rafaga.cs`
- Modify: `src/CineList/Views/DetallePage.xaml.cs`, `src/CineList/Views/Celdas/PeliculaViewCell.cs`, `src/CineList/Helpers/Animaciones.cs`

**Interfaces:**
- Produces: `public static class Rafaga { Task Lanzar(Layout contenedor, Point centro, Color color, int puntos = 8); }` — círculos de 6–8 px que salen radialmente 40–60 px con fade y escala a 0 en ~420 ms y se eliminan del contenedor; `Animaciones.GirarEtiqueta(VisualElement etiqueta, Action cambiarTexto)` — RotationY 0→90 (150 ms), `cambiarTexto()`, 90→0 (150 ms) con leve rebote.
- [ ] **Step 1:** Detalle: al marcar vista/pendiente con éxito → `GirarEtiqueta` + `Rafaga` desde el centro del botón con color `Acento` (solo al pasar a VISTA; a PENDIENTE solo giro).
- [ ] **Step 2:** Lista: tras la acción contextual con éxito, la etiqueta de la celda visible gira; ráfaga pequeña (6 puntos) desde la etiqueta al pasar a VISTA.
- [ ] **Step 3:** Build 0 errores; emulador ambos temas; varias veces seguidas sin acumular vistas (verificar que los puntos se eliminan); reducir movimiento → solo cambio de texto. Revertir los estados cambiados.
- [ ] **Step 4:** Commit `feat: rafaga y giro al marcar vista`.

### Task 6: README y APK

**Files:**
- Modify: `README.md` (sección Funciones: filtros por género/década/calificación, orden, animaciones nuevas; verificar enlace `#L` de `OnPeliculaTapped`).

- [ ] **Step 1:** Actualizar README; `grep -n OnPeliculaTapped src/CineList/Views/PeliculasPage.xaml.cs` y corregir el enlace.
- [ ] **Step 2:** `dotnet test tests/CineList.Core.Tests` → PASS.
- [ ] **Step 3:** Publicar Release (comando del README), comprobar timestamp, copiar `-Signed.apk` a `CineList.apk` (no versionado); instalar desde cero en `Pixel_8` y recorrer: filtros, orden, vuelo del póster, parallax, ráfaga, ambos temas. Sin crear/borrar datos reales.
- [ ] **Step 4:** Commit `docs: README con filtros y animaciones`.
