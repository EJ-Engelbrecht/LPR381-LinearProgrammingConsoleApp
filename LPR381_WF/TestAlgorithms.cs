using System;
using System.Collections.Generic;
using System.Linq;
using LPR381.Core;
using LPR381_Solver.Algorithms;
using LPR381_Solver.Models;
using LPR381_Solver.Input;

namespace LPR381_WF
{
    public class ConsoleLogger : IIterationLogger
    {
        public void Log(string message) => Console.WriteLine(message);
        public void LogHeader(string title) => Console.WriteLine($"\n=== {title} ===");
        public void LogMatrix(string title, double[,] mat, int round = 3, string[] colNames = null, string[] rowNames = null)
        {
            Console.WriteLine($"\n{title}:");
            for (int i = 0; i < mat.GetLength(0); i++)
            {
                for (int j = 0; j < mat.GetLength(1); j++)
                    Console.Write($"{mat[i, j]:F3}\t");
                Console.WriteLine();
            }
        }
        public void LogVector(string title, double[] vec, int round = 3, string[] names = null)
        {
            Console.WriteLine($"\n{title}: [{string.Join(", ", vec.Select(x => x.ToString($"F{round}")))}]");
        }
    }

    public static class TestAlgorithms
    {
        public static void RunTests()
        {
            Console.WriteLine("=== TESTING PRIMAL SIMPLEX AND BRANCH-AND-BOUND ===\n");
            
            // Test 1: Simple LP
            TestSimpleLP();
            
            // Test 2: Integer Programming
            TestIntegerProgramming();
        }
        
        private static void TestSimpleLP()
        {
            Console.WriteLine("TEST 1: Simple Linear Programming");
            Console.WriteLine("Problem: max 3x1 + 2x2 s.t. x1 + x2 <= 4, 2x1 + x2 <= 6, x1,x2 >= 0");
            
            var logger = new ConsoleLogger();
            var solver = new PrimalSimplex(logger);
            
            // Create canonical form
            var cf = new CanonicalForm
            {
                Sense = ProblemSense.Max,
                A = new double[,] { {1, 1}, {2, 1} },
                b = new double[] { 4, 6 },
                c = new double[] { 3, 2 },
                Signs = new ConstraintSign[] { ConstraintSign.LE, ConstraintSign.LE },
                VariableTypes = new VarType[] { VarType.Plus, VarType.Plus },
                VariableNames = new string[] { "x1", "x2" }
            };
            
            var result = solver.Solve(cf);
            Console.WriteLine($"\nResult: Status={result.Status}, Objective={result.Objective:F3}");
            if (result.X != null)
            {
                for (int i = 0; i < result.X.Length; i++)
                    Console.WriteLine($"x{i+1} = {result.X[i]:F3}");
            }
            
            Console.WriteLine("\n" + new string('=', 60) + "\n");
        }
        
        private static void TestIntegerProgramming()
        {
            Console.WriteLine("TEST 2: Integer Programming");
            Console.WriteLine("Problem: max 3x1 + 2x2 s.t. x1 + x2 <= 4, 2x1 + x2 <= 6, x1,x2 integer >= 0");
            
            var logger = new ConsoleLogger();
            var solver = new PrimalSimplex(logger);
            var bb = new BranchAndBound(solver, logger);
            
            // Create canonical form
            var cf = new CanonicalForm
            {
                Sense = ProblemSense.Max,
                A = new double[,] { {1, 1}, {2, 1} },
                b = new double[] { 4, 6 },
                c = new double[] { 3, 2 },
                Signs = new ConstraintSign[] { ConstraintSign.LE, ConstraintSign.LE },
                VariableTypes = new VarType[] { VarType.Int, VarType.Int },
                VariableNames = new string[] { "x1", "x2" }
            };
            
            var intVars = new HashSet<int> { 0, 1 }; // Both variables are integer
            
            var result = bb.Solve(cf, intVars);
            Console.WriteLine($"\nFinal Result: Status={result.Status}, Objective={result.Objective:F3}");
            if (result.Solution != null)
            {
                for (int i = 0; i < result.Solution.Length; i++)
                    Console.WriteLine($"x{i+1} = {result.Solution[i]:F3}");
            }
            
            Console.WriteLine("\n" + new string('=', 60) + "\n");
        }
    }
}