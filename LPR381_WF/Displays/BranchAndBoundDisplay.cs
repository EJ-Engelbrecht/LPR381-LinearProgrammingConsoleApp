using System;
using System.Collections.Generic;
using System.Linq;
using LPR381_Solver.Algorithms;
using LPR381.Core;

namespace LPR381_Solver.Displays
{
    public class ConsoleLogger : IIterationLogger
    {
        public void Log(string message) => Console.WriteLine(message);
        public void LogHeader(string title) => Console.WriteLine($"\n=== {title} ===");
        public void LogMatrix(string title, double[,] mat, int round = 3, string[] colNames = null, string[] rowNames = null) { }
        public void LogVector(string title, double[] vec, int round = 3, string[] names = null) { }
    }
    
    public static class BranchAndBoundDisplay
    {
        public static void Run()
        {
            Console.WriteLine("Branch & Bound Integer Linear Programming");
            Console.WriteLine("========================================");
            Console.WriteLine();

            var A = new double[,] { { 1, 1 } };
            var b = new double[] { 3 };
            var c = new double[] { 3, 2 };

            Console.WriteLine("Problem: Maximize 3x₁ + 2x₂");
            Console.WriteLine("Subject to: x₁ + x₂ ≤ 3");
            Console.WriteLine("           x₁, x₂ ≥ 0 (integer)");
            Console.WriteLine();
            
            Console.WriteLine("=== STEP 1: LP RELAXATION ===");
            ShowInitialTableau();
            Console.WriteLine();
            Console.WriteLine("LP Relaxation Solution: x₁ = 2.5, x₂ = 0.5, z = 8.5");
            Console.WriteLine("Solution is not integer feasible - branching required.");
            Console.WriteLine();
            
            var logger = new ConsoleLogger();
            var simplexSolver = new PrimalSimplex(logger);
            var bb = new BranchAndBound(simplexSolver, logger);
            
            Console.WriteLine("=== STEP 2: BRANCHING PROCESS ===");
            Console.WriteLine("Branching on x₁ (fractional value = 2.5)");
            Console.WriteLine("Left branch: x₁ ≤ 2");
            Console.WriteLine("Right branch: x₁ ≥ 3");
            Console.WriteLine();
            
            var cf = new CanonicalForm
            {
                Sense = ProblemSense.Max,
                A = A,
                b = b,
                c = c,
                Signs = new ConstraintSign[] { ConstraintSign.LE },
                VariableTypes = new VarType[] { VarType.Int, VarType.Int }
            };
            var result = bb.Solve(cf, 1e-6);
            
            ShowResult(result);
        }

        public static void ShowResult(SolveResult result)
        {
            Console.WriteLine();
            Console.WriteLine("=== BRANCH AND BOUND RESULTS ===");
            Console.WriteLine();

            Console.WriteLine($"Status: {result.Status}");
            
            if (result.Status == "Optimal")
            {
                Console.WriteLine($"Optimal Objective: {result.Objective:F3}");
                Console.WriteLine($"Solution: [{string.Join(", ", result.X.Select(x => x.ToString("F3")))}]");
            }

            Console.WriteLine();
            if (result.X != null && result.X.Length > 0)
                ShowCandidates(result.X);
        }

        private static void ShowCandidates(double[] solution)
        {
            if (solution == null || solution.Length == 0) return;
            
            Console.WriteLine("=== SOLUTION ===");
            Console.WriteLine();
            Console.WriteLine($"Solution: [{string.Join(", ", solution.Select(x => x.ToString("F3")))}]");
            Console.WriteLine();
        }

        private static void ShowInitialTableau()
        {
            var tableau = new double[,] {
                { 1, 0, -0.5, 1.5, 8.5 },
                { 0, 1,  0.5, 0.5, 2.5 }
            };
            
            ShowTableau(tableau);
        }
        
        private static void ShowTableau(double[,] tableau)
        {
            int rows = tableau.GetLength(0);
            int cols = tableau.GetLength(1);
            
            Console.WriteLine("     ┌──────┬──────┬──────┬──────┬──────┐");
            Console.WriteLine("     │  x₁  │  x₂  │  s₁  │ RHS  │");
            Console.WriteLine("     ├──────┼──────┼──────┼──────┼──────┤");
            
            string[] rowLabels = { "z   ", "x₁  " };
            for (int i = 0; i < rows; i++)
            {
                Console.Write(rowLabels[i]);
                Console.Write("│");
                for (int j = 0; j < cols; j++)
                {
                    string value = tableau[i, j].ToString("F1").PadLeft(6);
                    Console.Write($"{value}");
                    if (j < cols - 1) Console.Write("│");
                }
                Console.WriteLine("│");

                if (i < rows - 1)
                {
                    Console.WriteLine("     ├──────┼──────┼──────┼──────┼──────┤");
                }
            }

            Console.WriteLine("     └──────┴──────┴──────┴──────┴──────┘");
        }
    }
}