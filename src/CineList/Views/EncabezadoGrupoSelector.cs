using CineList.Core.Lista;

namespace CineList.Views;

// Con orden distinto de Género la lista es plana (un solo grupo de género ""): su encabezado no ocupa espacio.
public class EncabezadoGrupoSelector : DataTemplateSelector
{
    public DataTemplate? Con { get; set; }
    public DataTemplate? Sin { get; set; }

    protected override DataTemplate? OnSelectTemplate(object item, BindableObject container) =>
        item is GrupoPeliculas { Genero.Length: > 0 } ? Con : Sin;
}
