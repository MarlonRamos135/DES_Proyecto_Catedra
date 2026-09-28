# Documentación Técnica — Conciliación de Contratos SIAP/SAFI

Este documento explica cómo está construido el sistema, parte por parte, para que cualquiera del equipo pueda entenderlo, mantenerlo o extenderlo sin tener que leer todo el código desde cero.

## 1. ¿Qué problema resuelve?

Dirección Financiera del Ministerio de Educación maneja dos fuentes de datos:

- **SIAP**: presupuesto organizado por **Actividad**.
- **SAFI**: contratos y montos comprometidos, organizados por **Contrato** (no por Actividad), y a cargo del Ministerio de Hacienda.

El problema es que el código de contrato no viene escrito igual en ambas fuentes (prefijos con errores de digitación, sufijos distintos, códigos sin año, etc.), así que no se puede simplemente cruzar las dos tablas por ese campo tal cual viene. Este sistema lee ambos archivos, **normaliza** el código de contrato, los **vincula**, y muestra cuánto se ha comprometido por cada Actividad.

No usa base de datos: todo se procesa en memoria a partir de los dos CSV que se suben en cada ejecución.

## 2. Flujo general

```
Archivo SIAP (.csv o .xlsx) ─┐
                      ├─► Lectura y parseo ─► Normalización de código ─► Conciliación ─► Vista de resultado
Archivo SAFI (.csv o .xlsx) ─┘
```

Todo ocurre dentro de la misma petición HTTP (`POST /Conciliacion/Comparar`): no hay nada que se guarde entre una subida y otra.

## 3. Estructura de carpetas

```
ConciliacionSiapSafi/
├── Program.cs                      → arranque de la aplicación
├── Controllers/
│   └── ConciliacionController.cs   → recibe los archivos, orquesta el proceso
├── Models/
│   └── ContratoModels.cs           → todas las clases de datos (POCOs)
├── Services/
│   ├── CsvUtils.cs                 → utilidades de bajo nivel (parseo de líneas y montos)
│   ├── ExcelUtils.cs               → utilidades para leer filas de archivos XLSX
│   ├── CodigoContratoNormalizer.cs → el corazón del sistema: unifica formatos de código
│   ├── SiapCsvReader.cs            → lee el CSV de SIAP
│   ├── SafiCsvReader.cs            → lee el CSV de SAFI
│   ├── SiapXlsxReader.cs            → lee el XLSX de SIAP
│   ├── SafiXlsxReader.cs            → lee el XLSX de SAFI
│   └── ConciliacionService.cs      → agrupa, vincula y arma el resultado final
└── Views/
    └── Conciliacion/
        ├── Index.cshtml            → formulario de carga
        └── Resultado.cshtml        → tabla de resultado
```

## 4. Program.cs

Es el punto de entrada. Tres cosas relevantes:

- Sube el límite de tamaño de subida a 50 MB (`FormOptions.MultipartBodyLengthLimit`), porque el default de ASP.NET Core es muy bajo para archivos reales de gobierno.
- Registra los servicios en el contenedor de inyección de dependencias (`AddScoped`) — así el Controller los recibe listos por constructor, sin tener que instanciarlos a mano.
- Define la ruta por defecto apuntando directo a `Conciliacion/Index`, para no depender de un Home genérico.

## 5. Modelos (`Models/ContratoModels.cs`)

| Clase | Para qué sirve |
|---|---|
| `ContratoSiap` | Una fila de SIAP que **sí** tiene un contrato asociado (las que no tienen se descartan al leer). Incluye `AnioFiscal`, que se usa como respaldo cuando el código no trae año. |
| `TransaccionSafi` | Una fila cruda de SAFI, tal como viene en el archivo (una fila = un movimiento/ítem, no un contrato completo). |
| `ContratoSafiAgrupado` | El resultado de sumar todas las `TransaccionSafi` que pertenecen al mismo contrato una vez normalizado el código. Guarda además `VariantesOriginales` (todas las formas de escritura que se unieron) para trazabilidad. |
| `ResultadoConciliacion` | Una fila de la tabla final que ve el usuario: Actividad + código de ambos lados + montos + cómo se vinculó + estado. |

## 6. `CsvUtils.cs` — utilidades de parseo

Dos funciones puras, sin dependencias externas:

- **`SplitLine(linea, delimitador)`**: separa una línea de CSV respetando comillas (si un campo viene entre `"..."`, no se corta aunque el texto tenga el delimitador dentro). Los archivos reales sí traen comillas en texto libre, así que un `line.Split(';')` simple se habría roto.
- **`ParseMontoLatino(valor)`**: convierte montos con formato latino como `" $3.155.636,00 "` a `decimal`. Quita el `$`, quita los puntos (separador de miles), cambia la coma por punto (separador decimal). Si el valor es `"-"` o está vacío, devuelve `0`.

## 7. `CodigoContratoNormalizer.cs` — el componente más importante

**Problema que resuelve:** un mismo contrato aparece escrito de formas muy distintas entre archivos y hasta dentro del mismo archivo:

```
MINEDUCYT-66/2024-BIRF
MINEDCUYT-66/2024-BIRF      ← error de digitación
.MINEDUCYT-66/2024-BIRF     ← punto suelto al inicio
MINEDUCTY 152/2025 BIRF 9067 SV
```

**Cómo lo resuelve:** en vez de intentar limpiar el prefijo exacto (imposible con tantos errores de digitación distintos), usa una expresión regular que busca el patrón `número/año` **en cualquier parte del texto**, ignorando todo lo demás:

```csharp
private static readonly Regex PatronNumeroAnio = new(@"(\d{1,3})\s*/\s*(\d{4})");
```

Si lo encuentra, rellena el número con un cero a la izquierda si hace falta (`7/2026` → `07/2026`) y descarta el resto. Si no lo encuentra, devuelve el texto crudo con `TienePatron = false`, para que el resto del sistema sepa que ese código no se puede usar para vincular directamente.

**Método adicional — `NormalizarConAnioFiscal`:** en los datos reales, el 58% de los códigos de SIAP vienen *sin año* (solo `"07"`, `"100"`, etc.), porque el año ya está en la columna `Año fiscal` de esa misma fila. Este método intenta primero el patrón normal, y si no lo encuentra, reconstruye el código combinando los dígitos del código con el año fiscal de la fila. Esto es lo que sube la tasa de coincidencias de 42% a 60% sobre los datos reales que probamos.

## 8. `SiapCsvReader.cs`

Lee el CSV de SIAP con codificación `ISO-8859-1` (Latin1) — **no UTF-8**, porque así viene exportado el archivo y de otra forma las tildes salen mal. Busca las columnas por **nombre de encabezado**, no por posición fija, para que si el orden de columnas cambia en una futura exportación el código no se rompa (solo falla si falta una columna, con un mensaje claro).

Descarta cualquier fila donde `Número de contrato` venga vacío, porque esas filas no tienen nada que conciliar.

## 9. `SafiCsvReader.cs`

Mismo principio que el anterior, con una diferencia: el archivo real de SAFI trae **7 filas de título/metadatos** antes del encabezado (cosas como "INFORME DE COMPROMISOS"). El lector busca la primera fila que empiece con `"Ejercicio"` para saber dónde arranca la tabla real, en vez de asumir una posición fija.

## 10. `ConciliacionService.cs` — la lógica de negocio

Tiene dos métodos:

### `AgruparSafi`
Recorre todas las transacciones de SAFI, normaliza el código de cada una, y las agrupa **sumando el monto comprometido** de todas las que caen en el mismo código normalizado. Esto es lo que permite que `MINEDUCYT-66/2024-BIRF` y `MINEDCUYT-66/2024-BIRF` (con typo) terminen sumados en un solo grupo `66/2024`, en vez de aparecer como dos contratos distintos con montos parciales.

### `Conciliar`
Por cada contrato de SIAP, intenta vincularlo en este orden:

1. **Por código normalizado**: busca el código normalizado de SIAP (usando el respaldo de Año fiscal si hace falta) dentro de los grupos de SAFI que sí tienen patrón número/año.
2. **Por proveedor + monto** (respaldo, solo si el paso 1 no encontró nada): busca entre los grupos de SAFI que **no** tienen patrón de código un grupo con el mismo NIT de proveedor y un monto igual o muy cercano (tolerancia de 1%), ya que `Monto de Contrato` (SIAP) y `Comprometido` (SAFI) son conceptos contables distintos y rara vez coinciden exacto.

**Regla de negocio importante:** si un mismo código de contrato aplica a varias Actividades de SIAP, cada Actividad muestra el **monto completo** comprometido de SAFI — no se divide entre las Actividades. Esto fue una decisión explícita del equipo, no un supuesto técnico.

Al final, cualquier grupo de SAFI que **nunca** se haya vinculado a ninguna Actividad se agrega también al resultado, con Actividad en blanco, para que quede visible en vez de perderse silenciosamente.

**Sobre el estado "Vinculado":** solo significa que se encontró el contrato del otro lado. No compara si los montos son iguales, porque `Monto de Contrato` (el valor total adjudicado) y `Comprometido` (lo acumulado a la fecha de corte) casi nunca van a coincidir exactamente aunque el vínculo sea correcto — son dos cosas distintas, no un error.

## 11. Controller y Vistas

`ConciliacionController` es deliberadamente delgado: no tiene lógica de negocio, solo recibe los dos `IFormFile`, llama a los lectores, llama al servicio de conciliación, y pasa el resultado a la vista. Si algo falla al leer un archivo (columna faltante, archivo corrupto), captura la excepción y la muestra como mensaje de error en el formulario en vez de tumbar la aplicación.

`Index.cshtml` es el formulario de carga y acepta archivos `.csv` y `.xlsx`. `Resultado.cshtml` recibe la lista de `ResultadoConciliacion` como modelo fuertemente tipado y la pinta en una tabla, con color según el `Estado`.

## 12. Supuestos y limitaciones conocidas

Para que nadie los descubra por sorpresa en producción:

- El sistema asume que `Monto de Contrato` en SIAP y los montos de SAFI están en la misma unidad (no se aplica ninguna conversión ni división por 100). Si en algún archivo vinieran en centavos, hay que ajustarlo en `ParseMontoLatino` o en el lector de SIAP.
- Solo se probó con la hoja **"Comprometido"** de SAFI. Si se necesitan también Devengado y/o Pagado, hay que replicar `SafiCsvReader` o generalizarlo para leer varias hojas y sumarlas por separado.
- La tolerancia de 1% en el respaldo por proveedor+monto es un valor inicial razonable, pero es ajustable en `ConciliacionService.cs` si en la práctica resulta muy estricta o muy laxa.
- No hay historial: cada ejecución es independiente. Si se necesita guardar un registro de conciliaciones pasadas, hay que agregar una capa de exportación o persistencia (fuera del alcance actual).

## 13. Cómo correrlo

Ver `LEEME.txt` en la raíz del proyecto. La primera restauración requiere el paquete NuGet ClosedXML y el SDK de .NET 8.
