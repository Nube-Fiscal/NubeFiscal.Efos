namespace NubeFiscal.Efos

open System.Text.Json.Serialization

// ---------------------------------------------------------------------------
// EfosEntry
// ---------------------------------------------------------------------------

[<CLIMutable>]
type EfosEntry = {
    [<JsonPropertyName("rfc")>]
    Rfc: string

    [<JsonPropertyName("nombre")>]
    Nombre: string

    [<JsonPropertyName("situacion")>]
    Situacion: string

    [<JsonPropertyName("dof_presuncion")>]
    DofPresuncion: string

    [<JsonPropertyName("sat_presuncion")>]
    SatPresuncion: string

    [<JsonPropertyName("dof_definitivo")>]
    DofDefinitivo: string

    [<JsonPropertyName("sat_definitivo")>]
    SatDefinitivo: string

    [<JsonPropertyName("dof_desvirtuacion")>]
    DofDesvirtuacion: string

    [<JsonPropertyName("sat_desvirtuacion")>]
    SatDesvirtuacion: string

    [<JsonPropertyName("dof_sentencia")>]
    DofSentencia: string

    [<JsonPropertyName("sat_sentencia")>]
    SatSentencia: string
} with
    /// Presunto o Definitivo = riesgo activo. Desvirtuado/Sentencia = contribuyente rehabilitado.
    member this.EsRiesgoActivo =
        this.Situacion.Contains("Presunto",   System.StringComparison.OrdinalIgnoreCase) ||
        this.Situacion.Contains("Definitivo", System.StringComparison.OrdinalIgnoreCase)

// ---------------------------------------------------------------------------
// EfosMetadata
// ---------------------------------------------------------------------------

[<CLIMutable>]
type EfosMetadata = {
    [<JsonPropertyName("ultima_actualizacion")>]
    UltimaActualizacion: System.DateTimeOffset

    [<JsonPropertyName("total_registros")>]
    TotalRegistros: int

    [<JsonPropertyName("sha256")>]
    Sha256: string

    [<JsonPropertyName("fuente")>]
    Fuente: string

    [<JsonPropertyName("diff_disponible")>]
    DiffDisponible: string
}

// ---------------------------------------------------------------------------
// EfosDiff
// ---------------------------------------------------------------------------

[<CLIMutable>]
type EfosDiffResumen = {
    [<JsonPropertyName("total_anterior")>]
    TotalAnterior: int

    [<JsonPropertyName("total_actual")>]
    TotalActual: int

    [<JsonPropertyName("agregados")>]
    Agregados: int

    [<JsonPropertyName("eliminados")>]
    Eliminados: int
}

[<CLIMutable>]
type EfosDiff = {
    [<JsonPropertyName("fecha")>]
    Fecha: string

    [<JsonPropertyName("resumen")>]
    Resumen: EfosDiffResumen

    [<JsonPropertyName("agregados")>]
    Agregados: EfosEntry[]

    [<JsonPropertyName("eliminados")>]
    Eliminados: EfosEntry[]
}
