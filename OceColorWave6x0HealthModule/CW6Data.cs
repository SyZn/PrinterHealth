using System.Collections.Generic;
using System.Linq;

namespace OceColorWave6x0HealthModule
{
    static class CW6Data
    {
        public const string KeepWarmJob =
            "\u001B%-12345X@PJL JOB NAME = Oce TCS 400\n\n" +
            "\u001B%0B" +
            "IN;" +
            "SP1;" +
            "PU;" +
            "PA0,0;" +
            "BP5,2;" +
            "CO\"keep-warm\";" +
            "PS;" +
            "RO90;" +
            "\u001B%1A" +
            "\u001B*t600R" +
            "\u001B*v1N" +
            "\u001B*r-4U" +
            "\u001B&a0N" +
            "\u001B*r32S" +
            "\u001B*r1T" +
            "\u001B*b2M" +
            "\u001B*r1A" +
            "\u001B*b0W" +
            "\u001B*rC" +
            "\u001B%0B" +
            "PG;"
        ;

        public const string KeepWarmJobUpload =
            "--{0}\r\n" +
            "Content-Disposition: form-data; name=\"measurementUnit\"\r\n" +
            "\r\n" +
            "METRIC\r\n" +

            "--{0}\r\n" +
            "Content-Disposition: form-data; name=\"medium_auto\"\r\n" +
            "\r\n" +
            "anyMediaType\r\n" +

            "--{0}\r\n" +
            "Content-Disposition: form-data; name=\"medium_no_zoom\"\r\n" +
            "\r\n" +
            "anyMediaType\r\n" +

            "--{0}\r\n" +
            "Content-Disposition: form-data; name=\"scale\"\r\n" +
            "\r\n" +
            "NO_ZOOM\r\n" +

            "--{0}\r\n" +
            "Content-Disposition: form-data; name=\"flip_image\"\r\n" +
            "\r\n" +
            "flip_image_no\r\n" +

            "--{0}\r\n" +
            "Content-Disposition: form-data; name=\"orientation\"\r\n" +
            "\r\n" +
            "AUTO\r\n" +

            "--{0}\r\n" +
            "Content-Disposition: form-data; name=\"printmode\"\r\n" +
            "\r\n" +
            "auto\r\n" +

            "--{0}\r\n" +
            "Content-Disposition: form-data; name=\"colourmode\"\r\n" +
            "\r\n" +
            "COLOUR\r\n" +

            "--{0}\r\n" +
            "Content-Disposition: form-data; name=\"alignment\"\r\n" +
            "\r\n" +
            "TOP_RIGHT\r\n" +

            "--{0}\r\n" +
            "Content-Disposition: form-data; name=\"horizontalShift\"\r\n" +
            "\r\n" +
            "0\r\n" +

            "--{0}\r\n" +
            "Content-Disposition: form-data; name=\"verticalShift\"\r\n" +
            "\r\n" +
            "0\r\n" +

            "--{0}\r\n" +
            "Content-Disposition: form-data; name=\"cutsize\"\r\n" +
            "\r\n" +
            "SYNCHRO\r\n" +

            "--{0}\r\n" +
            "Content-Disposition: form-data; name=\"addLeadingStrip\"\r\n" +
            "\r\n" +
            "0\r\n" +

            "--{0}\r\n" +
            "Content-Disposition: form-data; name=\"addTrailingStrip\"\r\n" +
            "\r\n" +
            "0\r\n" +

            "--{0}\r\n" +
            "Content-Disposition: form-data; name=\"sheetDelivery\"\r\n" +
            "\r\n" +
            "TDT\r\n" +

            "--{0}\r\n" +
            "Content-Disposition: form-data; name=\"jobId\"\r\n" +
            "\r\n" +
            "\r\n" +

            "--{0}\r\n" +
            "Content-Disposition: form-data; name=\"docboxName\"\r\n" +
            "\r\n" +
            "Public\r\n" +

            "--{0}\r\n" +
            "Content-Disposition: form-data; name=\"directPrint\"\r\n" +
            "\r\n" +
            "directPrint\r\n" +

            "--{0}\r\n" +
            "Content-Disposition: form-data; name=\"hidden_directPrint\"\r\n" +
            "\r\n" +
            "true\r\n" +

            "--{0}\r\n" +
            "Content-Disposition: form-data; name=\"userName\"\r\n" +
            "\r\n" +
            "PRINTERHEALTH\r\n" +

            "--{0}\r\n" +
            "Content-Disposition: form-data; name=\"nrOfCopies\"\r\n" +
            "\r\n" +
            "1\r\n" +

            "--{0}\r\n" +
            "Content-Disposition: form-data; name=\"collate\"\r\n" +
            "\r\n" +
            "on\r\n" +

            "--{0}\r\n" +
            "Content-Disposition: form-data; name=\"uploadedFilenames\"\r\n" +
            "\r\n" +
            "KEEPWARM\r\n" +

            "--{0}\r\n" +
            "Content-Disposition: form-data; name=\"jobnameInput\"\r\n" +
            "\r\n" +
            "KEEPWARM\r\n" +

            "--{0}\r\n" +
            "Content-Disposition: form-data; name=\"uploadedFileIds\"\r\n" +
            "\r\n" +
            "file_0\r\n" +

            "--{0}\r\n" +
            "Content-Disposition: form-data; name=\"file_0\"; filename=\"KEEPWARM\"\r\n" +
            "Content-Type: application/octet-stream\r\n" +
            "\r\n" +
            "{1}\r\n" +

            "--{0}--\r\n"
        ;

        public static IEnumerable<byte> ToBytesNaiveEncoding(this string str)
        {
            // note: naive encoding, not native encoding
            return str.Select(c => (byte) c);
        }
    }
}
