using Charlaiu.Web;
using Charlaiu.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var webAssemblyHostBuilder = WebAssemblyHostBuilder.CreateDefault(args);

webAssemblyHostBuilder.RootComponents.Add<App>("#application-root");
webAssemblyHostBuilder.RootComponents.Add<HeadOutlet>("head::after");

// Хранилище и экспортёры зарегистрированы интерфейсами —
// реализацию можно заменить, не трогая компоненты
webAssemblyHostBuilder.Services.AddScoped<ICharacterProfileRepository, BrowserLocalStorageCharacterProfileRepository>();
webAssemblyHostBuilder.Services.AddScoped<MarkdownQuestionnaireExporter>();
webAssemblyHostBuilder.Services.AddScoped<JsonQuestionnaireExporter>();
webAssemblyHostBuilder.Services.AddScoped<BrowserFileDownloadService>();

await webAssemblyHostBuilder.Build().RunAsync();
