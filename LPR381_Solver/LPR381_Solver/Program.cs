using LPR381_Solver.Algorithms;
using LPR381_Solver.Utils;
using System;

namespace LPR381_Solver
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("LP Solver Menu");
                Console.WriteLine("1. PrimalSimplex (S1, S2)");
                Console.WriteLine("2. BranchAndBound (B1-B7)");
                Console.WriteLine("3. DualitySolver (D1, D2)");
                Console.WriteLine("4. RevisedSimplex (R1, R2)");
                Console.WriteLine("5. CuttingPlane (C1, C2)");
                Console.WriteLine("6. KnapsackBranchBound (K1, K2)");
                Console.WriteLine("7. Canonical Form Converter");
                Console.WriteLine("8. Exit");
                Console.Write("\nSelect option: ");

                string choice = Console.ReadLine();
                if (choice == "8") break;

                if (choice == "6")
                {
                    Console.Write("Enter Knapsack test case number (1 or 2): ");
                    if (int.TryParse(Console.ReadLine(), out int testNum) && (testNum == 1 || testNum == 2))
                    {
                        Console.Clear();
                        KnapsackBranchBound.Solve(testNum);
                    }
                    else
                    {
                        Console.WriteLine("Invalid test case number. Use 1 or 2.");
                    }
                }
                else if (choice == "7")
                {
                    Console.Write("Enter test case ID (e.g., S1, B3, D2, K1): ");
                    string id = Console.ReadLine()?.ToUpper();
                    if (!string.IsNullOrEmpty(id))
                    {
                        Console.Clear();
                        CanonicalFormConverter.ShowConversion(id);
                    }
                }
                else
                {
                    Console.WriteLine("Algorithm not implemented yet.");
                }

                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
        }
    }
}