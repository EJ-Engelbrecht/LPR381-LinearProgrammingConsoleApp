using System;
using System.Collections.Generic;
using System.Linq;
using LPR381_Solver.Algorithms;

namespace LPR381_Solver.Displays
{
    public static class BranchAndBoundDisplay
    {
        public static void Run(bool useAscii = false, bool useColor = true)
        {
            Console.WriteLine("Branch & Bound Integer Linear Programming");
            Console.WriteLine("========================================");
            Console.WriteLine();

            // Create test problem: max 3x1 + 2x2 subject to x1 + x2 <= 3, x1,x2 >= 0, x1,x2 integer
            var A = new double[,] { { 1, 1 } };
            var b = new double[] { 3 };
            var c = new double[] { 3, 2 };
            var integerVars = new HashSet<int> { 0, 1 };
            var model = new LpModel(A, b, c, 2, integerVars);

            Console.WriteLine("Problem: Maximize 3x₁ + 2x₂");
            Console.WriteLine("Subject to: x₁ + x₂ ≤ 3");
            Console.WriteLine("           x₁, x₂ ≥ 0 (integer)");
            Console.WriteLine();
            
            // Step 1: Show initial LP relaxation solution
            Console.WriteLine("=== STEP 1: LP RELAXATION ===");
            ShowInitialTableau(useAscii, useColor);
            Console.WriteLine();
            Console.WriteLine("LP Relaxation Solution: x₁ = 2.5, x₂ = 0.5, z = 8.5");
            Console.WriteLine("Solution is not integer feasible - branching required.");
            Console.WriteLine();
            
            // Create initial tableau for primal simplex (after solving LP relaxation)
            var tableau = new double[,] {
                { 1, 0, -0.5, 1.5, 8.5 },  // z row (optimal)
                { 0, 1,  0.5, 0.5, 2.5 }   // x1 row (basic)
            };
            var basis = new int[] { 0 }; // x1 is basic
            var initialState = new TableauState(tableau, basis);

            // Create mock primal solver with step-by-step display
            var primalSolver = new MockPrimalSolver(initialState, useAscii, useColor);
            
            // Create branch and bound solver
            var bb = new BranchAndBound(primalSolver);
            
            // Step 2: Show branching process
            Console.WriteLine("=== STEP 2: BRANCHING PROCESS ===");
            Console.WriteLine("Branching on x₁ (fractional value = 2.5)");
            Console.WriteLine("Left branch: x₁ ≤ 2");
            Console.WriteLine("Right branch: x₁ ≥ 3");
            Console.WriteLine();
            
            // Solve
            var result = bb.Solve(model);
            
            // Display results
            ShowResult(result, useAscii, useColor);
        }

        public static void ShowResult(BbResult result, bool useAscii = false, bool useColor = true)
        {
            Console.WriteLine();
            Console.WriteLine("=== BRANCH AND BOUND RESULTS ===");
            Console.WriteLine();

            // Show status
            string statusColor = useColor ? (result.Status == Status.Optimal ? "\u001b[32m" : "\u001b[31m") : "";
            string resetColor = useColor ? "\u001b[0m" : "";
            Console.WriteLine($"Status: {statusColor}{result.Status}{resetColor}");
            
            if (result.Status == Status.Optimal)
            {
                Console.WriteLine($"Optimal Objective: {result.Objective:F3}");
                Console.WriteLine($"Solution: [{string.Join(", ", result.Solution.Select(x => x.ToString("F3")))}]");
            }

            Console.WriteLine();
            ShowCandidates(result.Candidates, useAscii, useColor);
        }

        private static void ShowCandidates(List<BbNode> candidates, bool useAscii, bool useColor)
        {
            if (!candidates.Any()) return;

            string headerColor = useColor ? "\u001b[33m" : "";
            string resetColor = useColor ? "\u001b[0m" : "";
            
            Console.WriteLine($"{headerColor}=== INTEGER CANDIDATES ==={resetColor}");
            Console.WriteLine();

            foreach (var candidate in candidates.OrderByDescending(c => c.LpBound))
            {
                Console.WriteLine($"{candidate.CandidateName}:");
                Console.WriteLine($"  Objective: {candidate.LpBound:F3}");
                Console.WriteLine($"  Solution: [{string.Join(", ", candidate.X.Select(x => x.ToString("F3")))}]");
                Console.WriteLine($"  Node: {candidate.Label} (Depth {candidate.Depth})");
                Console.WriteLine();
            }
        }

        private static void ShowInitialTableau(bool useAscii, bool useColor)
        {
            // Show the optimal LP relaxation tableau
            var tableau = new double[,] {
                { 1, 0, -0.5, 1.5, 8.5 },  // z row
                { 0, 1,  0.5, 0.5, 2.5 }   // x1 row
            };
            
            ShowTableau(tableau, null, null, null, null, useAscii, useColor);
        }
        
        private static void ShowTableau(double[,] tableau, int? enteringCol, int? leavingRow, int? pivotRow, int? pivotCol, bool useAscii, bool useColor)
        {
            int rows = tableau.GetLength(0);
            int cols = tableau.GetLength(1);
            
            // Box drawing characters
            string topLeft = useAscii ? "+" : "┌";
            string topRight = useAscii ? "+" : "┐";
            string bottomLeft = useAscii ? "+" : "└";
            string bottomRight = useAscii ? "+" : "┘";
            string horizontal = useAscii ? "-" : "─";
            string vertical = useAscii ? "|" : "│";
            string cross = useAscii ? "+" : "┼";
            string teeDown = useAscii ? "+" : "┬";
            string teeUp = useAscii ? "+" : "┴";
            string teeRight = useAscii ? "+" : "├";
            string teeLeft = useAscii ? "+" : "┤";

            // Colors
            string pivotColor = useColor ? "\u001b[43m\u001b[30m" : "";
            string enteringColor = useColor ? "\u001b[44m\u001b[37m" : "";
            string leavingColor = useColor ? "\u001b[45m\u001b[37m" : "";
            string resetColor = useColor ? "\u001b[0m" : "";

            // Calculate column widths
            var colWidths = new int[cols];
            for (int j = 0; j < cols; j++)
            {
                colWidths[j] = Math.Max(6, 8);
            }

            // Top border
            Console.Write("     ");
            Console.Write(topLeft);
            for (int j = 0; j < cols; j++)
            {
                Console.Write(new string(horizontal[0], colWidths[j]));
                if (j < cols - 1) Console.Write(teeDown);
            }
            Console.WriteLine(topRight);
            
            // Headers
            Console.Write("     ");
            Console.Write(vertical);
            Console.Write(" x₁  ");
            Console.Write(vertical);
            Console.Write(" x₂  ");
            Console.Write(vertical);
            Console.Write(" s₁  ");
            Console.Write(vertical);
            Console.Write(" RHS ");
            Console.WriteLine(vertical);
            
            // Header separator
            Console.Write("     ");
            Console.Write(teeRight);
            for (int j = 0; j < cols; j++)
            {
                Console.Write(new string(horizontal[0], colWidths[j]));
                if (j < cols - 1) Console.Write(cross);
            }
            Console.WriteLine(teeLeft);

            // Data rows
            string[] rowLabels = { "z   ", "x₁  " };
            for (int i = 0; i < rows; i++)
            {
                Console.Write(rowLabels[i]);
                Console.Write(vertical);
                for (int j = 0; j < cols; j++)
                {
                    string cellColor = "";
                    if (useColor && pivotRow.HasValue && pivotCol.HasValue && i == pivotRow && j == pivotCol)
                        cellColor = pivotColor;
                    else if (useColor && enteringCol.HasValue && j == enteringCol)
                        cellColor = enteringColor;
                    else if (useColor && leavingRow.HasValue && i == leavingRow)
                        cellColor = leavingColor;

                    string value = tableau[i, j].ToString("F1").PadLeft(colWidths[j]);
                    Console.Write($"{cellColor}{value}{resetColor}");
                    if (j < cols - 1) Console.Write(vertical);
                }
                Console.WriteLine(vertical);

                // Row separator (except for last row)
                if (i < rows - 1)
                {
                    Console.Write("     ");
                    Console.Write(teeRight);
                    for (int j = 0; j < cols; j++)
                    {
                        Console.Write(new string(horizontal[0], colWidths[j]));
                        if (j < cols - 1) Console.Write(cross);
                    }
                    Console.WriteLine(teeLeft);
                }
            }

            // Bottom border
            Console.Write("     ");
            Console.Write(bottomLeft);
            for (int j = 0; j < cols; j++)
            {
                Console.Write(new string(horizontal[0], colWidths[j]));
                if (j < cols - 1) Console.Write(teeUp);
            }
            Console.WriteLine(bottomRight);
        }

        // Mock primal solver for testing
        private class MockPrimalSolver : ISimplexSolver
        {
            private readonly TableauState initialState;
            private readonly bool useAscii;
            private readonly bool useColor;
            public event Action<IterationSnapshot> OnIter;

            public MockPrimalSolver(TableauState initialState, bool useAscii, bool useColor)
            {
                this.initialState = initialState;
                this.useAscii = useAscii;
                this.useColor = useColor;
            }

            public SimplexResult Solve(LpModel model, TableauState warmStart = null, double tol = 1e-9)
            {
                // Simple mock: return LP relaxation solution (2.5, 0.5) with objective 8.5
                var primal = new double[] { 2.5, 0.5 };
                var objective = 3 * 2.5 + 2 * 0.5; // = 8.5
                
                return new SimplexResult(Status.Optimal, objective, primal, new double[0], 
                    initialState.Tableau, initialState);
            }
        }
    }
}