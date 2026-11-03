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

## Compilar y ejecutar en Android

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
- La configuración está en `src/CineList/Services/FirebaseConfig.cs` (`ApiKey` y `ProjectId`). La API key web de Firebase no es secreta por sí sola; la protección real son las reglas de seguridad.
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

Los pósters se guardan como JPEG en base64 dentro del documento (lado mayor 600 px, calidad 70, máximo 900 000 caracteres).

## Evidencias de la Guía 8

| Evidencia | Archivo |
|---|---|
| 1. Modelo | [`src/CineList.Core/Models/Pelicula.cs`](src/CineList.Core/Models/Pelicula.cs) |
| 2. Celda personalizada (`ViewCell`) | [`src/CineList/Views/Celdas/PeliculaViewCell.cs`](src/CineList/Views/Celdas/PeliculaViewCell.cs) |
| 3. Vista con el `ListView` | [`src/CineList/Views/PeliculasPage.xaml`](src/CineList/Views/PeliculasPage.xaml) |
| 4. Evento de selección `OnPeliculaTapped` (`ItemTapped`) | [`src/CineList/Views/PeliculasPage.xaml.cs`](src/CineList/Views/PeliculasPage.xaml.cs#L43) |

## Funciones

- Lista de películas agrupada por género, con encabezados de grupo
- Búsqueda sin distinguir mayúsculas ni tildes (título, director y género)
- Chips de filtro (por ejemplo vistas y pendientes)
- Acciones contextuales con pulsación larga (marcar como vista o pendiente, editar, eliminar)
- Pull-to-refresh
- Calificación con estrellas
- Fotos de póster desde la cámara o la galería, reducidas antes de guardarse
- Detalle, alta y edición de películas
- Temas claro y oscuro según el sistema
- Animaciones breves (se omiten las decorativas si el sistema pide reducir movimiento)
- Estados de carga, vacío y error con "Reintentar" cuando no hay conexión
