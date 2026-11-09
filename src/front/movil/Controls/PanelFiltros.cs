using System.ComponentModel;
using System.Diagnostics;
using CineList.Helpers;
using CineList.Services;
using CineList.ViewModels;
using Microsoft.Maui.Controls.Shapes;

namespace CineList.Controls;

// Panel inferior "Filtros y orden": velo + hoja que sube desde abajo, sobre la lista de la página.
// La hoja se arma en código porque cada apertura recibe un borrador nuevo (PanelFiltrosViewModel).
public class PanelFiltros : Grid
{
    // La hoja se prolonga bajo el borde inferior: el rebote de SpringOut (~10 % del recorrido)
    // sube la hoja un poco de más y así nunca deja ver un hueco debajo
    private const double Prolongacion = 96;

    private readonly BoxView _velo;
    private readonly Border _hoja;
    private readonly Grid _segmentado;
    private readonly Border _pildora;
    private readonly FlexLayout _generos;
    private readonly FlexLayout _decadas;
    private readonly EstrellasView _estrellas;
    private readonly Label _textoCalificacion;
    private readonly Button _botonVer;
    private readonly Label _numero;
    private readonly Label _palabra;
    private readonly ScrollView _desplazable;
    private readonly List<VisualElement> _secciones = [];
    private readonly Label[] _segmentos = new Label[5];

    private PanelFiltrosViewModel? _vm;
    private const double OpacidadVelo = 0.55;

    private bool _cerrando;
    private double _nivelVelo;
    private int _versionConteo;

    public bool Abierto => _vm is not null;

    public PanelFiltros()
    {
        IsVisible = false;

        _velo = new BoxView { Color = Colors.Black, Opacity = 0 };
        var toqueVelo = new TapGestureRecognizer();
        toqueVelo.Tapped += (_, _) => _vm?.CerrarCommand.Execute(null);
        _velo.GestureRecognizers.Add(toqueVelo);
        Children.Add(_velo);

        // Asa y encabezado
        var asa = new BoxView
        {
            WidthRequest = 40,
            HeightRequest = 4,
            CornerRadius = 2,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 0, 0, 14),
            Opacity = 0.4,
        };
        Tema(asa, BoxView.ColorProperty, "TextoSecundario");

        var titulo = new Label { Text = "Filtros y orden", FontFamily = "InterBold", FontSize = 19, VerticalOptions = LayoutOptions.Center };
        Tema(titulo, Label.TextColorProperty, "Texto");
        var limpiar = new Button
        {
            Text = "Limpiar",
            FontFamily = "InterSemi",
            FontSize = 14,
            BackgroundColor = Colors.Transparent,
            Padding = new Thickness(12, 0),
            HeightRequest = 40,
            MinimumWidthRequest = 0,
            HorizontalOptions = LayoutOptions.End,
        };
        Tema(limpiar, Button.TextColorProperty, "Acento");
        limpiar.SetBinding(Button.CommandProperty, nameof(PanelFiltrosViewModel.LimpiarCommand));
        SemanticProperties.SetDescription(limpiar, "Limpiar filtros y orden del panel");
        var encabezado = new Grid { ColumnDefinitions = Columnas("*,Auto"), Margin = new Thickness(0, 0, -8, 4) };
        encabezado.Add(titulo);
        encabezado.Add(limpiar, 1);
        SemanticProperties.SetHeadingLevel(titulo, SemanticHeadingLevel.Level2);

        // Ordenar por: control segmentado con una píldora que se desliza al elegir
        _pildora = new Border { StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 10 } };
        Tema(_pildora, BackgroundColorProperty, "Acento");
        _segmentado = new Grid { ColumnDefinitions = Columnas("*,*,*,*,*"), HeightRequest = 38 };
        _segmentado.Add(_pildora);
        var marcoSegmentado = new Border
        {
            Padding = 3,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 13 },
            Content = _segmentado,
        };
        Tema(marcoSegmentado, BackgroundColorProperty, "Campo");
        Tema(marcoSegmentado, Border.StrokeProperty, "BordeCampo");
        _segmentado.SizeChanged += (_, _) => MoverPildora(animar: false);

        _generos = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap };
        _decadas = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap };

        _estrellas = new EstrellasView { EsEditable = true, Tamano = 30, Spacing = 4, VerticalOptions = LayoutOptions.Center };
        _estrellas.SetBinding(EstrellasView.ValorProperty, nameof(PanelFiltrosViewModel.CalificacionMinima));
        SemanticProperties.SetDescription(_estrellas, "Calificación mínima");
        _textoCalificacion = new Label { FontFamily = "InterSemi", FontSize = 14, VerticalOptions = LayoutOptions.Center, Margin = new Thickness(12, 0, 0, 0) };
        Tema(_textoCalificacion, Label.TextColorProperty, "TextoSecundario");
        _textoCalificacion.SetBinding(Label.TextProperty, nameof(PanelFiltrosViewModel.TextoCalificacion));
        var calificacion = new HorizontalStackLayout { Children = { _estrellas, _textoCalificacion } };

        var cuerpo = new VerticalStackLayout { Spacing = 4, Padding = new Thickness(0, 0, 0, 8) };
        foreach (var (etiqueta, contenido) in new (string, View)[]
                 {
                     ("Ordenar por", marcoSegmentado),
                     ("Géneros", _generos),
                     ("Década", _decadas),
                     ("Calificación mínima", calificacion),
                 })
        {
            var seccion = new VerticalStackLayout { Spacing = 10, Children = { EtiquetaSeccion(etiqueta), contenido } };
            _secciones.Add(seccion);
            cuerpo.Add(seccion);
        }
        _desplazable = new ScrollView { Content = cuerpo, VerticalScrollBarVisibility = ScrollBarVisibility.Never };

        // Botón "Ver N películas": el número entra desde abajo al cambiar
        _botonVer = new Button { Style = Estilo("BotonPrimario"), Text = "", HeightRequest = 52 };
        _botonVer.SetBinding(Button.CommandProperty, nameof(PanelFiltrosViewModel.VerCommand));
        _botonVer.SetBinding(SemanticProperties.DescriptionProperty, nameof(PanelFiltrosViewModel.TextoBoton));
        _numero = TextoDelBoton();
        _palabra = TextoDelBoton();
        _palabra.SetBinding(Label.TextProperty, nameof(PanelFiltrosViewModel.PalabraPeliculas));
        var marcoNumero = new Grid { IsClippedToBounds = true, HeightRequest = 26, Children = { _numero } };
        var textoVer = new HorizontalStackLayout
        {
            Spacing = 5,
            InputTransparent = true,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Children = { TextoDelBoton("Ver"), marcoNumero, _palabra },
        };
        var pie = new Grid { Margin = new Thickness(0, 12, 0, 0), Children = { _botonVer, textoVer } };

        var estructura = new Grid
        {
            RowDefinitions = Filas("Auto,Auto,*,Auto"),
            Padding = new Thickness(20, 10, 20, 16 + Prolongacion),
        };
        estructura.Add(asa, 0, 0);
        estructura.Add(encabezado, 0, 1);
        estructura.Add(_desplazable, 0, 2);
        estructura.Add(pie, 0, 3);

        _hoja = new Border
        {
            VerticalOptions = LayoutOptions.End,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(26, 26, 0, 0) },
            Margin = new Thickness(0, 0, 0, -Prolongacion),
            Content = estructura,
            Shadow = new Shadow { Brush = Colors.Black, Opacity = 0.35f, Radius = 24, Offset = new Point(0, -4) },
        };
        Tema(_hoja, BackgroundColorProperty, "Superficie");
        Children.Add(_hoja);

        // Arrastrar el asa o el encabezado hacia abajo cierra la hoja
        var arrastre = new PanGestureRecognizer();
        arrastre.PanUpdated += OnArrastre;
        var zonaArrastre = new BoxView { Color = Colors.Transparent, HeightRequest = 28, VerticalOptions = LayoutOptions.Start };
        zonaArrastre.GestureRecognizers.Add(arrastre);
        estructura.Add(zonaArrastre, 0, 0);

        SizeChanged += (_, _) => LimitarAltura();
    }

    // Abre con un borrador nuevo: velo 0 -> 0.55 (200 ms) y hoja desde abajo con SpringOut (380 ms)
    public async Task AbrirAsync(PanelFiltrosViewModel vm)
    {
        if (Abierto) return;
        _vm = vm;
        _cerrando = false;
        vm.CerrarVista = CerrarAsync;
        vm.PropertyChanged += OnVmPropertyChanged;
        BindingContext = vm;
        ConstruirChips(vm);
        _numero.Text = vm.Conteo.ToString();
        _numero.TranslationY = 0;
        _numero.Opacity = 1;
        PintarSegmentos();
        _desplazable.ScrollToAsync(0, 0, false).SinEsperar();

        this.AbortAnimation("Arrastre");
        NivelVelo(0);
        _hoja.TranslationY = Height > 0 ? Height : 2000;
        IsVisible = true;
        LimitarAltura();
        MoverPildora(animar: false);

        if (Animaciones.ReducirMovimiento)
        {
            NivelVelo(1);
            _hoja.TranslationY = 0;
            return;
        }

        try
        {
            // Espera un ciclo de layout para conocer la altura real de la hoja
            await Task.Yield();
            var alto = _hoja.Height > 0 ? _hoja.Height : Height;
            _hoja.TranslationY = alto;
            await Task.WhenAll(
                AnimarVelo(1, 200, Easing.CubicOut),
                _hoja.TranslateTo(0, 0, 380, Easing.SpringOut),
                EntradaSecciones());
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PanelFiltros] {ex.Message}");
        }
        finally
        {
            if (!_cerrando)
            {
                NivelVelo(1);
                _hoja.TranslationY = 0;
            }
        }
    }

    // Cierra sin aplicar (botón atrás de Android)
    public void Cerrar() => _vm?.CerrarCommand.Execute(null);

    // Hoja hacia abajo con CubicIn (220 ms) y velo a 0
    private async Task CerrarAsync()
    {
        if (_cerrando || _vm is null) return;
        _cerrando = true;
        try
        {
            if (!Animaciones.ReducirMovimiento)
            {
                _hoja.AbortAnimation("TranslateTo");
                await Task.WhenAll(
                    AnimarVelo(0, 220, Easing.CubicIn),
                    _hoja.TranslateTo(0, _hoja.Height, 220, Easing.CubicIn));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PanelFiltros] {ex.Message}");
        }
        finally
        {
            IsVisible = false;
            _velo.Opacity = 0;
            _nivelVelo = 0;
            BarraEstado.FondoSistema(null, null);
            _vm.PropertyChanged -= OnVmPropertyChanged;
            _vm.CerrarVista = null;
            _vm = null;
            BindingContext = null;
        }
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PanelFiltrosViewModel.Conteo) && _vm is not null)
            AnimarConteo(_vm.Conteo.ToString());
        else if (e.PropertyName == nameof(PanelFiltrosViewModel.IndiceOrden))
        {
            PintarSegmentos();
            MoverPildora(animar: true);
        }
    }

    // El número sale hacia arriba (80 ms) y el nuevo entra desde abajo (80 ms)
    private async void AnimarConteo(string nuevo)
    {
        var version = ++_versionConteo;
        if (Animaciones.ReducirMovimiento)
        {
            _numero.Text = nuevo;
            return;
        }
        try
        {
            _numero.AbortAnimation("TranslateTo");
            _numero.AbortAnimation("FadeTo");
            await Task.WhenAll(
                _numero.TranslateTo(0, -18, 80, Easing.CubicIn),
                _numero.FadeTo(0, 80, Easing.CubicIn));
            if (version != _versionConteo) return; // llegó otro cambio: ese termina la animación
            _numero.Text = nuevo;
            _numero.TranslationY = 18;
            await Task.WhenAll(
                _numero.TranslateTo(0, 0, 80, Easing.CubicOut),
                _numero.FadeTo(1, 80, Easing.CubicOut));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PanelFiltros] {ex.Message}");
        }
        finally
        {
            if (version == _versionConteo)
            {
                _numero.Text = nuevo;
                _numero.TranslationY = 0;
                _numero.Opacity = 1;
            }
        }
    }

    private void ConstruirChips(PanelFiltrosViewModel vm)
    {
        Rellenar(_generos, vm.Generos, vm.AlternarGeneroCommand);
        Rellenar(_decadas, vm.Decadas, vm.SeleccionarDecadaCommand);

        _segmentado.Children.Clear();
        _segmentado.Add(_pildora);
        for (var i = 0; i < vm.Ordenes.Count; i++)
        {
            var opcion = vm.Ordenes[i];
            var segmento = new Label
            {
                Text = opcion.Texto,
                FontSize = 12.5,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                LineBreakMode = LineBreakMode.NoWrap,
            };
            var toque = new TapGestureRecognizer { Command = vm.SeleccionarOrdenCommand, CommandParameter = opcion };
            segmento.GestureRecognizers.Add(toque);
            SemanticProperties.SetDescription(segmento, $"Ordenar por {Core.Lista.OrdenPeliculasTexto.Etiqueta((Core.Lista.OrdenPeliculas)opcion.Valor!)}");
            _segmentos[i] = segmento;
            _segmentado.Add(segmento, i);
        }
    }

    private static void Rellenar(FlexLayout contenedor, IEnumerable<OpcionChip> opciones, System.Windows.Input.ICommand comando)
    {
        contenedor.Children.Clear();
        foreach (var opcion in opciones)
        {
            var chip = new ChipOpcion
            {
                Text = opcion.Texto,
                Activo = opcion.Seleccionado,
                Command = comando,
                CommandParameter = opcion,
                Margin = new Thickness(0, 0, 8, 8),
                BindingContext = opcion,
            };
            chip.SetBinding(ChipOpcion.ActivoProperty, nameof(OpcionChip.Seleccionado));
            contenedor.Children.Add(chip);
        }
    }

    // Segmento elegido: texto oscuro y en negrita sobre la píldora; el resto en texto normal
    private void PintarSegmentos()
    {
        if (_vm is null) return;
        for (var i = 0; i < _segmentos.Length; i++)
        {
            if (_segmentos[i] is not { } s) continue;
            var activo = i == _vm.IndiceOrden;
            s.FontFamily = activo ? "InterBold" : "InterSemi";
            Tema(s, Label.TextColorProperty, activo ? "AcentoTexto" : "Texto");
        }
    }

    private void MoverPildora(bool animar)
    {
        if (_vm is null || _segmentado.Width <= 0) return;
        var ancho = _segmentado.Width / 5;
        _pildora.WidthRequest = ancho;
        _pildora.HorizontalOptions = LayoutOptions.Start;
        _segmentado.SetColumnSpan(_pildora, 5);
        var destino = ancho * _vm.IndiceOrden;
        _pildora.AbortAnimation("TranslateTo");
        if (!animar || Animaciones.ReducirMovimiento || _pildora.Handler is null)
        {
            _pildora.TranslationX = destino;
            return;
        }
        _pildora.TranslateTo(destino, 0, 320, Easing.SpringOut).SinEsperar();
    }

    // t: 0 sin velo, 1 velo completo (opacidad 0.55). Las franjas de las barras del sistema siguen
    // al velo (arriba) y a la hoja (abajo) para que no queden bandas sin oscurecer
    private void NivelVelo(double t)
    {
        _nivelVelo = t;
        _velo.Opacity = OpacidadVelo * t;
        var tema = Application.Current?.RequestedTheme == AppTheme.Dark ? "Oscuro" : "Claro";
        var fondo = Recurso("Fondo" + tema);
        BarraEstado.FondoSistema(Mezclar(fondo, Colors.Black, OpacidadVelo * t), Mezclar(fondo, Recurso("Superficie" + tema), t), t);
    }

    private Task AnimarVelo(double hasta, uint duracion, Easing easing)
    {
        var tcs = new TaskCompletionSource();
        this.AbortAnimation("Velo");
        new Animation(NivelVelo, _nivelVelo, hasta, easing)
            .Commit(this, "Velo", 16, duracion, finished: (_, _) => tcs.TrySetResult());
        return tcs.Task;
    }

    private static Color Mezclar(Color a, Color b, double t) => new(
        (float)(a.Red + (b.Red - a.Red) * t),
        (float)(a.Green + (b.Green - a.Green) * t),
        (float)(a.Blue + (b.Blue - a.Blue) * t));

    // Las secciones aparecen escalonadas mientras sube la hoja
    private Task EntradaSecciones() => Animaciones.EntradaEscalonada(_secciones, 45);

    // Altura máxima: 85 % de la página (el resto se desplaza dentro)
    private void LimitarAltura()
    {
        if (Height > 0) _hoja.MaximumHeightRequest = Height * 0.85 + Prolongacion;
    }

    private void OnArrastre(object? sender, PanUpdatedEventArgs e)
    {
        if (_vm is null || _cerrando) return;
        switch (e.StatusType)
        {
            case GestureStatus.Running:
                // Hacia arriba ofrece resistencia; hacia abajo sigue al dedo
                _hoja.TranslationY = e.TotalY > 0 ? e.TotalY : e.TotalY * 0.15;
                NivelVelo(Math.Clamp(1 - _hoja.TranslationY / Math.Max(1, _hoja.Height), 0, 1));
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                if (_hoja.TranslationY > 110)
                    _vm.CerrarCommand.Execute(null);
                else if (Animaciones.ReducirMovimiento)
                {
                    _hoja.TranslationY = 0;
                    NivelVelo(1);
                }
                else
                {
                    _hoja.TranslateTo(0, 0, 260, Easing.SpringOut).SinEsperar();
                    AnimarVelo(1, 200, Easing.CubicOut).SinEsperar();
                }
                break;
        }
    }

    private static Label EtiquetaSeccion(string texto)
    {
        var etiqueta = new Label
        {
            Text = texto,
            TextTransform = TextTransform.Uppercase,
            FontFamily = "InterBold",
            FontSize = 11,
            CharacterSpacing = 1.6,
            Margin = new Thickness(0, 14, 0, 0),
        };
        Tema(etiqueta, Label.TextColorProperty, "Acento");
        return etiqueta;
    }

    private static Label TextoDelBoton(string? texto = null)
    {
        var etiqueta = new Label
        {
            Text = texto,
            FontFamily = "InterBold",
            FontSize = 16,
            VerticalOptions = LayoutOptions.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };
        Tema(etiqueta, Label.TextColorProperty, "AcentoTexto");
        return etiqueta;
    }

    // Color por token: "<nombre>Claro" / "<nombre>Oscuro" de Colors.xaml según el tema
    private static void Tema(BindableObject vista, BindableProperty propiedad, string token) =>
        vista.SetAppThemeColor(propiedad, Recurso(token + "Claro"), Recurso(token + "Oscuro"));

    private static Color Recurso(string clave) =>
        Application.Current?.Resources.TryGetValue(clave, out var valor) == true && valor is Color c ? c : Colors.Gray;

    private static Style Estilo(string clave) => (Style)Application.Current!.Resources[clave];

    // "*,Auto" -> columnas / filas
    private static ColumnDefinitionCollection Columnas(string definicion) =>
        new([.. definicion.Split(',').Select(d => new ColumnDefinition(Longitud(d)))]);

    private static RowDefinitionCollection Filas(string definicion) =>
        new([.. definicion.Split(',').Select(d => new RowDefinition(Longitud(d)))]);

    private static GridLength Longitud(string d) => d == "Auto" ? GridLength.Auto : GridLength.Star;
}

internal static class TareaExtensiones
{
    // Lanza una animación sin esperarla, registrando el error si lo hay
    public static async void SinEsperar(this Task tarea)
    {
        try { await tarea; }
        catch (Exception ex) { Debug.WriteLine($"[PanelFiltros] {ex.Message}"); }
    }
}
