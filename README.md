# NubeFiscal.Efos

Cliente .NET para consultar el listado 69-B del SAT (EFOS) actualizado diariamente.

## Instalación

```bash
dotnet add package NubeFiscal.Efos
```

## Uso rápido

```csharp
// Program.cs
builder.Services.AddEfosClient();

// En tu servicio o controlador
public class MiServicio(EfosClient efos)
{
    public async Task<bool> ValidarRfc(string rfc)
        => await efos.EsEfosAsync(rfc);
}
```

## Métodos disponibles

```csharp
// Verifica si el RFC es riesgo activo (Presunto o Definitivo)
bool esRiesgo = await efos.EsEfosAsync("AAA010101AAA");

// Detalle completo — devuelve null si no está en el listado
EfosEntry? entry = (await efos.ConsultarAsync(rfc))?.Value;

// Qué RFCs entraron y salieron hoy
EfosDiff? diff = (await efos.ObtenerDiffAsync())?.Value;

// Metadata ligera sin descargar el listado completo
EfosMetadata meta = await efos.ObtenerMetadatosAsync();
```

## Cómo funciona

El cliente descarga `metadata.json` (~360 bytes) en cada consulta y compara el SHA256.
Solo descarga el listado completo (~6MB) cuando detecta un cambio. El resto del tiempo
responde desde un índice en memoria con búsqueda O(1) por RFC.

## Fuente de datos

Los datos provienen del [listado 69-B del SAT](https://www.gob.mx/sat), convertidos
diariamente a JSON por [efos-mx](https://github.com/Nube-Fiscal/efos-mx).

## Licencia

MIT © [Nube Fiscal](https://nubefiscal.com.mx)
