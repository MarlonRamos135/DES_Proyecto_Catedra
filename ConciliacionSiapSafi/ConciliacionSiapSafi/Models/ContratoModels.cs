namespace ConciliacionSiapSafi.Models;

// Una fila de SIAP (Seguimiento de Planes Operativos) que trae un contrato asociado.
// Solo se incluyen las filas donde 'Número de contrato' no viene vacío.
public class ContratoSiap
{
    public Dictionary<string, string> Columnas { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string Actividad { get; set; } = "";              // código numérico, ej. 12229
    public string ActividadDescripcion { get; set; } = "";
    public string NumeroContratoOriginal { get; set; } = "";  // ej. "07/2026", "MINEDUCYT-29/2023", "72/2024 BIRF"
    public string AnioFiscal { get; set; } = "";              // respaldo para códigos sin año, ej. "2026"
    public decimal MontoContrato { get; set; }
    public string NitProveedor { get; set; } = "";
    public string NombreProveedor { get; set; } = "";
}

// Una transacción individual de SAFI (una fila = un item/mes de un documento responsable).
public class TransaccionSafi
{
    public string CodigoOriginal { get; set; } = "";   // valor crudo de 'No. D. Resp'
    public string NoDResp { get; set; } = "";
    public string Nit { get; set; } = "";
    public string Proveedor { get; set; } = "";
    public decimal Comprometido { get; set; }
    public string NoCompro { get; set; } = "";
    public string Compromiso { get; set; } = "";
    public string NoDocuOrig { get; set; } = "";
    public string FF { get; set; } = "";
    public string Proy { get; set; } = "";
    public string FR { get; set; } = "";
    public string AO { get; set; } = "";
    public decimal Descomp { get; set; }
    public string Concepto { get; set; } = "";
    public decimal Devengado { get; set; }
    public decimal Pagado { get; set; }
}

// SAFI agrupado: todas las transacciones que comparten el mismo código de contrato
// (después de unir variantes con errores de digitación que normalizan igual) se suman en un solo total.
public class ContratoSafiAgrupado
{
    public string CodigoNormalizado { get; set; } = "";     // ej. "66/2024", o el texto crudo si no tiene patrón
    public bool TienePatronNumeroAnio { get; set; }
    public List<string> VariantesOriginales { get; set; } = new();
    public string Nit { get; set; } = "";
    public string Proveedor { get; set; } = "";
    public decimal TotalComprometido { get; set; }
    public List<decimal> Comprometidos { get; set; } = new();
    public int CantidadTransacciones { get; set; }
    public bool YaUsadoEnAlgunaVinculacion { get; set; }
    public string NoCompro { get; set; } = "";
    public string NoDocuOrig { get; set; } = "";
    public string FF { get; set; } = "";
    public string Proy { get; set; } = "";
    public string FR { get; set; } = "";
    public string AO { get; set; } = "";
    public decimal Descomp { get; set; }
    public string Concepto { get; set; } = "";
    public decimal Devengado { get; set; }
    public decimal Pagado { get; set; }
}

public class ResultadoConciliacion
{
    public Dictionary<string, string> ColumnasSiap { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string Actividad { get; set; } = "";
    public string ActividadDescripcion { get; set; } = "";
    public string CodigoSiap { get; set; } = "";
    public string CodigoSafi { get; set; } = "";       // representante de las variantes unidas, o "—"
    public decimal MontoSiap { get; set; }
    public decimal MontoSafi { get; set; }
    public string MetodoVinculo { get; set; } = "";     // "Código normalizado" | "Proveedor + monto" | "Sin vínculo"
    public string Estado { get; set; } = "";            // "Vinculado" | "Diferencia" | "Sin correspondencia"
    public string NoCompro { get; set; } = "";
    public string NoDocuOrig { get; set; } = "";
    public string NoDResp { get; set; } = "";
    public string Nit { get; set; } = "";
    public string Proveedor { get; set; } = "";
    public string FF { get; set; } = "";
    public string Proy { get; set; } = "";
    public string FR { get; set; } = "";
    public string AO { get; set; } = "";
    public decimal Comprometido { get; set; }
    public decimal ComprometidoSafi { get; set; }
    public decimal Descomp { get; set; }
    public string Concepto { get; set; } = "";
    public decimal Devengado { get; set; }
    public decimal Pagado { get; set; }
}
