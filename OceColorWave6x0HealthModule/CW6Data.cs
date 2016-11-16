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

        public static IEnumerable<byte> ToBytesNaiveEncoding(this string str)
        {
            // note: naive encoding, not native encoding
            return str.Select(c => (byte) c);
        }
    }
}
