using System;
using System.Text;

namespace LPR381_Solver.Displays
{
    public class PrimalSimplexDisplay
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

            ShowPrimalSimplexSolution();
        }

        static void ShowPrimalSimplexSolution()
        {
            // Title
            Console.WriteLine("Primal Simplex Solution");
            Console.WriteLine();

            // Initial tag
            Console.WriteLine("                                                        [ Initial ]");
            Console.WriteLine();

            // T-1 (Initial tableau)
            ShowTableau("T-1", isInitial: true);
            
            Console.WriteLine();
            Console.WriteLine("Entering: x2 (most negative reduced cost = -5)");
            Console.WriteLine("Leaving: row 1 (min θ = 6)");
            Console.WriteLine();

            // T-2 
            ShowTableau("T-2", isInitial: false);
            
            Console.WriteLine();
            Console.WriteLine("Entering: x1 (most negative reduced cost = -3)");
            Console.WriteLine("Leaving: row 2 (min θ = 4)");
            Console.WriteLine();

            // T-3* (Optimal)
            ShowTableau("T-3*", isOptimal: true);
            
            Console.WriteLine();
            Console.WriteLine("                    Same with B&B simplex algorithm, choose the one closest to 0.5.");
            Console.WriteLine("                    If both are the same distance, choose the lower subscript.");
        }

        static void ShowTableau(string label, bool isInitial = false, bool isOptimal = false)
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

            // Header
            Console.WriteLine($"{tl}{new string(h[0], 60)}{tr}");
            Console.WriteLine($"{v} {label.PadRight(58)} {v}");
            Console.WriteLine($"{crossDown}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{crossUp}");

            // Column headers
            Console.WriteLine($"{v}    {v} x1 {v} x2 {v} x3 {v} s1 {v} s2 {v} s3 {v} rhs{v}  θ {v}");
            Console.WriteLine($"{crossDown}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{cross}{new string(h[0], 4)}{crossUp}");

            if (label == "T-1")
            {
                // Initial tableau with entering column highlighted
                Console.WriteLine($"{v} z  {v}  4 {v}{Yellow("-8")}{v}  0 {v}  0 {v}  0 {v}  0 {v}  0 {v}    {v}");
                Console.WriteLine($"{v} s1 {v}  2 {v}{Yellow(" 1")}{v}  1 {v}  1 {v}  0 {v}  0 {v} 12 {v}{Yellow(" 12")}{v}");
                Console.WriteLine($"{v} s2 {v}  1 {v}{Yellow(" 1")}{v}  1 {v}  0 {v}  1 {v}  0 {v}  9 {v}{Yellow("  9")}{v}");
                Console.WriteLine($"{v} s3 {v}  1 {v}{Yellow(" 0")}{v}  0 {v}  0 {v}  0 {v}  1 {v}  6 {v}{Yellow("  ∞")}{v}");
            }
            else if (label == "T-2")
            {
                Console.WriteLine($"{v} z  {v}{Yellow("-3")}{v}  0 {v}  8 {v}  0 {v}  8 {v}  0 {v} 72 {v}    {v}");
                Console.WriteLine($"{v} s1 {v}{Yellow(" 1")}{v}  0 {v} -1 {v}  1 {v} -1 {v}  0 {v}  3 {v}{Yellow("  3")}{v}");
                Console.WriteLine($"{v} x2 {v}{Yellow(" 1")}{v}  1 {v}  1 {v}  0 {v}  1 {v}  0 {v}  9 {v}{Yellow("  9")}{v}");
                Console.WriteLine($"{v} s3 {v}{Yellow(" 1")}{v}  0 {v}  0 {v}  0 {v}  0 {v}  1 {v}  6 {v}{Yellow("  6")}{v}");
            }
            else if (label == "T-3*")
            {
                Console.WriteLine($"{v} z  {v}  0 {v}  0 {v}  5 {v}  3 {v}  5 {v}  0 {v} 81 {v}    {v}");
                Console.WriteLine($"{v} x1 {v}  1 {v}  0 {v} -1 {v}  1 {v} -1 {v}  0 {v}  3 {v}    {v}");
                Console.WriteLine($"{v} x2 {v}  0 {v}  1 {v}  2 {v} -1 {v}  2 {v}  0 {v}  6 {v}    {v}");
                Console.WriteLine($"{v} s3 {v}  0 {v}  0 {v}  1 {v} -1 {v}  1 {v}  1 {v}  3 {v}    {v}");
            }

            Console.WriteLine($"{bl}{new string(h[0], 60)}{br}");
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
    }
}