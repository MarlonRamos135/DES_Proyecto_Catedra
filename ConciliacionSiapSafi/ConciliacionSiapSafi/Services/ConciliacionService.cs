using ConciliacionSiapSafi.Models;

namespace ConciliacionSiapSafi.Services;

public class ConciliacionService
{
    private readonly CodigoContratoNormalizer _normalizer;

    public ConciliacionService(CodigoContratoNormalizer normalizer)
    {
        _normalizer = normalizer;
    }

    // Agrupa las transacciones de SAFI por código normalizado, uniendo variantes con
    // errores de digitación que caen en el mismo número/año (ej. "MINEDUCTY 152/2025 BIRF 9067 SV"
    // y "MINEDUCYT-152/2025-BIRF" se funden en un solo grupo "152/2025").
    // Las que no tienen patrón número/año se agrupan por su texto crudo tal cual.
    public List<ContratoSafiAgrupado> AgruparSafi(List<TransaccionSafi> transacciones)
    {
        var grupos = new Dictionary<string, ContratoSafiAgrupado>();

        foreach (var t in transacciones)
        {
            var norm = _normalizer.Normalizar(t.CodigoOriginal);
            var clave = norm.CodigoNormalizado;

            if (!grupos.TryGetValue(clave, out var grupo))
            {
                grupo = new ContratoSafiAgrupado
                {
                    CodigoNormalizado = norm.CodigoNormalizado,
                    TienePatronNumeroAnio = norm.TienePatron,
                    Nit = t.Nit,
                    Proveedor = t.Proveedor,
                    NoCompro = t.NoCompro,
                    NoDocuOrig = t.NoDocuOrig,
                    FF = t.FF,
                    Proy = t.Proy,
                    FR = t.FR,
                    AO = t.AO
                };
                grupos[clave] = grupo;
            }

            if (!grupo.VariantesOriginales.Contains(t.CodigoOriginal))
                grupo.VariantesOriginales.Add(t.CodigoOriginal);

            grupo.Comprometidos.Add(t.Comprometido);
            grupo.TotalComprometido += t.Comprometido;
            grupo.Descomp += t.Descomp;
            grupo.Devengado += t.Devengado;
            grupo.Pagado += t.Pagado;
            if (!string.IsNullOrWhiteSpace(t.NoCompro)) grupo.NoCompro = t.NoCompro;
            if (!string.IsNullOrWhiteSpace(t.NoDocuOrig))
                grupo.NoDocuOrig = string.Join(" / ", new[] { grupo.NoDocuOrig, t.NoDocuOrig }
                    .SelectMany(x => x.Split(" / ", StringSplitOptions.RemoveEmptyEntries))
                    .Distinct(StringComparer.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(t.Concepto))
            {
                var conceptos = (grupo.Concepto + "|" + t.Concepto).Split('|', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim('(', ')'))
                    .Distinct(StringComparer.OrdinalIgnoreCase);
                grupo.Concepto = string.Join(" ", conceptos.Select(x => $"({x})"));
            }
            grupo.CantidadTransacciones++;
        }

        return grupos.Values.ToList();
    }

    public List<ResultadoConciliacion> Conciliar(List<ContratoSiap> contratosSiap, List<TransaccionSafi> transaccionesSafi)
    {
        var gruposSafi = AgruparSafi(transaccionesSafi);

        // Índice por código normalizado, solo de los grupos CON patrón número/año.
        var porCodigo = gruposSafi
            .Where(g => g.TienePatronNumeroAnio)
            .ToDictionary(g => g.CodigoNormalizado, g => g);

        // Grupos SIN patrón: se resuelven por Nit + monto (respaldo acordado).
        var sinPatron = gruposSafi.Where(g => !g.TienePatronNumeroAnio).ToList();

        var resultado = new List<ResultadoConciliacion>();

        foreach (var s in contratosSiap)
        {
            var normSiap = _normalizer.NormalizarConAnioFiscal(s.NumeroContratoOriginal, s.AnioFiscal);

            ContratoSafiAgrupado? match = null;
            string metodo = "Sin vínculo";

            // 1) Intento por código normalizado
            if (normSiap.TienePatron && porCodigo.TryGetValue(normSiap.CodigoNormalizado, out var porCod))
            {
                match = porCod;
                metodo = "Código normalizado";
            }

            if (match != null) match.YaUsadoEnAlgunaVinculacion = true;

            resultado.Add(new ResultadoConciliacion
            {
                Actividad = s.Actividad,
                ActividadDescripcion = s.ActividadDescripcion,
                CodigoSiap = s.NumeroContratoOriginal,
                CodigoSafi = match != null ? string.Join(" / ", match.VariantesOriginales) : "—",
                MontoSiap = s.MontoContrato,
                MontoSafi = match?.TotalComprometido ?? 0,
                MetodoVinculo = metodo,
                // Nota: 'Monto de Contrato' (SIAP) es el valor total adjudicado, y 'Comprometido'
                // (SAFI) es lo comprometido acumulado a la fecha del corte — casi nunca son iguales
                // aunque el contrato esté correctamente vinculado, así que ya no se usan como
                // criterio de error. 'Vinculado' significa que se encontró el contrato; la diferencia
                // entre montos es información para Dirección Financiera, no un estado de fallo.
                Estado = match == null ? "Sin correspondencia" : "Vinculado"
                ,ColumnasSiap = s.Columnas
                ,NoCompro = match?.NoCompro ?? ""
                ,NoDocuOrig = match?.NoDocuOrig ?? ""
                ,NoDResp = match?.VariantesOriginales.FirstOrDefault() ?? ""
                ,Nit = match?.Nit ?? ""
                ,Proveedor = match?.Proveedor ?? ""
                ,FF = match?.FF ?? ""
                ,Proy = match?.Proy ?? ""
                ,FR = match?.FR ?? ""
                ,AO = match?.AO ?? ""
                ,Comprometido = match?.TotalComprometido ?? 0
                ,ComprometidoSafi = match?.TotalComprometido ?? 0
                ,Descomp = match?.Descomp ?? 0
                ,Concepto = match?.Concepto ?? ""
                ,Devengado = match?.Devengado ?? 0
                ,Pagado = match?.Pagado ?? 0
            });
        }

        // Grupos de SAFI que nunca se vincularon a ninguna Actividad de SIAP: se listan aparte,
        // sin Actividad asignada, para que Dirección Financiera los revise.
        foreach (var g in gruposSafi.Where(g => !g.YaUsadoEnAlgunaVinculacion))
        {
            resultado.Add(new ResultadoConciliacion
            {
                Actividad = "—",
                ActividadDescripcion = "(sin Actividad asociada en SIAP)",
                CodigoSiap = "—",
                CodigoSafi = string.Join(" / ", g.VariantesOriginales),
                MontoSiap = 0,
                MontoSafi = g.TotalComprometido,
                MetodoVinculo = "Sin vínculo",
                Estado = "Sin correspondencia"
                ,NoDResp = string.Join(" / ", g.VariantesOriginales)
                ,Nit = g.Nit
                ,Proveedor = g.Proveedor
                ,NoCompro = g.NoCompro
                ,NoDocuOrig = g.NoDocuOrig
                ,FF = g.FF
                ,Proy = g.Proy
                ,FR = g.FR
                ,AO = g.AO
                ,Comprometido = g.TotalComprometido
                ,ComprometidoSafi = g.TotalComprometido
                ,Descomp = g.Descomp
                ,Concepto = g.Concepto
                ,Devengado = g.Devengado
                ,Pagado = g.Pagado
            });
        }

        return resultado;
    }
}
