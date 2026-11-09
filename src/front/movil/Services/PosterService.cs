using System.Diagnostics;
using CineList.Core.Poster;
using Microsoft.Maui.Graphics.Platform;
using IImage = Microsoft.Maui.Graphics.IImage;

namespace CineList.Services;

// Obtiene la foto del póster (cámara o galería) y la devuelve como JPEG en base64,
// reducida para que quepa en el documento de Firestore (límite ~1 MiB por documento).
public class PosterService
{
    private const float CalidadNormal = PosterPolicy.CalidadJpeg / 100f; // 0.70
    private const float CalidadReducida = 0.5f;
    private const int LadoReducido = 450;

    public bool PuedeTomarFoto => MediaPicker.Default.IsCaptureSupported;

    // Devuelve el base64 o null si el usuario cancela o niega el permiso (en ese caso ya se avisó).
    // alProcesar se llama cuando ya hay foto y empieza la compresión (para mostrar el indicador).
    public async Task<string?> TomarFotoAsync(Action? alProcesar = null)
    {
        if (!PuedeTomarFoto) return null;

        var estado = await Permissions.RequestAsync<Permissions.Camera>();
        if (estado != PermissionStatus.Granted)
        {
            await AvisarPermisoAsync("Permiso de cámara",
                "CineList necesita la cámara para tomar la foto del póster. Puedes activarla en los ajustes de la app.");
            return null;
        }

        FileResult? foto;
        try
        {
            foto = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions { Title = "Foto del póster" });
        }
        catch (PermissionException ex)
        {
            Debug.WriteLine($"[PosterService] {ex.Message}");
            await AvisarPermisoAsync("Permiso necesario",
                "No se pudo abrir la cámara porque falta un permiso. Puedes activarlo en los ajustes de la app.");
            return null;
        }
        if (foto is null) return null;
        alProcesar?.Invoke();
        return await ProcesarAsync(foto);
    }

    // En Android 13+ usa el selector de fotos del sistema, que no pide permisos
    public async Task<string?> ElegirDeGaleriaAsync(Action? alProcesar = null)
    {
        FileResult? foto;
        try
        {
            foto = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions { Title = "Elige el póster" });
        }
        catch (PermissionException ex)
        {
            Debug.WriteLine($"[PosterService] {ex.Message}");
            await AvisarPermisoAsync("Permiso de fotos",
                "CineList necesita acceso a tus fotos para elegir el póster. Puedes activarlo en los ajustes de la app.");
            return null;
        }
        if (foto is null) return null;
        alProcesar?.Invoke();
        return await ProcesarAsync(foto);
    }

    // Lectura, decodificación y compresión fuera del hilo de la interfaz
    private static Task<string> ProcesarAsync(FileResult foto) => Task.Run(async () =>
    {
        byte[] bytes;
        await using (var origen = await foto.OpenReadAsync())
        using (var memoria = new MemoryStream())
        {
            await origen.CopyToAsync(memoria);
            bytes = memoria.ToArray();
        }
        return Comprimir(bytes, foto.FullPath);
    });

    // Reduce a 600 px y JPEG 0.70; si no cabe, reintenta a 450 px y 0.5; si aún no cabe, error
    private static string Comprimir(byte[] bytes, string? ruta)
    {
        // Downsize/Orientar pueden devolver la misma imagen: se liberan todas al final, una sola vez
        var imagenes = new List<IImage>();
        T Guardar<T>(T img) where T : IImage { imagenes.Add(img); return img; }
        try
        {
            var original = Guardar(PlatformImage.FromStream(new MemoryStream(bytes), ImageFormat.Jpeg)
                                   ?? throw new InvalidOperationException("No se pudo leer la imagen"));
            if (original.Width <= 0 || original.Height <= 0)
                throw new InvalidOperationException("No se pudo leer la imagen");

            var normal = Guardar(Orientar(Guardar(original.Downsize(PosterPolicy.LadoMaximo)), LeerGiro(bytes, ruta)));
            var base64 = Codificar(normal, CalidadNormal);
            Debug.WriteLine($"[PosterService] {original.Width}x{original.Height} -> {normal.Width}x{normal.Height}, {base64.Length} chars");
            if (PosterPolicy.CabeEnDocumento(base64)) return base64;

            var reducida = Guardar(normal.Downsize(LadoReducido));
            base64 = Codificar(reducida, CalidadReducida);
            Debug.WriteLine($"[PosterService] reintento {reducida.Width}x{reducida.Height}, {base64.Length} chars");
            if (PosterPolicy.CabeEnDocumento(base64)) return base64;

            throw new InvalidOperationException("La imagen es demasiado grande");
        }
        finally
        {
            foreach (var img in imagenes.Distinct(ReferenceEqualityComparer.Instance).Cast<IImage>())
                img.Dispose();
        }
    }

    private static string Codificar(IImage imagen, float calidad)
    {
        using var jpeg = imagen.AsStream(ImageFormat.Jpeg, calidad);
        using var salida = new MemoryStream();
        jpeg.CopyTo(salida);
        return Convert.ToBase64String(salida.ToArray());
    }

    // Grados que hay que girar la foto según su EXIF (las cámaras guardan la foto "acostada")
    private static int LeerGiro(byte[] bytes, string? ruta)
    {
#if ANDROID
        try
        {
            Android.Media.ExifInterface exif;
            if (!string.IsNullOrEmpty(ruta) && File.Exists(ruta))
                exif = new Android.Media.ExifInterface(ruta);
            else if (OperatingSystem.IsAndroidVersionAtLeast(24))
                exif = new Android.Media.ExifInterface(new MemoryStream(bytes));
            else
                return 0;
            using (exif)
            {
                return exif.GetAttributeInt(Android.Media.ExifInterface.TagOrientation, 1) switch
                {
                    6 => 90,  // ORIENTATION_ROTATE_90
                    3 => 180, // ORIENTATION_ROTATE_180
                    8 => 270, // ORIENTATION_ROTATE_270
                    _ => 0,
                };
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PosterService] EXIF: {ex.Message}");
            return 0;
        }
#else
        return 0;
#endif
    }

    // PlatformImage.FromStream no aplica la orientación EXIF en Android: se gira el bitmap ya reducido
    private static IImage Orientar(IImage imagen, int grados)
    {
#if ANDROID
        if (grados == 0 || imagen is not PlatformImage { PlatformRepresentation: { } bitmap }) return imagen;
        using var matriz = new Android.Graphics.Matrix();
        matriz.PostRotate(grados);
        var girado = Android.Graphics.Bitmap.CreateBitmap(bitmap, 0, 0, bitmap.Width, bitmap.Height, matriz, true);
        return girado is null ? imagen : new PlatformImage(girado);
#else
        return imagen;
#endif
    }

    private static async Task AvisarPermisoAsync(string titulo, string mensaje)
    {
        try
        {
            if (Shell.Current is not { } shell) return;
            var abrirAjustes = await shell.DisplayAlert(titulo, mensaje, "Abrir ajustes", "Ahora no");
            if (abrirAjustes) AppInfo.Current.ShowSettingsUI();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PosterService] {ex.Message}");
        }
    }
}
