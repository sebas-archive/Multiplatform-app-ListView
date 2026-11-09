using Microsoft.JSInterop;

namespace CineList.Web.Services;

/// <summary>Acceso a localStorage del navegador vía interop.</summary>
public class AlmacenLocal(IJSRuntime js)
{
    public async Task<string?> LeerAsync(string clave)
    {
        try { return await js.InvokeAsync<string?>("cinelist.leer", clave); }
        catch (JSException) { return null; }
    }

    public async Task GuardarAsync(string clave, string valor)
    {
        try { await js.InvokeVoidAsync("cinelist.guardar", clave, valor); }
        catch (JSException) { /* almacenamiento bloqueado: se ignora */ }
    }
}
