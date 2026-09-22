namespace ConciliacionSiapSafi.Models;

// Una fila de SIAP (Seguimiento de Planes Operativos) que trae un contrato asociado.
// Solo se incluyen las filas donde 'Número de contrato' no viene vacío.
public class ContratoSiap
{
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
    public string Nit { get; set; } = "";
    public string Proveedor { get; set; } = "";
    public decimal Comprometido { get; set; }
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
    public int CantidadTransacciones { get; set; }
    public bool YaUsadoEnAlgunaVinculacion { get; set; }
}

public class ResultadoConciliacion
{
    public string Actividad { get; set; } = "";
    public string ActividadDescripcion { get; set; } = "";
    public string CodigoSiap { get; set; } = "";
    public string CodigoSafi { get; set; } = "";       // representante de las variantes unidas, o "—"
    public decimal MontoSiap { get; set; }
    public decimal MontoSafi { get; set; }
    public string MetodoVinculo { get; set; } = "";     // "Código normalizado" | "Proveedor + monto" | "Sin vínculo"
    public string Estado { get; set; } = "";            // "Vinculado" | "Diferencia" | "Sin correspondencia"
}
