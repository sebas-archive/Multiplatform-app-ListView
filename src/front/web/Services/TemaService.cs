using Microsoft.JSInterop;

namespace CineList.Web.Services;

/// <summary>
/// Tema claro/oscuro. La preferencia vive en localStorage ("tema": "claro" | "oscuro");
/// sin valor se sigue prefers-color-scheme. El script de index.html ya fijó data-tema antes de cargar Blazor.
/// </summary>
public class TemaService(IJSRuntime js, AlmacenLocal almacen)
{
    private const string ClaveTema = "tema";

    public bool Oscuro { get; private set; } = true;

    public event Action? Cambio;

    /// <summary>Lee el tema que dejó el script inicial en &lt;html&gt;.</summary>
    public async Task InicializarAsync()
    {
        var actual = await js.InvokeAsync<string>("cinelist.temaActual");
        Oscuro = actual != "claro";
        Cambio?.Invoke();
    }

    public async Task AlternarAsync()
    {
        Oscuro = !Oscuro;
        var valor = Oscuro ? "oscuro" : "claro";
        await js.InvokeVoidAsync("cinelist.fijarTema", valor);
        await almacen.GuardarAsync(ClaveTema, valor);
        Cambio?.Invoke();
    }
}
