using System;
using System.Text;

namespace LPR381_Solver.Displays
{
    public class CuttingPlaneDisplay
    {
        private static bool useAscii = false;
        private static bool useColor = true;

        public static void Run(bool asciiMode = false, bool colorMode = true)
        {
            useAscii = asciiMode;
            useColor = colorMode;
            
            if (!useAscii)
            {
                try
                {
                    Console.OutputEncoding = Encoding.UTF8;
                }
                catch
                {
                    useAscii = true;
                }
            }

            ShowCuttingPlaneAlgorithm();
        }

        static void ShowCuttingPlaneAlgorithm()
        {
            // Title
            Console.WriteLine("Cutting Plane Algorithm — Sub-Problem 1");
            Console.WriteLine();

            // Mini flow
            ShowMiniFlow();
            Console.WriteLine();

            // Show T-3* (unchanged)
            ShowOptimalTableau();
            Console.WriteLine();

            // Show cut selection
            ShowCutSelection();
            Console.WriteLine();

            // Show derivation
            ShowDerivation();
            Console.WriteLine();

            // Show new tableaux
            ShowNewTableaux();
            Console.WriteLine();

            // Final result
            Console.WriteLine("Integer optimal reached after 1 cut(s).");
        }

        static void ShowMiniFlow()
        {
            string tl = useAscii ? "+" : "┌";
            string tr = useAscii ? "+" : "┐";
            string bl = useAscii ? "+" : "└";
            string br = useAscii ? "+" : "┘";
            string h = useAscii ? "-" : "─";
            string v = useAscii ? "|" : "│";

            Console.WriteLine($"{tl}{new string(h[0], 11)}{tr}");
            Console.WriteLine($"{v} Initial   {v}");
            Console.WriteLine($"{bl}{new string(h[0], 11)}{br}");
            Console.WriteLine("     |");
            Console.WriteLine($"{tl}{new string(h[0], 11)}{tr}");
            Console.WriteLine($"{v} Cut 1: x1 {v}");
            Console.WriteLine($"{bl}{new string(h[0], 11)}{br}");
        }

        static void ShowOptimalTableau()
        {
            string tl = useAscii ? "+" : "┌";
            string tr = useAscii ? "+" : "┐";
            string bl = useAscii ? "+" : "└";
            string br = useAscii ? "+" : "┘";
            string h = useAscii ? "-" : "─";
            string v = useAscii ? "|" : "│";
            string cross = useAscii ? "+" : "┼";
            string crossDown = useAscii ? "+" : "├";
            string crossUp = useAscii ? "+" : "┤";

            Console.WriteLine($"{tl}{new string(h[0], 15)} T-3* {new string(h[0], 15)}{tr}");
            Console.WriteLine($"{crossDown}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{crossUp}");
            Console.WriteLine($"{v}    {v} x1 {v} x2 {v} s1 {v} s2 {v} s3 {v} rhs{v}");
            Console.WriteLine($"{crossDown}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{crossUp}");
            Console.WriteLine($"{v} z  {v}  0 {v}  0 {v}  3 {v}  5 {v}  0 {v} 81 {v}");
            Console.WriteLine($"{v} x1 {v}  1 {v}  0 {v}  1 {v} -1 {v}  0 {v}3.75{v}");
            Console.WriteLine($"{v} x2 {v}  0 {v}  1 {v} -1 {v}  2 {v}  0 {v}  6 {v}");
            Console.WriteLine($"{v} s3 {v}  0 {v}  0 {v} -1 {v}  1 {v}  1 {v}  3 {v}");
            Console.WriteLine($"{bl}{new string(h[0], 35)}{br}");
        }

        static void ShowCutSelection()
        {
            string tl = useAscii ? "+" : "┌";
            string tr = useAscii ? "+" : "┐";
            string bl = useAscii ? "+" : "└";
            string br = useAscii ? "+" : "┘";
            string h = useAscii ? "-" : "─";
            string v = useAscii ? "|" : "│";
            string cross = useAscii ? "+" : "┼";
            string crossDown = useAscii ? "+" : "├";
            string crossUp = useAscii ? "+" : "┤";

            Console.WriteLine($"{tl}{new string(h[0], 12)} T-3* → {new string(h[0], 12)}{tr}");
            Console.WriteLine($"{crossDown}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{crossUp}");
            Console.WriteLine($"{v}    {v} x1 {v} x2 {v} s1 {v} s2 {v} s3 {v} rhs{v}");
            Console.WriteLine($"{crossDown}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{crossUp}");
            Console.WriteLine($"{v} z  {v}  0 {v}  0 {v}  3 {v}  5 {v}  0 {v} 81 {v}");
            Console.WriteLine($"{v} x1 {v} {Orange("1")} {v} {Orange("0")} {v} {Orange("1")} {v} {Orange("-1")} {v} {Orange("0")} {v} {Orange("3.75")} {v}");
            Console.WriteLine($"{v} x2 {v}  0 {v}  1 {v} -1 {v}  2 {v}  0 {v}  6 {v}");
            Console.WriteLine($"{v} s3 {v}  0 {v}  0 {v} -1 {v}  1 {v}  1 {v}  3 {v}");
            Console.WriteLine($"{bl}{new string(h[0], 35)}{br}");
        }

        static void ShowDerivation()
        {
            Console.WriteLine("Derivation:");
            Console.WriteLine();
            Console.WriteLine("x1     -1.25 s1 + 0.25 s2 = 3 + 3/4");
            Console.WriteLine("x1     -2 s1   + 0.75 s1 + 0 s2 = 3 + 0.75");
            Console.WriteLine("x1 + 1.25 s1 - 0.25 s2 = 3.75");
            Console.WriteLine();
            Console.WriteLine(Bold("-0.75 s1 - 0.25 s2 + 0.75 ≤ 0"));
        }

        static void ShowNewTableaux()
        {
            string tl = useAscii ? "+" : "┌";
            string tr = useAscii ? "+" : "┐";
            string bl = useAscii ? "+" : "└";
            string br = useAscii ? "+" : "┘";
            string h = useAscii ? "-" : "─";
            string v = useAscii ? "|" : "│";
            string cross = useAscii ? "+" : "┼";
            string crossDown = useAscii ? "+" : "├";
            string crossUp = useAscii ? "+" : "┤";

            // T-3* + cut
            Console.WriteLine($"{tl}{new string(h[0], 15)} T-3* + cut {new string(h[0], 15)}{tr}      {tl}{new string(h[0], 17)} T-4* {new string(h[0], 17)}{tr}");
            Console.WriteLine($"{v} z | x1 x2 | s1 s2 {Cyan("s3")} | rhs | θ             {v}      {v} z | x1 x2 | s1 s2 {Cyan("s3")} | rhs | θ         {v}");
            Console.WriteLine($"{v} 0 |  0  0 |  3  5  0 |  81 |                   {v}      {v} 0 |  0  0 |  0  2 -4 |  78 |           {v}");
            Console.WriteLine($"{v} 1 |  1  0 |  1 -1  0 |3.75|                   {v}      {v} 1 |  1  0 |  0  0 -1 |   3 |           {v}");
            Console.WriteLine($"{v} 0 |  0  1 | -1  2  0 |   6 |                   {v}      {v} 0 |  0  1 | -1  2  0 |   6 |           {v}");
            Console.WriteLine($"{v} 0 |  0  0 | -1  1  1 |   3 |                   {v}      {v} 0 |  0  0 | -1  1  1 |   3 |           {v}");
            Console.WriteLine($"{v} 0 |  0  0 |-0.75-0.25 1|0.75|                   {v}      {v} 0 |  0  0 |  1  0 -1 |   0 |           {v}");
            Console.WriteLine($"{bl}{new string(h[0], 41)}{br}      {bl}{new string(h[0], 41)}{br}");
        }

        static string Yellow(string text)
        {
            if (!useColor) return $"[Y]{text}";
            return $"\u001b[33m{text}\u001b[0m"; // ANSI yellow
        }

        static string Purple(string text)
        {
            if (!useColor) return $"[P]{text}";
            return $"\u001b[35m{text}\u001b[0m"; // ANSI purple
        }

        static string Orange(string text)
        {
            if (!useColor) return $"[O]{text}";
            return $"\u001b[38;5;208m{text}\u001b[0m"; // ANSI orange
        }

        static string Cyan(string text)
        {
            if (!useColor) return $"[C]{text}";
            return $"\u001b[36m{text}\u001b[0m"; // ANSI cyan
        }

        static string Bold(string text)
        {
            if (!useColor) return $"[B]{text}";
            return $"\u001b[1m{text}\u001b[0m"; // ANSI bold
        }
    }
}