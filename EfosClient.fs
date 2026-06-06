namespace NubeFiscal.Efos

open System
open System.Collections.Generic
open System.Net.Http
open System.Net.Http.Json
open System.Runtime.InteropServices
open System.Text.Json
open System.Threading
open System.Threading.Tasks

// ---------------------------------------------------------------------------
// Opciones
// ---------------------------------------------------------------------------

type EfosClientOptions() =
    member val BaseUrl = "https://nube-fiscal.github.io/efos-mx" with get, set

// ---------------------------------------------------------------------------
// Cliente
// ---------------------------------------------------------------------------

/// <summary>
/// Consulta el listado 69-B (EFOS) publicado como JSON en GitHub Pages.
/// Polling eficiente: solo descarga el listado completo cuando el SHA256 cambia.
/// Thread-safe: comparte una sola copia en memoria entre todos los requests.
/// </summary>
type EfosClient(http: HttpClient, options: EfosClientOptions) =

    let mutable cachedHash = String.Empty
    let mutable index: Dictionary<string, EfosEntry> option = None
    let semaphore = new SemaphoreSlim(1, 1)

    let baseUrl = options.BaseUrl.TrimEnd('/')
    let normalizeRfc (rfc: string) = rfc.Trim().ToUpperInvariant()

    let loadCache (ct: CancellationToken) = task {
        let! meta = http.GetFromJsonAsync<EfosMetadata>($"{baseUrl}/metadata.json", ct)
        if isNull (box meta) then
            failwith "No se pudo obtener metadata.json"

        if cachedHash <> meta.Sha256 then
            let! stream = http.GetStreamAsync($"{baseUrl}/listado.json", ct)
            use doc = JsonDocument.Parse(stream)
            let entries = JsonSerializer.Deserialize<EfosEntry[]>(doc.RootElement.GetProperty("data"))

            if isNull (box entries) then
                failwith "No se pudo deserializar el listado"

            let idx =
                entries
                |> Array.map (fun e -> e.Rfc, e)
                |> dict
                |> Dictionary

            index <- Some idx
            cachedHash <- meta.Sha256
    }

    let ensureCache (ct: CancellationToken) = task {
        if index.IsNone then
            do! semaphore.WaitAsync(ct)
            try
                if index.IsNone then
                    do! loadCache ct
            finally
                semaphore.Release() |> ignore
    }

    // -----------------------------------------------------------------------
    // API publica - parametros opcionales compatibles con C#
    // -----------------------------------------------------------------------

    /// Retorna los metadatos actuales sin descargar el listado completo.
    member _.ObtenerMetadatosAsync
        ([<Optional; DefaultParameterValue(CancellationToken())>] ct: CancellationToken)
        : Task<EfosMetadata> =
        http.GetFromJsonAsync<EfosMetadata>($"{baseUrl}/metadata.json", ct)

    /// Verifica si el RFC esta en el listado 69-B con situacion de riesgo activo
    /// (Presunto o Definitivo). Desvirtuado/Sentencia favorable devuelve false.
    member _.EsEfosAsync
        (rfc: string,
         [<Optional; DefaultParameterValue(CancellationToken())>] ct: CancellationToken)
        : Task<bool> =
        task {
            do! ensureCache ct
            return
                match index with
                | None -> false
                | Some idx ->
                    match idx.TryGetValue(normalizeRfc rfc) with
                    | true, entry -> entry.EsRiesgoActivo
                    | _           -> false
        }

    /// Consulta un RFC. Devuelve None si no esta en el listado.
    /// Incluye cualquier situacion: presunto, definitivo, desvirtuado, sentencia favorable.
    member _.ConsultarAsync
        (rfc: string,
         [<Optional; DefaultParameterValue(CancellationToken())>] ct: CancellationToken)
        : Task<EfosEntry option> =
        task {
            do! ensureCache ct
            return
                match index with
                | None -> None
                | Some idx ->
                    match idx.TryGetValue(normalizeRfc rfc) with
                    | true, entry -> Some entry
                    | _           -> None
        }

    /// Descarga el diff de hoy. Devuelve None si aun no hay diff disponible.
    member this.ObtenerDiffAsync
        ([<Optional; DefaultParameterValue(CancellationToken())>] ct: CancellationToken)
        : Task<EfosDiff option> =
        this.ObtenerDiffAsync(DateOnly.FromDateTime(DateTime.UtcNow), ct)

    /// Descarga el diff de la fecha indicada. Devuelve None si no existe para esa fecha.
    member _.ObtenerDiffAsync
        (fecha: DateOnly,
         [<Optional; DefaultParameterValue(CancellationToken())>] ct: CancellationToken)
        : Task<EfosDiff option> =
        task {
            let fechaStr = fecha.ToString("yyyy-MM-dd")
            let url = $"{baseUrl}/diff/{fechaStr}.json"
            try
                let! diff = http.GetFromJsonAsync<EfosDiff>(url, ct)
                return if isNull (box diff) then None else Some diff
            with
            | :? HttpRequestException as ex
                when ex.StatusCode = Nullable(Net.HttpStatusCode.NotFound) ->
                return None
        }

    /// Fuerza la recarga del listado ignorando el cache en memoria.
    member _.RefrescarAsync
        ([<Optional; DefaultParameterValue(CancellationToken())>] ct: CancellationToken)
        : Task =
        task {
            do! semaphore.WaitAsync(ct)
            try
                cachedHash <- String.Empty
                index <- None
                do! loadCache ct
            finally
                semaphore.Release() |> ignore
        }
