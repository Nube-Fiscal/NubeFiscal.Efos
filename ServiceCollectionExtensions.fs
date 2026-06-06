namespace NubeFiscal.Efos

open System
open System.Runtime.CompilerServices
open Microsoft.Extensions.DependencyInjection

[<Extension>]
type ServiceCollectionExtensions =

    /// <summary>
    /// Registra EfosClient como singleton con su HttpClient dedicado.
    /// El cache en memoria se comparte entre todos los requests del proceso.
    /// </summary>
    [<Extension>]
    static member AddEfosClient
        (services: IServiceCollection,
         ?configure: Action<EfosClientOptions>) : IServiceCollection =

        let options = EfosClientOptions()
        configure |> Option.iter (fun f -> f.Invoke(options))

        services.AddSingleton(options) |> ignore

        services
            .AddHttpClient<EfosClient>(fun client ->
                client.BaseAddress <- Uri(options.BaseUrl)
                client.DefaultRequestHeaders.Add("User-Agent", "NubeFiscal.Efos/1.0")
                client.Timeout <- TimeSpan.FromSeconds(30.0))
        |> ignore

        services.AddSingleton<EfosClient>() |> ignore
        services
