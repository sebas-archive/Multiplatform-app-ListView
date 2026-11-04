# CineList

Aplicación móvil de lista de películas hecha con .NET MAUI 9 (Android). Proyecto de la Guía 8, "Trabajo con ListView". Las películas se guardan en Firebase Firestore y se muestran en un `ListView` agrupado por género con celdas personalizadas (`ViewCell`).

## Tecnologías

- .NET 9 y .NET MAUI (destino `net9.0-android`, Android 5.0 / API 21 o superior)
- `ListView` con `PeliculaViewCell : ViewCell` en C# y `ItemTapped`
- Firebase Firestore mediante la API REST (sin SDK nativo)
- xUnit para las pruebas de la lógica (`CineList.Core`)
- Íconos vectoriales (Material Symbols), sin emojis

## Estructura

- `src/CineList`: aplicación MAUI (vistas, celdas, servicios de la interfaz)
- `src/CineList.Core`: modelo, búsqueda, validación, mapeo de Firestore y repositorio
- `tests/CineList.Core.Tests`: pruebas unitarias

## Paso a paso: cómo ver la app

CineList es una app **Android**. Hay tres formas de verla.

### Opción A: en el PC con el emulador de Android (la más fácil para el video)

Requisitos (ya instalados en este equipo): .NET 9 SDK con `maui-android`, JDK 17, Android SDK y el emulador `Pixel_8`.

1. Abre una terminal en la carpeta del proyecto:
   ```bash
   cd "/home/t1an/aplicacion listview"
   ```
2. Enciende el emulador (se abre una ventana con el celular virtual; espera a que muestre la pantalla de inicio):
   ```bash
   $HOME/Android/Sdk/emulator/emulator -avd Pixel_8 &
   ```
3. Instala y abre la app. Elige una de las dos formas:
   - **Con el APK ya compilado** (rápido):
     ```bash
     $HOME/Android/Sdk/platform-tools/adb install -r CineList.apk
     $HOME/Android/Sdk/platform-tools/adb shell monkey -p com.cinelist.app -c android.intent.category.LAUNCHER 1
     ```
   - **Compilando desde el código** (si cambiaste algo):
     ```bash
     dotnet build src/CineList -f net9.0-android -t:Run \
       -p:JavaSdkDirectory=/usr/lib/jvm/java-17-openjdk \
       -p:AndroidSdkDirectory=$HOME/Android/Sdk
     ```
4. La app aparece en el emulador. También queda instalada en el cajón de apps como **CineList** (ícono de claqueta dorada).
5. Para grabar el video puedes usar el grabador de pantalla del sistema sobre la ventana del emulador, o el botón de grabar de los controles del emulador (menú `...` → *Record and Playback*).

Si la app se ve con una versión vieja, desinstálala y vuelve al paso 3:
```bash
$HOME/Android/Sdk/platform-tools/adb uninstall com.cinelist.app
```

### Opción B: en tu celular Android

1. Copia `CineList.apk` (está en la raíz del proyecto) al celular: por cable USB, Google Drive, Telegram o correo.
2. En el celular, abre el archivo. Android pedirá permitir **"Instalar apps desconocidas"** para la app con la que lo abriste (Archivos, Drive...). Actívalo y vuelve.
3. Toca **Instalar** y luego **Abrir**.
4. El celular necesita internet: las películas se leen y guardan en Firebase.

Alternativa por cable: activa *Opciones de desarrollador → Depuración USB*, conecta el celular y ejecuta `adb install -r CineList.apk` (mismo comando de la opción A).

Para mostrar el celular en la pantalla del PC (útil en la exposición) puedes usar [scrcpy](https://github.com/Genymobile/scrcpy): con el celular conectado por USB y la depuración activada, ejecuta `scrcpy`.

### Opción C: ¿y en la web?

Esta app **no se puede abrir en el navegador**: .NET MAUI genera apps nativas (Android, iOS, Windows, macOS), no páginas web. Lo que sí se puede ver desde el navegador:

- **Los datos**: en la [consola de Firebase](https://console.firebase.google.com/) → proyecto `cinelist-7a4de` → *Firestore Database* → colección `movies`. Si agregas o editas una película en la app, el cambio aparece ahí.
- **Una versión web de la app**: está planeada como trabajo futuro (React leyendo la misma colección de Firestore, con los mismos colores). Todavía no existe.

Nota sobre Windows: el proyecto incluye el destino `net9.0-windows10.0.19041.0`, que solo se compila desde Windows con Visual Studio 2022 (carga de trabajo ".NET MAUI"). No se ha probado; la versión probada es la de Android.

## Compilar y ejecutar en Android (referencia)

Requisitos: .NET 9 SDK con la carga de trabajo `maui-android`, JDK 17 y Android SDK.

```bash
# Depuración
dotnet build src/CineList -f net9.0-android \
  -p:JavaSdkDirectory=/usr/lib/jvm/java-17-openjdk \
  -p:AndroidSdkDirectory=$HOME/Android/Sdk

# Instalar y ejecutar en el emulador o dispositivo conectado
dotnet build src/CineList -f net9.0-android -t:Run \
  -p:JavaSdkDirectory=/usr/lib/jvm/java-17-openjdk \
  -p:AndroidSdkDirectory=$HOME/Android/Sdk

# APK de publicación (queda en src/CineList/bin/Release/net9.0-android/publish/)
dotnet publish src/CineList -f net9.0-android -c Release -p:AndroidPackageFormat=apk \
  -p:JavaSdkDirectory=/usr/lib/jvm/java-17-openjdk \
  -p:AndroidSdkDirectory=$HOME/Android/Sdk
```

El paquete es `com.cinelist.app`. El APK firmado resultante (`com.cinelist.app-Signed.apk`) se copia a `CineList.apk` en la raíz del repositorio; ese archivo no se versiona en git.

## Pruebas

```bash
dotnet test tests/CineList.Core.Tests
```

## Configuración de Firebase

- Proyecto: `cinelist-7a4de`
- Colección: `movies` (campos `title`, `director`, `genre`, `year`, `durationMin`, `synopsis`, `watched`, `rating`, `poster`, `createdAt`)
- La configuración está en `src/CineList.Core/Firestore/FirebaseConfig.cs` (`ApiKey` y `ProjectId`). La API key web de Firebase no es secreta por sí sola; la protección real son las reglas de seguridad.
- Reglas usadas: lectura y escritura abiertas únicamente sobre `movies/{id}`; cualquier otra colección responde 403.

```
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    match /movies/{id} {
      allow read, write: if true;
    }
  }
}
```

Advertencia: las reglas abiertas son aceptables solo para un proyecto de clase. Cualquier persona con la API key puede leer y modificar la colección `movies`. Para un uso real habría que activar autenticación (por ejemplo anónima) y restringir las reglas.

Los pósters se guardan como JPEG en base64 dentro del documento (lado mayor 600 px, calidad 70, máximo 900 000 caracteres de base64).

## Evidencias de la Guía 8

| Evidencia | Archivo |
|---|---|
| 1. Modelo | [`src/CineList.Core/Models/Pelicula.cs`](src/CineList.Core/Models/Pelicula.cs) |
| 2. Celda personalizada (`ViewCell`) | [`src/CineList/Views/Celdas/PeliculaViewCell.cs`](src/CineList/Views/Celdas/PeliculaViewCell.cs) |
| 3. Vista con el `ListView` | [`src/CineList/Views/PeliculasPage.xaml`](src/CineList/Views/PeliculasPage.xaml) |
| 4. Evento de selección `OnPeliculaTapped` (`ItemTapped`) | [`src/CineList/Views/PeliculasPage.xaml.cs`](src/CineList/Views/PeliculasPage.xaml.cs#L113) |

## Funciones

- Lista de películas agrupada por género, con encabezados de grupo
- Búsqueda sin distinguir mayúsculas ni tildes (título y director)
- Chips de filtro: todas, vistas y pendientes
- Acciones contextuales con pulsación larga (marcar como vista o pendiente, eliminar)
- Pull-to-refresh
- Calificación con estrellas
- Fotos de póster desde la cámara o la galería, reducidas antes de guardarse
- Detalle, alta y edición de películas
- Temas claro y oscuro: el primer arranque sigue al sistema; un botón permite alternarlo y la elección se recuerda
- Animaciones breves (se omiten las decorativas si el sistema pide reducir movimiento)
- Estados de carga, vacío y error con "Reintentar" cuando no hay conexión
