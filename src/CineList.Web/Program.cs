using CineList.Core.Firestore;
using CineList.Web;
using CineList.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// En WASM hay un solo usuario por pestaña: los servicios son singleton (EstadoLista conserva la lista entre páginas)
builder.Services.AddSingleton(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(20) });
builder.Services.AddSingleton<IPeliculasRepository>(sp => new PeliculasRepository(sp.GetRequiredService<HttpClient>()));
builder.Services.AddSingleton<AlmacenLocal>();
builder.Services.AddSingleton<TemaService>();
builder.Services.AddSingleton<EstadoLista>();

await builder.Build().RunAsync();
