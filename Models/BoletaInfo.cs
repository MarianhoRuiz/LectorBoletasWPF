using System;
using System.Collections.Generic;

namespace LectorBoletasWPF.Models
{
    public class BoletaInfo
    {
        public string CodigoBarrasOriginal { get; set; } = string.Empty;
        public int Longitud => CodigoBarrasOriginal.Length;

        public TipoEmpresa TipoEmpresa { get; set; } = TipoEmpresa.Desconocida;
        public string NombreEmpresa { get; set; } = "Desconocida";
        public string SubtituloEmpresa { get; set; } = string.Empty;
        public string CodigoEmpresa { get; set; } = string.Empty;

        public decimal ImportePrincipal { get; set; }
        public string ImportePrincipalStr { get; set; } = "$ 0,00";

        public DateTime? FechaVencimiento1 { get; set; }
        public string FechaVencimiento1Str { get; set; } = "-";

        public decimal? ImporteSegundoVencimiento { get; set; }
        public string ImporteSegundoVencimientoStr { get; set; } = "-";

        public DateTime? FechaVencimiento2 { get; set; }
        public string FechaVencimiento2Str { get; set; } = "-";

        public string IdentificadorClienteFactura { get; set; } = string.Empty;
        public string FormatoDetectado { get; set; } = string.Empty;

        public string DigitoVerificadorCalculado { get; set; } = string.Empty;
        public string DigitoVerificadorOriginal { get; set; } = string.Empty;
        public bool EsDigitoVerificadorValido { get; set; }
        public string MensajeValidacion { get; set; } = string.Empty;

        public bool EsValido { get; set; }
        public string MensajeError { get; set; } = string.Empty;

        public List<CampoDetalle> Campos { get; set; } = new();
        public DateTime FechaEscaneo { get; set; } = DateTime.Now;
    }
}
