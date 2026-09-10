using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LectorBoletasWPF.Models;
using LectorBoletasWPF.Services;

namespace LectorBoletasWPF
{
    public partial class MainWindow : Window
    {
        private readonly IBarcodeParserService _parserService;
        private BoletaInfo? _boletaActual;

        // Códigos reales de las muestras provistas en el requerimiento
        private const string CodigoMuestraSat = "654000405601831900100781404700040560183348";
        private const string CodigoMuestraEdet = "01007389164847578000282817";

        public MainWindow()
        {
            InitializeComponent();
            _parserService = new BarcodeParserService();

            // Focalizar el cuadro de texto al iniciar
            Loaded += (s, e) => TxtBarcode.Focus();
        }

        private void TxtBarcode_TextChanged(object sender, TextChangedEventArgs e)
        {
            string texto = TxtBarcode.Text.Trim();
            TxtPlaceholder.Visibility = string.IsNullOrEmpty(texto) ? Visibility.Visible : Visibility.Collapsed;
            TxtLongitud.Text = $"{texto.Length} caracteres";
        }

        private void TxtBarcode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ProcesarCodigo();
            }
        }

        private void BtnProcesar_Click(object sender, RoutedEventArgs e)
        {
            ProcesarCodigo();
        }

        private void ProcesarCodigo()
        {
            string codigo = TxtBarcode.Text.Trim();
            if (string.IsNullOrWhiteSpace(codigo))
            {
                MessageBox.Show("Por favor, ingrese o escanee un código de barras.", "Campo Vacío", MessageBoxButton.OK, MessageBoxImage.Information);
                TxtBarcode.Focus();
                return;
            }

            _boletaActual = _parserService.Parse(codigo);
            MostrarResultado(_boletaActual);
        }

        private void MostrarResultado(BoletaInfo boleta)
        {
            // Limpiar errores previos
            BorderError.Visibility = Visibility.Collapsed;
            BtnCopiarResumen.Visibility = Visibility.Visible;

            if (!boleta.EsValido)
            {
                // Configurar tarjeta para error
                BorderEmpresaBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF2F2"));
                TxtEmpresaIcono.Text = "⚠️";
                TxtEmpresaNombre.Text = boleta.NombreEmpresa;
                TxtEmpresaNombre.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#991B1B"));
                TxtEmpresaSubtitulo.Text = "No se pudo procesar el código";

                TxtImportePrincipal.Text = "$ 0,00";
                TxtImportePrincipal.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                TxtSubtextoImporte.Text = "Sin importe disponible";

                TxtFechaVto1.Text = "-";
                TxtFechaVto2.Text = "-";
                TxtImporteVto2.Text = "";
                TxtIdentificador.Text = "-";

                BorderValidacion.Visibility = Visibility.Collapsed;
                BorderError.Visibility = Visibility.Visible;
                TxtErrorMensaje.Text = boleta.MensajeError;

                TxtFormatoDetectado.Text = "Sin formato reconocido";
                TxtCantidadCampos.Text = "0 campos";
                GridCampos.ItemsSource = null;

                TxtStatus.Text = $"Error: {boleta.MensajeError}";
                return;
            }

            // Configurar según Empresa
            if (boleta.TipoEmpresa == TipoEmpresa.EDET)
            {
                // Verde esmeralda EDET
                BorderEmpresaBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ECFDF5"));
                TxtEmpresaIcono.Text = "⚡";
                TxtEmpresaNombre.Text = boleta.NombreEmpresa;
                TxtEmpresaNombre.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#065F46"));
                TxtEmpresaSubtitulo.Text = boleta.SubtituloEmpresa;
            }
            else if (boleta.TipoEmpresa == TipoEmpresa.SAT)
            {
                // Azul océano SAT
                BorderEmpresaBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0F2FE"));
                TxtEmpresaIcono.Text = "💧";
                TxtEmpresaNombre.Text = boleta.NombreEmpresa;
                TxtEmpresaNombre.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0369A1"));
                TxtEmpresaSubtitulo.Text = boleta.SubtituloEmpresa;
            }

            // Importe Principal
            TxtImportePrincipal.Text = boleta.ImportePrincipalStr;
            TxtImportePrincipal.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));
            TxtSubtextoImporte.Text = boleta.FechaVencimiento1.HasValue ? "Importe al 1° Vencimiento" : "Importe Total a Pagar";

            // Fechas de Vencimiento
            TxtFechaVto1.Text = boleta.FechaVencimiento1Str;

            if (!string.IsNullOrEmpty(boleta.FechaVencimiento2Str) && boleta.FechaVencimiento2Str != "-")
            {
                TxtFechaVto2.Text = boleta.FechaVencimiento2Str;
                TxtImporteVto2.Text = boleta.ImporteSegundoVencimientoStr != "-" ? $"Total: {boleta.ImporteSegundoVencimientoStr}" : "";
            }
            else
            {
                TxtFechaVto2.Text = "No aplica";
                TxtImporteVto2.Text = "";
            }

            // Identificador de Factura / Cuenta
            TxtIdentificador.Text = string.IsNullOrEmpty(boleta.IdentificadorClienteFactura) ? "-" : boleta.IdentificadorClienteFactura;

            // Validación de Dígitos Verificadores
            BorderValidacion.Visibility = Visibility.Visible;
            if (boleta.EsDigitoVerificadorValido)
            {
                BorderValidacion.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ECFDF5"));
                BorderValidacion.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A7F3D0"));
                TxtValidacionIcono.Text = "✓";
                TxtValidacionIcono.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669"));
                TxtValidacionTitulo.Text = "Integridad Verificada (Válido)";
                TxtValidacionTitulo.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#065F46"));
                TxtValidacionMensaje.Text = boleta.MensajeValidacion;
                TxtValidacionMensaje.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#047857"));
            }
            else
            {
                BorderValidacion.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7"));
                BorderValidacion.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDE68A"));
                TxtValidacionIcono.Text = "⚠️";
                TxtValidacionIcono.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706"));
                TxtValidacionTitulo.Text = "Advertencia de Integridad";
                TxtValidacionTitulo.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#92400E"));
                TxtValidacionMensaje.Text = boleta.MensajeValidacion;
                TxtValidacionMensaje.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#78350F"));
            }

            // Desglose de Campos
            TxtFormatoDetectado.Text = boleta.FormatoDetectado;
            TxtCantidadCampos.Text = $"{boleta.Campos.Count} campos desglosados";
            GridCampos.ItemsSource = boleta.Campos;

            TxtStatus.Text = $"Boleta de {boleta.NombreEmpresa} procesada correctamente. Importe: {boleta.ImportePrincipalStr}";
        }

        private void BtnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            TxtBarcode.Text = string.Empty;
            TxtBarcode.Focus();

            BorderEmpresaBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3F4F6"));
            TxtEmpresaIcono.Text = "📋";
            TxtEmpresaNombre.Text = "Esperando Código...";
            TxtEmpresaNombre.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F2937"));
            TxtEmpresaSubtitulo.Text = "Ingrese un código de barras para comenzar";

            TxtImportePrincipal.Text = "$ 0,00";
            TxtImportePrincipal.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));
            TxtSubtextoImporte.Text = "Importe de 1° Vencimiento";

            TxtFechaVto1.Text = "-";
            TxtFechaVto2.Text = "-";
            TxtImporteVto2.Text = "";
            TxtIdentificador.Text = "-";

            BorderValidacion.Visibility = Visibility.Visible;
            BorderValidacion.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ECFDF5"));
            BorderValidacion.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A7F3D0"));
            TxtValidacionIcono.Text = "✓";
            TxtValidacionTitulo.Text = "Control de Integridad";
            TxtValidacionMensaje.Text = "Ingrese una boleta para validar.";

            BorderError.Visibility = Visibility.Collapsed;
            BtnCopiarResumen.Visibility = Visibility.Collapsed;

            TxtFormatoDetectado.Text = "Estructura según especificación técnica";
            TxtCantidadCampos.Text = "0 campos";
            GridCampos.ItemsSource = null;

            TxtStatus.Text = "Listo para escanear. Ingrese un código para comenzar.";
        }


        private void BtnCopiarResumen_Click(object sender, RoutedEventArgs e)
        {
            if (_boletaActual == null) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== RESUMEN DE BOLETA DE SERVICIO ===");
            sb.AppendLine($"Empresa: {_boletaActual.NombreEmpresa}");
            sb.AppendLine($"Código Barras: {_boletaActual.CodigoBarrasOriginal}");
            sb.AppendLine($"Importe a Pagar: {_boletaActual.ImportePrincipalStr}");
            if (_boletaActual.FechaVencimiento1.HasValue)
                sb.AppendLine($"1° Vencimiento: {_boletaActual.FechaVencimiento1Str}");
            if (_boletaActual.FechaVencimiento2.HasValue)
                sb.AppendLine($"2° Vencimiento: {_boletaActual.FechaVencimiento2Str} ({_boletaActual.ImporteSegundoVencimientoStr})");
            sb.AppendLine($"Identificación: {_boletaActual.IdentificadorClienteFactura}");
            sb.AppendLine($"Estado Verificación: {_boletaActual.MensajeValidacion}");
            sb.AppendLine("---------------------------------------");
            sb.AppendLine("Desglose de campos:");
            foreach (var campo in _boletaActual.Campos)
            {
                sb.AppendLine($"  - {campo.Nombre}: {campo.ValorInterpretado} (Raw: {campo.ValorRaw}, {campo.Posicion})");
            }

            Clipboard.SetText(sb.ToString());
            MessageBox.Show("Resumen copiado al portapapeles con éxito.", "Copiado", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}