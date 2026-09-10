namespace LectorBoletasWPF.Models
{
    public class CampoDetalle
    {
        public string Nombre { get; set; } = string.Empty;
        public string ValorRaw { get; set; } = string.Empty;
        public string ValorInterpretado { get; set; } = string.Empty;
        public string Posicion { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;

        public CampoDetalle() { }

        public CampoDetalle(string nombre, string valorRaw, string valorInterpretado, string posicion, string descripcion)
        {
            Nombre = nombre;
            ValorRaw = valorRaw;
            ValorInterpretado = valorInterpretado;
            Posicion = posicion;
            Descripcion = descripcion;
        }
    }
}
