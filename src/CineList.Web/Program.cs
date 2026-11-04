using CineList.Core.Firestore;
using CineList.Web;
using CineList.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(20) });
builder.Services.AddScoped<IPeliculasRepository, PeliculasRepository>();
builder.Services.AddScoped<AlmacenLocal>();
builder.Services.AddScoped<TemaService>();

await builder.Build().RunAsync();
