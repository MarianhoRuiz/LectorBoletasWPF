using System;
using System.Collections.Generic;
using System.Globalization;
using LectorBoletasWPF.Models;

namespace LectorBoletasWPF.Services
{
    public class BarcodeParserService : IBarcodeParserService
    {
        private static readonly CultureInfo CultureEsAr = new("es-AR");

        public bool CanParse(string rawBarcode)
        {
            if (string.IsNullOrWhiteSpace(rawBarcode))
                return false;

            string clean = NormalizarCodigo(rawBarcode);
            return clean.StartsWith("0100") || clean.StartsWith("654") || clean.StartsWith("0654");
        }

        public BoletaInfo Parse(string rawBarcode)
        {
            var info = new BoletaInfo();

            if (string.IsNullOrWhiteSpace(rawBarcode))
            {
                info.EsValido = false;
                info.MensajeError = "El código de barras ingresado está vacío.";
                return info;
            }

            string clean = NormalizarCodigo(rawBarcode);
            info.CodigoBarrasOriginal = clean;

            // Verificar que sean todos dígitos
            foreach (char c in clean)
            {
                if (!char.IsDigit(c))
                {
                    info.EsValido = false;
                    info.MensajeError = "El código contiene caracteres no numéricos.";
                    return info;
                }
            }

            // 1. Identificar si es EDET
            if (clean.StartsWith("0100"))
            {
                info.TipoEmpresa = TipoEmpresa.EDET;
                info.NombreEmpresa = "EDET S.A.";
                info.SubtituloEmpresa = "Empresa de Distribución Eléctrica de Tucumán S.A.";
                info.CodigoEmpresa = "0100";

                if (clean.Length == 26)
                {
                    return ParseEdet26(clean, info);
                }
                else if (clean.Length == 42)
                {
                    return ParseSepsaEstandar(clean, info);
                }
                else
                {
                    info.EsValido = false;
                    info.MensajeError = $"Longitud de código para EDET no reconocida ({clean.Length} caracteres). Se esperaba 26 o 42 dígitos.";
                    return info;
                }
            }

            // 2. Identificar si es SAT
            if (clean.StartsWith("654") || clean.StartsWith("0654"))
            {
                info.TipoEmpresa = TipoEmpresa.SAT;
                info.NombreEmpresa = "Sociedad Aguas del Tucumán";
                info.SubtituloEmpresa = "SAT SAPEM - Servicio de Agua Potable y Cloacas";
                info.CodigoEmpresa = clean.StartsWith("6540") ? "6540" : "654";

                // Formato real de la boleta SAT (42 caracteres con 2 vencimientos y factura de 12 dígitos)
                if (clean.Length == 42 && clean.StartsWith("654"))
                {
                    // Comprobar si corresponde al formato de 2 vencimientos con importe completo
                    // Empresa (3) + Imp1 (8) + Fec1 (5) + Factura (12) + Imp2 (8) + Fec2 (5) + DV (1) = 42
                    if (EsFormatoSatDobleVto(clean))
                    {
                        return ParseSatDobleVto42(clean, info);
                    }
                    else
                    {
                        return ParseSepsaEstandar(clean, info);
                    }
                }
                else if (clean.Length == 42)
                {
                    return ParseSepsaEstandar(clean, info);
                }
                else
                {
                    info.EsValido = false;
                    info.MensajeError = $"Longitud de código para SAT no reconocida ({clean.Length} caracteres). Se esperaba 42 dígitos.";
                    return info;
                }
            }

            // Empresa no identificada
            info.TipoEmpresa = TipoEmpresa.Desconocida;
            info.NombreEmpresa = "Empresa No Identificada";
            info.EsValido = false;
            info.MensajeError = "El código ingresado no corresponde a EDET (prefijo 0100) ni a SAT (prefijo 654 / 6540). Solo se admiten estas dos empresas.";
            return info;
        }

        #region Parser EDET (26 dígitos)

        private static BoletaInfo ParseEdet26(string code, BoletaInfo info)
        {
            info.FormatoDetectado = "Código de Barras Talón EDET (26 dígitos)";
            info.EsValido = true;

            string codEmpresa = code[..4];          // 0..3: 0100
            string numServicio = code.Substring(4, 6); // 4..9: 738916 (6 dígitos estándar de servicio EDET)
            string numFactura = code.Substring(10, 6); // 10..15: 484757 (6 dígitos comprobante)
            string codTipo = code.Substring(16, 1);    // 16: 8 (código de control/tarifa)
            string importeRaw = code.Substring(17, 8); // 17..24: 00028281 (6 enteros, 2 decimales)
            string dvOriginal = code.Substring(25, 1); // 25: 7 (dígito verificador)

            // Importe
            decimal importe = decimal.Parse(importeRaw, CultureInfo.InvariantCulture) / 100m;
            info.ImportePrincipal = importe;
            info.ImportePrincipalStr = string.Format(CultureEsAr, "$ {0:N2}", importe);
            info.IdentificadorClienteFactura = $"Servicio: {numServicio} | Factura: {numFactura}";

            // Validación Dígito Verificador (Luhn / Módulo 10 con peso inicial 1)
            int dvCalculado = ChecksumValidator.CalcularLuhn(code[..25], pesoInicial: 1);
            info.DigitoVerificadorOriginal = dvOriginal;
            info.DigitoVerificadorCalculado = dvCalculado.ToString();
            info.EsDigitoVerificadorValido = (dvOriginal == dvCalculado.ToString());

            info.MensajeValidacion = info.EsDigitoVerificadorValido
                ? "Dígito verificador válido (Algoritmo Luhn Mod-10 correcto)."
                : $"Dígito verificador inconsistente (Esperado: {dvCalculado}, Leído: {dvOriginal}).";

            // Detalle de campos
            info.Campos = new List<CampoDetalle>
            {
                new("Empresa de Servicio", codEmpresa, "EDET S.A. (Empresa de Distribución Eléctrica de Tucumán)", "Pos 1-4 (4 chars)", "Identificación asignada a EDET"),
                new("Número de Servicio", numServicio, numServicio, "Pos 5-10 (6 chars)", "Identificador de suministro/servicio del cliente"),
                new("Número de Factura / Comprobante", numFactura, numFactura, "Pos 11-16 (6 chars)", "Número de comprobante emitido"),
                new("Código de Control / Tipo", codTipo, codTipo, "Pos 17 (1 char)", "Código interno de liquidación/tarifa"),
                new("Importe a Pagar", importeRaw, string.Format(CultureEsAr, "$ {0:N2}", importe), "Pos 18-25 (8 chars)", "6 enteros y 2 decimales en moneda nacional"),
                new("Dígito Verificador (DV)", dvOriginal, $"{dvOriginal} ({(info.EsDigitoVerificadorValido ? "Válido" : "Inconsistente")})", "Pos 26 (1 char)", "Control de integridad Módulo 10 (Luhn)")
            };

            return info;
        }

        #endregion

        #region Parser SAT Doble Vencimiento (42 dígitos)

        private static bool EsFormatoSatDobleVto(string code)
        {
            // Verifica si el segmento de fecha 1 es una fecha juliana válida AADDD
            if (code.Length != 42) return false;

            string fec1 = code.Substring(11, 5);
            string fec2 = code.Substring(36, 5);

            return EsFechaJulianaValida(fec1) && EsFechaJulianaValida(fec2);
        }

        private static BoletaInfo ParseSatDobleVto42(string code, BoletaInfo info)
        {
            info.FormatoDetectado = "Boleta SAT SAPEM (42 dígitos con doble vencimiento)";
            info.EsValido = true;

            string codEmpresa = code[..3];              // 0..2: 654
            info.CodigoEmpresa = codEmpresa;
            string imp1Raw = code.Substring(3, 8);      // 3..10: 00040560 ($ 405.60)
            string fec1Raw = code.Substring(11, 5);     // 11..15: 18319 (15/11/2018)
            string facturaRaw = code.Substring(16, 12); // 16..27: 001007814047 (Factura 0010-07814047)
            string imp2Raw = code.Substring(28, 8);     // 28..35: 00040560 ($ 405.60)
            string fec2Raw = code.Substring(36, 5);     // 36..40: 18334 (30/11/2018)
            string dvOriginal = code.Substring(41, 1);  // 41: 8

            // 1° Vencimiento
            decimal imp1 = decimal.Parse(imp1Raw, CultureInfo.InvariantCulture) / 100m;
            info.ImportePrincipal = imp1;
            info.ImportePrincipalStr = string.Format(CultureEsAr, "$ {0:N2}", imp1);

            DateTime? d1 = ParseFechaJuliana(fec1Raw);
            info.FechaVencimiento1 = d1;
            info.FechaVencimiento1Str = d1.HasValue ? d1.Value.ToString("dd/MM/yyyy") : fec1Raw;

            // Factura formateada
            string facturaFormateada = (facturaRaw.Length == 12)
                ? $"{facturaRaw[..4]}-{facturaRaw[4..]}"
                : facturaRaw;
            info.IdentificadorClienteFactura = $"Factura: {facturaFormateada}";

            // 2° Vencimiento
            decimal imp2 = decimal.Parse(imp2Raw, CultureInfo.InvariantCulture) / 100m;
            info.ImporteSegundoVencimiento = imp2;
            info.ImporteSegundoVencimientoStr = string.Format(CultureEsAr, "$ {0:N2}", imp2);

            DateTime? d2 = ParseFechaJuliana(fec2Raw);
            info.FechaVencimiento2 = d2;
            info.FechaVencimiento2Str = d2.HasValue ? d2.Value.ToString("dd/MM/yyyy") : fec2Raw;

            // Validación Luhn en los primeros 41 dígitos (peso inicial 2)
            int dvCalculado = ChecksumValidator.CalcularLuhn(code[..41], pesoInicial: 2);
            info.DigitoVerificadorOriginal = dvOriginal;
            info.DigitoVerificadorCalculado = dvCalculado.ToString();
            info.EsDigitoVerificadorValido = (dvOriginal == dvCalculado.ToString());

            info.MensajeValidacion = info.EsDigitoVerificadorValido
                ? "Dígito verificador válido (Algoritmo Luhn Mod-10 correcto)."
                : $"Dígito verificador inconsistente (Esperado: {dvCalculado}, Leído: {dvOriginal}).";

            // Detalle de campos
            info.Campos = new List<CampoDetalle>
            {
                new("Empresa de Servicio", codEmpresa, "SAT (Sociedad Aguas del Tucumán SAPEM)", "Pos 1-3 (3 chars)", "Identificador de entidad asignado a SAT"),
                new("Importe 1° Vencimiento", imp1Raw, string.Format(CultureEsAr, "$ {0:N2}", imp1), "Pos 4-11 (8 chars)", "6 enteros y 2 decimales"),
                new("Fecha 1° Vencimiento", fec1Raw, info.FechaVencimiento1Str, "Pos 12-16 (5 chars)", "Formato AADDD (Año y día juliano transcurrido)"),
                new("Número de Factura / Liquidación", facturaRaw, facturaFormateada, "Pos 17-28 (12 chars)", "Punto de venta y número correlativo de factura"),
                new("Importe 2° Vencimiento", imp2Raw, string.Format(CultureEsAr, "$ {0:N2}", imp2), "Pos 29-36 (8 chars)", "6 enteros y 2 decimales para segundo plazo"),
                new("Fecha 2° Vencimiento", fec2Raw, info.FechaVencimiento2Str, "Pos 37-41 (5 chars)", "Formato AADDD (Año y día juliano segundo plazo)"),
                new("Dígito Verificador (DV)", dvOriginal, $"{dvOriginal} ({(info.EsDigitoVerificadorValido ? "Válido" : "Inconsistente")})", "Pos 42 (1 char)", "Control de integridad Módulo 10 (Luhn)")
            };

            return info;
        }

        #endregion

        #region Parser SEPSA Estándar (42 dígitos con recargo y doble verificación)

        private static BoletaInfo ParseSepsaEstandar(string code, BoletaInfo info)
        {
            info.FormatoDetectado = "Diseño Estándar SEPSA / Pago Fácil (42 dígitos)";
            info.EsValido = true;

            string codEmpresa = code[..4];              // 0..3 (4 chars)
            string imp1Raw = code.Substring(4, 8);      // 4..11 (8 chars)
            string fec1Raw = code.Substring(12, 5);     // 12..16 (5 chars: AADDD)
            string clienteRaw = code.Substring(17, 14); // 17..30 (14 chars)
            string monedaRaw = code.Substring(31, 1);   // 31 (1 char: '0' = pesos)
            string rec2Raw = code.Substring(32, 6);     // 32..37 (6 chars: 4 enteros, 2 dec)
            string fec2Raw = code.Substring(38, 2);     // 38..39 (2 chars: DD diferencia días)
            string dvOriginal = code.Substring(40, 2);  // 40..41 (2 chars: doble verificación)

            // Importe 1
            decimal imp1 = decimal.Parse(imp1Raw, CultureInfo.InvariantCulture) / 100m;
            info.ImportePrincipal = imp1;
            info.ImportePrincipalStr = string.Format(CultureEsAr, "$ {0:N2}", imp1);

            // Fecha 1
            DateTime? d1 = ParseFechaJuliana(fec1Raw);
            info.FechaVencimiento1 = d1;
            info.FechaVencimiento1Str = d1.HasValue ? d1.Value.ToString("dd/MM/yyyy") : fec1Raw;

            // Cliente
            info.IdentificadorClienteFactura = clienteRaw.Trim();

            // Recargo 2 y fecha 2
            decimal rec2 = decimal.Parse(rec2Raw, CultureInfo.InvariantCulture) / 100m;
            int diasDiferencia = int.Parse(fec2Raw);

            if (d1.HasValue && diasDiferencia > 0)
            {
                DateTime d2 = d1.Value.AddDays(diasDiferencia);
                info.FechaVencimiento2 = d2;
                info.FechaVencimiento2Str = d2.ToString("dd/MM/yyyy");
            }
            else
            {
                info.FechaVencimiento2Str = diasDiferencia > 0 ? $"+{diasDiferencia} días" : "-";
            }

            decimal imp2 = imp1 + rec2;
            info.ImporteSegundoVencimiento = imp2;
            info.ImporteSegundoVencimientoStr = string.Format(CultureEsAr, "$ {0:N2}", imp2);

            // Validación SEPSA doble verificación
            string dvCalculado = ChecksumValidator.CalcularSepsaDobleVerificador(code[..40]);
            info.DigitoVerificadorOriginal = dvOriginal;
            info.DigitoVerificadorCalculado = dvCalculado;
            info.EsDigitoVerificadorValido = (dvOriginal == dvCalculado);

            info.MensajeValidacion = info.EsDigitoVerificadorValido
                ? "Doble dígito verificador SEPSA válido."
                : $"Dígitos verificadores inconsistentes (Esperado: {dvCalculado}, Leído: {dvOriginal}).";

            string monedaTexto = monedaRaw == "0" ? "Pesos Argentinos ($)" : $"Moneda Código {monedaRaw}";

            info.Campos = new List<CampoDetalle>
            {
                new("Empresa de Servicio", codEmpresa, info.NombreEmpresa, "Pos 1-4 (4 chars)", "Código asignado por SEPSA / Pago Fácil"),
                new("Importe 1° Vencimiento", imp1Raw, string.Format(CultureEsAr, "$ {0:N2}", imp1), "Pos 5-12 (8 chars)", "6 enteros y 2 decimales"),
                new("Fecha 1° Vencimiento", fec1Raw, info.FechaVencimiento1Str, "Pos 13-17 (5 chars)", "Formato AADDD (Año y día juliano transcurrido)"),
                new("Identificación de Cliente / Factura", clienteRaw, clienteRaw.Trim(), "Pos 18-31 (14 chars)", "Datos de cuenta, recibo o factura"),
                new("Moneda", monedaRaw, monedaTexto, "Pos 32 (1 char)", "Valor 0 = pesos argentinos"),
                new("Recargo 2° Vencimiento", rec2Raw, string.Format(CultureEsAr, "$ {0:N2}", rec2), "Pos 33-38 (6 chars)", "Diferencia de monto a cobrar al 2° vencimiento"),
                new("Fecha 2° Vencimiento", fec2Raw, $"{info.FechaVencimiento2Str} (+{diasDiferencia} días)", "Pos 39-40 (2 chars)", "Días entre el 1° y 2° vencimiento"),
                new("Importe Total 2° Vencimiento", "-", string.Format(CultureEsAr, "$ {0:N2}", imp2), "Calculado", "Monto base más recargo"),
                new("Dígitos Verificadores", dvOriginal, $"{dvOriginal} ({(info.EsDigitoVerificadorValido ? "Válido" : "Inconsistente")})", "Pos 41-42 (2 chars)", "Doble verificación SEPSA")
            };

            return info;
        }

        #endregion

        #region Utilidades

        public static string NormalizarCodigo(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Remueve espacios, guiones y caracteres de control comunes de pistolas lectoras
            return input.Trim()
                        .Replace(" ", "")
                        .Replace("-", "")
                        .Replace("\t", "")
                        .Replace("\r", "")
                        .Replace("\n", "");
        }

        private static bool EsFechaJulianaValida(string aaddd)
        {
            if (string.IsNullOrWhiteSpace(aaddd) || aaddd.Length != 5)
                return false;

            if (!int.TryParse(aaddd[..2], out int _) || !int.TryParse(aaddd[2..], out int ddd))
                return false;

            return ddd is >= 1 and <= 366;
        }

        private static DateTime? ParseFechaJuliana(string aaddd)
        {
            try
            {
                if (!EsFechaJulianaValida(aaddd))
                    return null;

                int aa = int.Parse(aaddd[..2]);
                int ddd = int.Parse(aaddd[2..]);

                // Asumir siglo 2000 si aa < 80, sino 1900
                int anio = (aa < 80) ? 2000 + aa : 1900 + aa;

                return new DateTime(anio, 1, 1).AddDays(ddd - 1);
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}
