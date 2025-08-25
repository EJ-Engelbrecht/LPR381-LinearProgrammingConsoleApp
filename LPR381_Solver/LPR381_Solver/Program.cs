<<<<<<< Updated upstream
using LPR381_Solver.Algorithms;
using LPR381_Solver.Utils;
using System;
=======
using System;
using System.Linq;
using LPR381_Solver.Displays;
>>>>>>> Stashed changes

namespace LPR381_Solver
{
    class Program
    {
        static void Main(string[] args)
        {
<<<<<<< Updated upstream
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
=======
            bool useAscii = args.Contains("--ascii");
            bool useColor = !args.Contains("--no-color");
            
            while (true)
            {
                Console.Clear();
                Console.WriteLine("LPR381 Solver - Algorithm Displays");
                Console.WriteLine("===================================");
                Console.WriteLine("1. Knapsack Branch & Bound");
                Console.WriteLine("2. Primal Simplex Solution");
                Console.WriteLine("3. Cutting Plane Algorithm");
                Console.WriteLine("4. Branch & Bound (Integer LP)");
                Console.WriteLine("5. Run All");
                Console.WriteLine("0. Exit");
                Console.WriteLine();
                Console.Write("Select option (0-5): ");
                
                string choice = Console.ReadLine();
                Console.WriteLine();
                
                switch (choice)
                {
                    case "0":
                        return;
                    case "1":
                        KnapsackDisplay.Run(useAscii);
                        break;
                    case "2":
                        PrimalSimplexDisplay.Run(useAscii, useColor);
                        break;
                    case "3":
                        CuttingPlaneDisplay.Run(useAscii, useColor);
                        break;
                    case "4":
                        BranchAndBoundDisplay.Run(useAscii, useColor);
                        break;
                    case "5":
                        Console.WriteLine("=== Knapsack Branch & Bound ===");
                        KnapsackDisplay.Run(useAscii);
                        Console.WriteLine("\n\n=== Primal Simplex Solution ===");
                        PrimalSimplexDisplay.Run(useAscii, useColor);
                        Console.WriteLine("\n\n=== Cutting Plane Algorithm ===");
                        CuttingPlaneDisplay.Run(useAscii, useColor);
                        Console.WriteLine("\n\n=== Branch & Bound (Integer LP) ===");
                        BranchAndBoundDisplay.Run(useAscii, useColor);
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Try again.");
                        break;
                }
                
                if (choice != "0")
                {
                    Console.WriteLine("\nPress Enter to return to menu...");
                    Console.ReadLine();
                }
>>>>>>> Stashed changes
            }
        }
    }
}