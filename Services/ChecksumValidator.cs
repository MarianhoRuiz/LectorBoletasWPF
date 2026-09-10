using System;

namespace LectorBoletasWPF.Services
{
    public static class ChecksumValidator
    {
        /// <summary>
        /// Calcula el dígito verificador usando el algoritmo Luhn (Módulo 10).
        /// pesoInicial define el peso del dígito más a la derecha del payload (2 o 1).
        /// </summary>
        public static int CalcularLuhn(string digitos, int pesoInicial = 2)
        {
            if (string.IsNullOrWhiteSpace(digitos))
                return 0;

            int suma = 0;
            int peso = pesoInicial;

            for (int i = digitos.Length - 1; i >= 0; i--)
            {
                if (!char.IsDigit(digitos[i]))
                    continue;

                int valor = digitos[i] - '0';
                int producto = valor * peso;

                if (producto > 9)
                    producto -= 9;

                suma += producto;
                peso = (peso == 2) ? 1 : 2;
            }

            int resto = suma % 10;
            return (resto == 0) ? 0 : 10 - resto;
        }

        /// <summary>
        /// Valida si el último dígito del código coincide con el cálculo de Luhn.
        /// </summary>
        public static bool ValidarLuhn(string codigoCompleto, int pesoInicial = 2)
        {
            if (string.IsNullOrWhiteSpace(codigoCompleto) || codigoCompleto.Length < 2)
                return false;

            string baseCodigo = codigoCompleto[..^1];
            int dvEsperado = codigoCompleto[^1] - '0';
            int dvCalculado = CalcularLuhn(baseCodigo, pesoInicial);

            return dvEsperado == dvCalculado;
        }

        /// <summary>
        /// Calcula el doble dígito verificador SEPSA (especificado en la documentación técnica de Pago Fácil).
        /// Secuencia de pesos: 1, 3, 5, 7, 9 y luego repite 3, 5, 7, 9.
        /// Suma dividida por 2, y luego resto de división por 10.
        /// </summary>
        public static string CalcularSepsaDobleVerificador(string digitos)
        {
            if (string.IsNullOrWhiteSpace(digitos))
                return "00";

            int dv1 = CalcularPasoSepsa(digitos);
            int dv2 = CalcularPasoSepsa(digitos + dv1);

            return $"{dv1}{dv2}";
        }

        private static int CalcularPasoSepsa(string cadena)
        {
            int[] pesosIniciales = { 1, 3, 5, 7, 9 };
            int[] pesosRepetidos = { 3, 5, 7, 9 };

            long suma = 0;
            for (int i = 0; i < cadena.Length; i++)
            {
                if (!char.IsDigit(cadena[i]))
                    continue;

                int digito = cadena[i] - '0';
                int peso;

                if (i < pesosIniciales.Length)
                {
                    peso = pesosIniciales[i];
                }
                else
                {
                    peso = pesosRepetidos[(i - pesosIniciales.Length) % pesosRepetidos.Length];
                }

                suma += (long)digito * peso;
            }

            long mitad = suma / 2;
            return (int)(mitad % 10);
        }

        /// <summary>
        /// Valida si los últimos 2 dígitos coinciden con la verificación SEPSA.
        /// </summary>
        public static bool ValidarSepsa(string codigoCompleto)
        {
            if (string.IsNullOrWhiteSpace(codigoCompleto) || codigoCompleto.Length < 3)
                return false;

            string baseCodigo = codigoCompleto[..^2];
            string dvEsperado = codigoCompleto[^2..];
            string dvCalculado = CalcularSepsaDobleVerificador(baseCodigo);

            return dvEsperado == dvCalculado;
        }
    }
}
