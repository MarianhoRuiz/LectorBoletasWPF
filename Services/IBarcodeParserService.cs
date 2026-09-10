using LectorBoletasWPF.Models;

namespace LectorBoletasWPF.Services
{
    public interface IBarcodeParserService
    {
        BoletaInfo Parse(string rawBarcode);
        bool CanParse(string rawBarcode);
    }
}
