using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneInsArch
{
    public class RainbowPrinter
    {
        private static double hue = 0;
        private const double Step = 6;

        public static void WriteRainbow(string text)
        {
            foreach (char c in text)
            {
                var (r, g, b) = HsvToRgb(hue, 1.0, 1.0);
                Console.Write($"\x1b[38;2;{r};{g};{b}m{c}\x1b[0m");
                hue = (hue + Step) % 360;
            }
        }

        private static (int r, int g, int b) HsvToRgb(double h, double s, double v)
        {
            double c = v * s;
            double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
            double m = v - c;
            double r, g, b;

            if (h < 60) (r, g, b) = (c, x, 0);
            else if (h < 120) (r, g, b) = (x, c, 0);
            else if (h < 180) (r, g, b) = (0, c, x);
            else if (h < 240) (r, g, b) = (0, x, c);
            else if (h < 300) (r, g, b) = (x, 0, c);
            else (r, g, b) = (c, 0, x);

            return (
                (int)((r + m) * 255),
                (int)((g + m) * 255),
                (int)((b + m) * 255)
            );
        }
    }
}
