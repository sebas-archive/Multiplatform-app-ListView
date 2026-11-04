# CineList — Diseño

Fecha: 2026-11-03 · Autor: Sebastián Revelo (con Claude)

## Intención

App móvil cross platform para la **Guía 8 — Trabajo con ListView** (Unidad 1, Interfaz del Usuario). La guía exige:

1. Una vista con un `ListView`.
2. Una clase **Model** nueva con los datos de la lista.
3. Una clase de tipo **`ViewCell`** que personaliza cada fila.
4. Un **evento de selección** de registro.

Evidencias: capturas del ListView funcionando, la clase Model, la clase ViewCell y el código del evento. Además: exposición en inglés + video.

El estudiante quiere algo **más completo** que el mínimo, con diseño cuidado (no "básico"), para que luzca en el video. Tema elegido: **películas** (lista personal de películas vistas / pendientes).

## Metas

- Cumplir los 4 requisitos de la guía **literalmente** (nombres `ListView`, `ViewCell` visibles en el código).
- CRUD completo de películas contra **Firebase Firestore**.
- Diseño pulido con **modo oscuro y modo claro** conmutables.
- Código ordenado y fácil de mostrar en capturas.

## No-metas

- Login / usuarios.
- Firebase Storage (exige plan Blaze).
- Compilar iOS / Windows (desde Linux solo Android; el código queda multiplataforma).
- Versión web: **más adelante**, en React, sobre la misma Firestore. No ahora, pero el diseño la facilita (ver "Preparado para web").

## Anti-metas (sería un fracaso aunque funcione)

- Usar `CollectionView` en vez de `ListView`, o un `DataTemplate` anónimo en vez de una clase `ViewCell` propia: rompe la evidencia de la guía.
- Diseño plano tipo wireframe.
- Emojis como íconos; pósters que no correspondan a la película.

## Decisiones tomadas (por el estudiante)

| Tema | Decisión |
|---|---|
| Tecnología | .NET MAUI 9 (C# + XAML) |
| Datos | Firebase Firestore, proyecto nuevo `cinelist-7a4de`, colección `movies` |
| Tema de la app | Películas |
| Pósters | Se suben fotos (cámara o galería); guardadas comprimidas en base64 dentro del documento de Firestore |
| Extras de la lista | Todos: búsqueda, chips Todas/Vistas/Pendientes, agrupar por género, menú contextual para borrar, pull-to-refresh, estrellas 1–5 |
| Estilo | Oscuro = "Sala oscura" (negro + dorado). Claro = "Claro editorial" (crema + terracota, títulos con serifa). Botón para cambiar de modo |
| Flujo | Lista → Detalle (tap) → Agregar/Editar |

## Decisiones del builder (Claude), revisables

- **Nombre** provisional: CineList. Paquete Android `com.cinelist.app`.
- **Modo de tema**: primera vez sigue el sistema; luego recuerda la elección (`Preferences`).
- **Firestore vía REST** con `HttpClient` (mismo patrón que TiendaApp), sin SDK nativo. Campos en inglés para que la web en React los lea igual.
- **Reglas Firestore**: lectura/escritura abierta solo en `movies/{id}` (ya publicadas). Aceptable para proyecto de clase; documentado como riesgo.
- **Datos semilla**: si la colección está vacía, botón "Cargar ejemplos" crea ~6 películas **sin póster**; se muestra un póster generado (degradado + iniciales del título) para no usar imágenes que no correspondan.
- **Arquitectura**: MVVM ligero con `CommunityToolkit.Mvvm` para ViewModels; el **evento `ItemTapped` va en el code-behind** de la página de lista (así la captura del "código del evento" es directa y clara).
- **Íconos**: vectoriales (fuente de íconos Material Symbols o SVG en `Resources/Images`).

## Modelo de datos

Clase `Models/Pelicula.cs` (el Model de la guía):

| Propiedad C# | Campo Firestore | Tipo | Notas |
|---|---|---|---|
| Id | (nombre del doc) | string | |
| Titulo | `title` | string | obligatorio |
| Director | `director` | string | |
| Genero | `genre` | string | p. ej. Ciencia ficción, Animación, Drama, Comedia, Terror, Acción |
| Anio | `year` | int | |
| DuracionMin | `durationMin` | int | |
| Sinopsis | `synopsis` | string | |
| Vista | `watched` | bool | Vista / Pendiente |
| Calificacion | `rating` | int | 0 = sin calificar, 1–5 |
| PosterBase64 | `poster` | string? | JPEG base64, objetivo ≤ 300 KB, máx. 900 KB |
| CreadaEn | `createdAt` | timestamp | |

Firestore limita cada documento a 1 MB → el póster se redimensiona (lado mayor ~600 px, JPEG calidad ~70) antes de guardar. Si aun así supera 900 KB, se reduce otra vez o se rechaza con mensaje.

## Pantallas

### 1. Lista (`PeliculasPage`) — núcleo de la guía

- `ListView` con:
  - `ItemTemplate` = **`PeliculaViewCell`** (clase C# que hereda de `ViewCell`): póster 44×64 redondeado, título, "Director · Año · Duración", estrellas, etiqueta VISTA / PENDIENTE.
  - `ContextActions` en la celda: **Marcar vista/pendiente** y **Eliminar** (con confirmación).
  - `IsGroupingEnabled` con encabezado por género (`GroupHeaderTemplate`).
  - `IsPullToRefreshEnabled` → recarga desde Firestore.
  - `Footer`: "N películas · M vistas".
  - `CachingStrategy="RecycleElement"`. (Actualización fase 2A: ahora se usa `RetainElement`; `RecycleElement` perdía la última fila en las reconstrucciones de grupos.)
  - Evento **`ItemTapped`** → navega al detalle y limpia la selección.
- Arriba: título grande, botón de modo claro/oscuro, `SearchBar` (título o director), chips Todas / Vistas / Pendientes.
- Botón flotante **+** → formulario nuevo.
- Estados: cargando, vacío (con botón "Cargar ejemplos"), error de red (con reintentar).

### 2. Detalle (`DetallePage`)

Póster grande, título, metadatos, estrellas, sinopsis. Botones: Marcar vista/pendiente, Editar, Eliminar.

### 3. Formulario (`FormularioPage`)

Campos: título, director, género (Picker), año, duración, sinopsis, vista (switch), calificación (estrellas tocables). Póster: botones **Tomar foto** / **Elegir de galería** (`MediaPicker`), vista previa. Validación: título obligatorio; año y duración numéricos. Guardar → Firestore → vuelve a la lista actualizada.

## Diseño visual

Colores definidos una sola vez en `Resources/Styles/Colors.xaml` y usados con `AppThemeBinding`.

| Token | Oscuro (Sala oscura) | Claro (Claro editorial) |
|---|---|---|
| Fondo | `#0F1115` | `#F7F5F0` |
| Superficie / celda | `#181B21` | `#FFFFFF` (borde `#ECE7DE`) |
| Campo búsqueda / chip | `#1C1F26` | `#FFFFFF` (borde `#E6E1D8`) |
| Texto | `#F2F2F2` | `#1D1B18` |
| Texto secundario | `#8B929C` | `#7A7368` |
| Acento (chips activos, estrellas, botón +, grupo) | `#F5C518` | `#B4532A` |
| Vista (etiqueta) | `#4CD964` sobre verde 15 % | `#2F6B2A` sobre `#E5EFE3` |
| Pendiente (etiqueta) | `#FFB340` sobre naranja 15 % | `#A1531F` sobre `#F6E7D8` |

Tipografía: títulos con serifa en modo claro (fuente embebida, p. ej. "Fraunces" o "Playfair Display"); sans en el resto.

### Animaciones y transiciones (pedido explícito del estudiante)

Duraciones cortas (150–350 ms), easing `CubicOut`, nada que bloquee el uso.

- **Entrada de la lista**: celdas aparecen escalonadas (fade + desliza 16 px hacia arriba, 40 ms entre filas) al cargar y al cambiar filtro.
- **Tap en celda**: la celda se encoge a 0.97 y vuelve antes de navegar.
- **Lista → Detalle**: el detalle entra con fade + póster que escala de 0.9 a 1; textos y botones aparecen escalonados.
- **Abrir formulario**: entra desde abajo (modal) con fade.
- **Chips de filtro**: el fondo del chip activo transiciona de color; la lista hace cross-fade.
- **Estrellas**: al tocar, la estrella hace "pop" (escala 1 → 1.3 → 1).
- **Marcar vista/pendiente**: la etiqueta cambia con fade + leve rebote.
- **Eliminar**: la celda se desvanece y colapsa antes de quitarse.
- **Botón +**: rota 90° al pulsar; aparece con escala al cargar la página.
- **Cambio de modo claro/oscuro**: cross-fade del contenido; el ícono sol/luna rota.
- **Estados**: shimmer (esqueleto) mientras carga; mensaje de vacío/error aparece con fade.

Implementación: API de animación de MAUI (`FadeTo`, `TranslateTo`, `ScaleTo`, `RotateTo`, `Animation`) encapsulada en una clase helper (`Helpers/Animaciones.cs`), como en TiendaApp (`Views/Controles/Animar.cs`). Respetar "reducir movimiento" del sistema si está activo (saltarse animaciones decorativas).

Referencia visual aprobada: `.superpowers/brainstorm/*/content/estilo.html` (opciones A y B).

## Preparado para web

- Campos Firestore en inglés y planos.
- Paleta como tokens con nombre (tabla de arriba) → se copia a CSS variables en React.
- Póster en base64 → `<img src="data:image/jpeg;base64,...">` directo.

## Configuración Firebase

`Services/FirebaseConfig.cs` con `ApiKey` y `ProjectId` (`cinelist-7a4de`). La API key web de Firebase no es secreta por sí sola; la protección real son las reglas.

Config web (para la futura versión React):

```js
const firebaseConfig = {
  apiKey: "AIzaSyBkCaRxSZDMS0uvn2IvqDWyjUDnhruJckE",
  authDomain: "cinelist-7a4de.firebaseapp.com",
  projectId: "cinelist-7a4de",
  storageBucket: "cinelist-7a4de.firebasestorage.app",
  messagingSenderId: "1082464722268",
  appId: "1:1082464722268:web:8683df69c716db3795b599"
};
```

Verificado 2026-11-03: REST `GET .../documents/movies` → 200; otra colección → 403 (reglas correctas).

## Calidad y verificación

- Proyecto de pruebas xUnit para lógica pura: filtro (búsqueda + chips), agrupación por género, conteos del footer, mapeo Firestore ↔ `Pelicula`, compresión de póster bajo el límite.
- Prueba manual en emulador `Pixel_8`: CRUD, foto desde galería, cambio de modo, pull-to-refresh, menú contextual.
- Build: `dotnet build -f net9.0-android -p:JavaSdkDirectory=/usr/lib/jvm/java-17-openjdk -p:AndroidSdkDirectory=$HOME/Android/Sdk`.

## Riesgos

- Reglas abiertas en `movies`: cualquiera con la API key puede modificar datos. Mitigación futura: autenticación anónima.
- Lista descarga todos los pósters base64: válido para decenas de películas, no para miles.
- `ListView` está marcado obsoleto a partir de .NET 10; en .NET 9 funciona sin avisos. Mencionarlo en la exposición como dato técnico.

## Fuera de esta entrega (posible después)

- Versión web en React.
- Informe .docx de la guía (preguntas orientadoras, cuadro comparativo del trabajo autónomo).
