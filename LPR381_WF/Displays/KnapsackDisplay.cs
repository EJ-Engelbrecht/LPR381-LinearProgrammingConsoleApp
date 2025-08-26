using System;

namespace LPR381_Solver.Displays
{
    public class KnapsackDisplay
    {
        public static void Run(bool useAscii = false)
        {
            Console.WriteLine("┌────────────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ Branch & Bound Algorithm – Knapsack method                                 │");
            Console.WriteLine("└────────────────────────────────────────────────────────────────────────────┘");
            Console.WriteLine();

            Console.WriteLine("┌────────────┐");
            Console.WriteLine("│ Ratio Test │");
            Console.WriteLine("├──────┬─────┤");
            Console.WriteLine("│Item  │Rank │");
            Console.WriteLine("├──────┼─────┤");
            Console.WriteLine("│ x1   │  5  │");
            Console.WriteLine("│ x2   │  3  │");
            Console.WriteLine("│ x3   │  2  │");
            Console.WriteLine("│ x4   │  4  │");
            Console.WriteLine("│ x5   │  1  │");
            Console.WriteLine("└──────┴─────┘");
            Console.WriteLine();

            Console.WriteLine("max z =  4x1  +  2x2  +  2x3  +  x4  +  10x5");
            Console.WriteLine("s.t.    12x1  +   2x2  +  1x3  +  x4  +   4x5  ≤  15");
            Console.WriteLine("        xi = 0 or 1");
            Console.WriteLine();

            Console.WriteLine("┌─────────────── Sub-Problem ───────────────┐");
            Console.WriteLine("│ x5 = 1         15 - 4  = 11               │");
            Console.WriteLine("│ x3 = 1         11 - 1  = 10               │");
            Console.WriteLine("│ x2 = 1         10 - 2  =  8               │");
            Console.WriteLine("│ x4 = 1          8 - 1  =  7               │");
            Console.WriteLine("│ x1 = 7/12            →  remaining 7/12    │");
            Console.WriteLine("└───────────────────────────────────────────┘");
            Console.WriteLine();

            Console.WriteLine("┌─────────────── Sub-P 1: x1 = 0 ───────────────┐      ┌────────────── Sub-P 2: x1 = 1 ──────────────┐");
            Console.WriteLine("│ Sub-Problem 1                                  │      │ Sub-Problem 2                                 │");
            Console.WriteLine("│ * x1 = 0      15 - 0  = 15                     │      │ * x1 = 1      15 - 12 = 3                    │");
            Console.WriteLine("│   x5 = 1      15 - 4  = 11                     │      │   x5 = 3/4    3 - 4  (fractional)            │");
            Console.WriteLine("│   x3 = 1      11 - 1  = 10                     │      │   x3 = 0                                       │");
            Console.WriteLine("│   x2 = 1      10 - 2  =  8                     │      │   x2 = 0                                       │");
            Console.WriteLine("│   x4 = 1       8 - 1  =  7                     │      │   x4 = 0                                       │");
            Console.WriteLine("│ z = 10 + 2 + 2 + 1 = 15                        │      │                                                │");
            Console.WriteLine("│ Candidate A                                    │      └──────────────────────────────────────────────┘");
            Console.WriteLine("│ Best Candidate                                 │");
            Console.WriteLine("└────────────────────────────────────────────────┘");
            Console.WriteLine();

            Console.WriteLine("┌────────────── Sub-P 2.1: x5 = 0 ──────────────┐      ┌────────────── Sub-P 2.2: x5 = 1 ──────────────┐");
            Console.WriteLine("│ Sub-Problem 1                                  │      │ Sub-Problem 2                                 │");
            Console.WriteLine("│ * x1 = 1      15 - 12 = 3                      │      │ * x1 = 1      15 - 12 = 3                    │");
            Console.WriteLine("│ * x5 = 0       3 - 0  = 3                      │      │ * x5 = 1       3 - 4  < 0                    │");
            Console.WriteLine("│   x3 = 1       3 - 1  = 2                      │      │   x3 = 0                                       │");
            Console.WriteLine("│   x2 = 1       2 - 2  = 0                      │      │   x2 = 0                                       │");
            Console.WriteLine("│   x4 = 0       stays 0                         │      │   x4 = 0                                       │");
            Console.WriteLine("│ z = 4 + 2 + 2 = 8                              │      │ Infeasible                                     │");
            Console.WriteLine("│ Candidate B                                    │      └──────────────────────────────────────────────┘");
            Console.WriteLine("└────────────────────────────────────────────────┘");
            Console.WriteLine();

            Console.WriteLine("Current best candidate: Candidate A with z = 15.");
        }
    }
}