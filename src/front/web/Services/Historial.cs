using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CineList.Web.Services;

/// <summary>
/// Salida de formularios y detalle sin dejar entradas obsoletas en el historial del navegador.
/// Si la entrada anterior pertenece a la app se retrocede con history.back() (la entrada actual desaparece
/// al avanzar); si no (carga directa o recarga) se navega con replace al destino.
/// </summary>
public sealed class Historial(NavigationManager navegacion, IJSRuntime js)
{
    /// <summary>True si la entrada anterior del historial es de la app (se llegó navegando dentro de ella).</summary>
    public async Task<bool> HayEntradaPreviaAsync()
    {
        try { return await js.InvokeAsync<bool>("cinelist.hayEntradaPrevia"); }
        catch (JSException) { return false; }
        catch (JSDisconnectedException) { return false; }
    }

    /// <summary>True si la navegación en curso (p. ej. la que detuvo un NavigationLock) la causó Atrás/Adelante.</summary>
    public async Task<bool> FueRetrocesoAsync()
    {
        try { return await js.InvokeAsync<bool>("cinelist.fueRetroceso"); }
        catch (JSException) { return false; }
        catch (JSDisconnectedException) { return false; }
    }

    /// <summary>
    /// Sale de la página actual. El llamador debe haber puesto antes su bandera de salida para que su
    /// NavigationLock deje pasar el retroceso. Devuelve true si fue con history.back().
    /// </summary>
    public async Task<bool> SalirAsync(string destino)
    {
        if (await HayEntradaPreviaAsync())
        {
            try { await js.InvokeVoidAsync("history.back"); return true; }
            catch (JSException) { }
            catch (JSDisconnectedException) { return true; }
        }
        navegacion.NavigateTo(destino, replace: true);
        return false;
    }
}
