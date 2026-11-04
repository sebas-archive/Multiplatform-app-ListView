# CineList

Aplicación multiplataforma para administrar una lista personal de películas. La app de Android está hecha con **.NET MAUI 9** y la versión web con **Blazor WebAssembly**; ambas comparten la lógica de `CineList.Core` y guardan los datos en **Firebase Firestore**.

Proyecto de la **Guía 8, «Trabajo con ListView»**, de Computación móvil (Universidad Manuela Beltrán). El requisito central es un `ListView` agrupado por género con celdas personalizadas (`ViewCell`) y un evento de selección que abre el detalle.

**Estado:** app Android y web funcionando; **132 pruebas unitarias** pasando.

## Contenido

- [Qué es](#qué-es)
- [Características](#características)
- [Tecnologías](#tecnologías)
- [Estructura del repositorio](#estructura-del-repositorio)
- [Requisitos](#requisitos)
- [Inicio rápido](#inicio-rápido)
  - [Web](#web)
  - [Android](#android)
  - [Pruebas](#pruebas)
- [Arquitectura](#arquitectura)
- [Firebase](#firebase)
- [Evidencias de la Guía 8](#evidencias-de-la-guía-8)
- [Publicación](#publicación)
- [Limitaciones y notas](#limitaciones-y-notas)
- [Contexto académico](#contexto-académico)

## Qué es

CineList permite registrar películas con título, director, género, año, duración, sinopsis, estado (vista o pendiente), calificación y póster. La lista se agrupa por género y se puede buscar, filtrar y ordenar; cada fila abre una pantalla de detalle desde la que se puede editar, cambiar de estado o eliminar.

La misma solución existe en dos clientes:

- **Android:** app nativa con .NET MAUI, `ListView`, `PeliculaViewCell : ViewCell` y navegación por `Shell`.
- **Web:** Blazor WebAssembly, con componentes Razor que reutilizan el modelo y las reglas de negocio.

Ambos leen y escriben la misma colección de Firestore, así que un cambio hecho en la web se ve en la app y viceversa al recargar.

## Características

### Lista y navegación

- `ListView` agrupado por género, con encabezados de grupo (solo con el orden Género; con los demás órdenes la lista es plana).
- Celda personalizada con póster, título, director, año, duración, estrellas y etiqueta **VISTA/PENDIENTE**.
- Evento `ItemTapped` que navega al detalle de la película.
- Detalle con sinopsis, calificación, acciones de estado, edición y eliminación.
- Alta y edición mediante formulario.
- Acciones contextuales con pulsación larga y actualización por deslizamiento (*pull-to-refresh*).

### Búsqueda, filtros y orden

- Búsqueda por título o director, sin distinguir mayúsculas ni tildes.
- Chips de estado: **Todas**, **Vistas** y **Pendientes**.
- Panel **Filtros y orden** con:
  - selección múltiple de géneros;
  - década (Todas, Antes de 1980, 1980s, 1990s, 2000s, 2010s, 2020s);
  - calificación mínima;
  - orden por Género, Título, Año, Calificación o Recientes.
- El botón **Ver N películas** se actualiza en vivo y **Limpiar** restablece todo.
- Insignia con el número de filtros activos y línea **Activos:** con el resumen.
- Los filtros y el orden se recuerdan entre aperturas; el texto de búsqueda no.

### Datos y póster

- Persistencia en Firebase Firestore mediante su API REST, sin SDK nativo.
- Póster desde la cámara o la galería en Android, y desde un archivo en la web.
- La imagen se reduce a 600 px de lado mayor, JPEG calidad 70 y máximo 900 000 caracteres en base64.
- Límite de 25 MB por archivo en la versión web.

### Interfaz

- Temas claro y oscuro: el primer arranque sigue al sistema y el botón sol/luna lo alterna.
- Animaciones breves: el póster vuela de la lista al detalle, el detalle usa el póster desenfocado con parallax (Android 12 o superior), las estrellas se llenan, la etiqueta gira al cambiar de estado y el panel de filtros sube con rebote.
- Las animaciones decorativas se omiten si el sistema pide reducir movimiento.
- Estados de carga, lista vacía, sin resultados y error de conexión con **Reintentar**.

### Versión web

- Mismos datos, reglas de búsqueda, filtros y orden que la app Android.
- Cada fila tiene un menú **⋮** en lugar de la pulsación larga.
- El póster se sube desde un archivo de imagen.
- No hay actualización en tiempo real ni *pull-to-refresh*: la lista se refresca con **Recargar** o al recargar la página.
- Filtros, orden y tema se guardan en `localStorage`.

## Tecnologías

| Capa | Tecnología |
|---|---|
| App Android | .NET MAUI 9, XAML, Shell, `ListView` y `ViewCell` |
| Web | Blazor WebAssembly (.NET 9), componentes Razor e interop con JavaScript |
| Lógica compartida | `CineList.Core` (.NET 9) |
| Datos | Firebase Firestore mediante API REST |
| MVVM | CommunityToolkit.Mvvm |
| Pruebas | xUnit |
| Iconografía | Material Symbols Rounded (fuentes locales) |

## Estructura del repositorio

```text
CineList.sln                 Solución con los cuatro proyectos
CineList.apk                 APK de ejemplo (no se versiona)
src/
  CineList/                  App .NET MAUI para Android
    Views/                   Páginas y celdas (PeliculasPage, PeliculaViewCell)
    ViewModels/              Estado y comandos de cada pantalla
    Services/                Tema, póster, transiciones y barra de estado
    Controls/                Controles reutilizables (chips, estrellas, póster)
  CineList.Core/             Lógica compartida, sin dependencias de interfaz
    Models/                  Pelicula
    Lista/                   Búsqueda, filtros, orden y agrupación
    Firestore/               Mapeo, repositorio y configuración
    Formulario/              Validación y datos del formulario
    Poster/                  Política de tamaño y compresión
  CineList.Web/              Versión Blazor WebAssembly
    Pages/                   Lista, detalle y formulario
    Components/              Filas, filtros, pósters y diálogos
    Services/                Estado de la lista, tema y almacenamiento local
tests/
  CineList.Core.Tests/       132 pruebas con xUnit
```

`CineList.sln` es el archivo de solución: agrupa los cuatro proyectos para abrirlos juntos en Visual Studio, Rider o VS Code y compilarlos o probarlos desde la raíz. También puedes consultar sus proyectos con `dotnet sln list`.

## Requisitos

Para la **versión web** solo necesitas el SDK de .NET 9.

Para **Android**:

- .NET 9 SDK con la carga de trabajo `maui-android` (`dotnet workload install maui-android`).
- JDK 17.
- Android SDK (API 35 recomendado) y las build-tools; la app soporta Android 5.0 (API 21) o superior.
- Un emulador o un dispositivo con depuración USB.
- Conexión a internet: las películas se leen y se guardan en Firestore.

Puedes comprobar el entorno con:

```bash
dotnet --version
dotnet workload list
java -version
```

## Inicio rápido

Clona el repositorio y entra a la carpeta:

```bash
git clone https://github.com/sebas-archive/Multiplatform-app-ListView.git
cd Multiplatform-app-ListView
```

### Web

```bash
dotnet run --project src/CineList.Web
```

Abre <http://localhost:5180>. Para detener el servidor, pulsa `Ctrl+C`.

### Android

Define las rutas de tu JDK y tu Android SDK (ajústalas a tu equipo):

```bash
export JAVA_HOME=/usr/lib/jvm/java-17-openjdk
export ANDROID_HOME=$HOME/Android/Sdk
export PATH="$ANDROID_HOME/platform-tools:$ANDROID_HOME/emulator:$PATH"
```

Enciende un emulador (o conecta un dispositivo con depuración USB):

```bash
emulator -list-avds
emulator -avd Pixel_8 &
```

Compila, instala y ejecuta la app:

```bash
dotnet build src/CineList -f net9.0-android -t:Run \
  -p:JavaSdkDirectory="$JAVA_HOME" \
  -p:AndroidSdkDirectory="$ANDROID_HOME"
```

Si ya tienes el APK compilado en la raíz, puedes instalarlo directamente:

```bash
adb install -r CineList.apk
adb shell monkey -p com.cinelist.app -c android.intent.category.LAUNCHER 1
```

Para instalarlo en un celular, copia `CineList.apk` al teléfono (por USB, Drive, Telegram o correo), abre el archivo y permite **«Instalar apps desconocidas»** para la app con la que lo abras. El celular necesita internet porque los datos viven en Firestore.

Si la app se ve con una versión vieja, desinstálala y vuelve a instalar:

```bash
adb uninstall com.cinelist.app
```

El paquete es `com.cinelist.app`. Para ver el celular en el PC durante la exposición puedes usar [scrcpy](https://github.com/Genymobile/scrcpy).

### Pruebas

```bash
dotnet test tests/CineList.Core.Tests
```

Son **132 pruebas** de la lógica: filtros y orden, validación, mapeo de Firestore, repositorio, pósters y textos de la interfaz. No necesitan emulador ni conexión, porque el repositorio se prueba con un manejador HTTP simulado.

Para compilar la solución completa:

```bash
dotnet build CineList.sln \
  -p:JavaSdkDirectory="$JAVA_HOME" \
  -p:AndroidSdkDirectory="$ANDROID_HOME"
```

En Linux, la app MAUI necesita esas dos rutas (o las variables de entorno equivalentes). Si solo te interesan la web o las pruebas, compila esos proyectos directamente.

## Arquitectura

La solución se divide en tres capas:

- **`CineList.Core`:** modelo `Pelicula`, búsqueda, filtros, orden, validación, mapeo entre documentos de Firestore y objetos, repositorio REST y política de pósters. No referencia controles de interfaz, por eso se comparte y se prueba por separado.
- **`CineList` (MAUI):** navegación por `Shell`, vistas XAML, view models con CommunityToolkit.Mvvm y servicios de tema, póster y transiciones.
- **`CineList.Web` (Blazor):** componentes Razor, estado de la lista en servicios, persistencia en `localStorage` e interop con JavaScript para la reducción de imágenes y las transiciones.

El flujo de datos es unidireccional:

```text
Vista (XAML / Razor) → ViewModel → Repositorio → Firestore REST
```

Decisiones técnicas que conviene conocer:

- Se usa `CachingStrategy="RetainElement"` en el `ListView`. Con `RecycleElement`, al reconstruir los grupos por un cambio de orden o de filtros, Android dejaba una fila vacía y la última película desaparecía.
- El póster se guarda como JPEG en base64 dentro del documento porque Firestore limita cada documento a ~1 MiB.
- La web reduce la imagen en JavaScript con `createImageBitmap` y un lienzo antes de enviarla a Firestore. Blazor serializa los arreglos de bytes como base64 al cruzar a JavaScript, y el cliente los decodifica antes de usarlos como binario.

## Firebase

- Proyecto: `cinelist-7a4de`
- Colección: `movies`
- Campos: `title`, `director`, `genre`, `year`, `durationMin`, `synopsis`, `watched`, `rating`, `poster` y `createdAt`.
- Configuración: `src/CineList.Core/Firestore/FirebaseConfig.cs` (`ApiKey` y `ProjectId`). La API key web de Firebase no es un secreto por sí sola; la protección real son las reglas de seguridad.

Reglas usadas en el proyecto de clase: lectura y escritura abiertas únicamente sobre `movies/{id}`; cualquier otra colección responde 403.

```text
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    match /movies/{id} {
      allow read, write: if true;
    }
  }
}
```

> **Advertencia:** estas reglas son aceptables solo para un proyecto académico. Cualquier persona con la API key puede leer y modificar la colección `movies`. Para un uso real hay que activar autenticación (por ejemplo, anónima) y restringir las reglas por usuario.

Los pósters se guardan como JPEG en base64 dentro del documento (lado mayor 600 px, calidad 70, máximo 900 000 caracteres). Puedes ver los datos en la [consola de Firebase](https://console.firebase.google.com/) → proyecto `cinelist-7a4de` → *Firestore Database* → colección `movies`.

## Evidencias de la Guía 8

| Requisito | Archivo |
|---|---|
| Vista con el `ListView` | [`PeliculasPage.xaml`](src/CineList/Views/PeliculasPage.xaml#L141) |
| Clase Model | [`Pelicula.cs`](src/CineList.Core/Models/Pelicula.cs) |
| Clase ViewCell personalizada | [`PeliculaViewCell.cs`](src/CineList/Views/Celdas/PeliculaViewCell.cs) |
| Evento de selección (`ItemTapped`) | [`OnPeliculaTapped`](src/CineList/Views/PeliculasPage.xaml.cs#L147) |

La vista se ve en ejecución; el modelo, la celda y el evento están en los archivos enlazados.

## Publicación

### Firebase Hosting

La versión web se puede publicar en Firebase Hosting (estos comandos están documentados, no se han ejecutado en este repositorio):

```bash
dotnet publish src/CineList.Web -c Release
firebase init hosting
# Carpeta pública: src/CineList.Web/bin/Release/net9.0/publish/wwwroot
# Aplicación de una sola página: sí (reescribe todo a /index.html)
firebase deploy
```

### APK de Android

```bash
dotnet publish src/CineList -f net9.0-android -c Release -p:AndroidPackageFormat=apk \
  -p:JavaSdkDirectory="$JAVA_HOME" \
  -p:AndroidSdkDirectory="$ANDROID_HOME"
```

El APK firmado queda en `src/CineList/bin/Release/net9.0-android/publish/`. El archivo `CineList.apk` de la raíz es una copia para instalar rápido y **no se versiona** en Git.

## Limitaciones y notas

- La versión probada es **Android** (emulador y APK) y la **web**. Los destinos iOS y Windows están declarados en el proyecto, pero no se probaron; Windows requiere Visual Studio 2022 con la carga de trabajo de .NET MAUI.
- La versión web no tiene actualización en tiempo real ni *pull-to-refresh*.
- El desenfoque del póster en el detalle requiere Android 12 o superior.
- Las reglas abiertas de Firestore son solo para el proyecto de clase.
- El APK no se versiona: hay que compilarlo o copiarlo desde `publish/`.

## Contexto académico

Proyecto de la **Guía 8, «Trabajo con ListView»**, de la asignatura **Computación móvil**, Universidad Manuela Beltrán. El objetivo es construir una aplicación multiplataforma con un `ListView`, una clase Model, una clase ViewCell personalizada y un evento de selección.
