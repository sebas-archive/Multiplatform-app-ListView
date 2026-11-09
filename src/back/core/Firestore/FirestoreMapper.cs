using System.Globalization;
using System.Text.Json.Nodes;
using CineList.Core.Models;

namespace CineList.Core.Firestore;

/// <summary>Convierte entre <see cref="Pelicula"/> y el JSON de documentos de la API REST de Firestore.</summary>
public static class FirestoreMapper
{
    /// <summary>Genera el objeto "fields" de Firestore para la película.</summary>
    public static JsonObject ACampos(Pelicula p)
    {
        return new JsonObject
        {
            ["title"] = Texto(p.Titulo),
            ["director"] = Texto(p.Director),
            ["genre"] = Texto(p.Genero),
            ["year"] = Entero(p.Anio),
            ["durationMin"] = Entero(p.DuracionMin),
            ["synopsis"] = Texto(p.Sinopsis),
            ["watched"] = new JsonObject { ["booleanValue"] = p.Vista },
            ["rating"] = Entero(p.Calificacion),
            ["poster"] = p.PosterBase64 is null ? new JsonObject { ["nullValue"] = null } : Texto(p.PosterBase64),
            ["createdAt"] = new JsonObject
            {
                ["timestampValue"] = p.CreadaEn.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture)
            }
        };
    }

    /// <summary>Lee un documento Firestore (name + fields); los campos faltantes toman valores por defecto.</summary>
    public static Pelicula DesdeDocumento(JsonObject documento)
    {
        var campos = documento["fields"] as JsonObject ?? new JsonObject();
        var nombre = (string?)documento["name"] ?? "";

        return new Pelicula
        {
            Id = nombre[(nombre.LastIndexOf('/') + 1)..],
            Titulo = LeerTexto(campos, "title") ?? "",
            Director = LeerTexto(campos, "director") ?? "",
            Genero = LeerTexto(campos, "genre") ?? "",
            Anio = LeerEntero(campos, "year"),
            DuracionMin = LeerEntero(campos, "durationMin"),
            Sinopsis = LeerTexto(campos, "synopsis") ?? "",
            Vista = campos["watched"]?["booleanValue"]?.GetValueKind() is System.Text.Json.JsonValueKind.True,
            Calificacion = LeerEntero(campos, "rating"),
            PosterBase64 = LeerTexto(campos, "poster"),
            CreadaEn = LeerFecha(campos, "createdAt")
        };
    }

    private static JsonObject Texto(string valor) => new() { ["stringValue"] = valor };

    private static JsonObject Entero(int valor) =>
        new() { ["integerValue"] = valor.ToString(CultureInfo.InvariantCulture) };

    private static string? LeerTexto(JsonObject campos, string nombre) =>
        TryString(campos[nombre]?["stringValue"]);

    private static int LeerEntero(JsonObject campos, string nombre)
    {
        var campo = campos[nombre];
        var entero = campo?["integerValue"];
        if (entero is not null)
        {
            if (int.TryParse(TryString(entero), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                return n;
        }
        var real = campo?["doubleValue"];
        if (real is not null)
        {
            if (double.TryParse(TryString(real), NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                return (int)Math.Round(d);
        }
        return 0;
    }

    private static DateTimeOffset LeerFecha(JsonObject campos, string nombre)
    {
        var texto = TryString(campos[nombre]?["timestampValue"]);
        return DateTimeOffset.TryParse(texto, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var f)
            ? f
            : default;
    }

    /// <summary>Convierte un nodo JSON (cadena o número) a texto invariante, o null si no es un valor.</summary>
    private static string? TryString(JsonNode? nodo)
    {
        if (nodo is not JsonValue valor) return null;
        if (valor.TryGetValue<string>(out var s)) return s;
        return valor.ToString();
    }
}
