using System;
using System.Collections.Generic;
using System.Linq;
using LPR381.Core;

namespace LPR381_Solver.Algorithms
{
<<<<<<< Updated upstream
    public class CuttingPlane
    {
        private readonly LPR381.Core.IIterationLogger _log;
        private readonly PrimalSimplex _solver;

        public CuttingPlane(LPR381.Core.IIterationLogger logger)
        {
            _log = logger;
            _solver = new PrimalSimplex(logger);
        }

        public SolveResult Solve(CanonicalForm cf, double intTol = 1e-6)
        {
            _log.LogHeader("Cutting Plane (Gomory) Algorithm");
            
            try
            {
                var current = cf.Clone();
                int cutCount = 0;
                const int maxCuts = 20;

                for (int iter = 1; iter <= maxCuts; iter++)
                {
                    _log.Log($"\n=== ITERATION {iter} ===");
                    
                    // Solve current LP relaxation
                    var lpResult = _solver.Solve(current);
                    
                    if (lpResult.Status != "Optimal")
                    {
                        _log.Log($"LP relaxation is {lpResult.Status}");
                        return lpResult;
                    }

                    _log.Log($"LP Solution: Z = {lpResult.Objective:F3}");
                    for (int i = 0; i < lpResult.X.Length; i++)
                        _log.Log($"x{i+1} = {lpResult.X[i]:F3}");

                    // Check if solution is integer
                    int fracVar = FindMostFractionalVariable(lpResult.X, intTol);
                    if (fracVar == -1)
                    {
                        _log.Log("\nAll variables are integer - optimal solution found!");
                        lpResult.Status = "Optimal (Integer)";
                        lpResult.Iterations = cutCount;
                        return lpResult;
                    }

                    double fracValue = lpResult.X[fracVar];
                    double fractionalPart = fracValue - Math.Floor(fracValue);
                    
                    _log.Log($"\nMost fractional variable: x{fracVar+1} = {fracValue:F3}");
                    _log.Log($"Fractional part: {fractionalPart:F3}");

                    // Generate Gomory cut: x_j <= floor(fracValue)
                    double cutRhs = Math.Floor(fracValue);
                    _log.Log($"Adding Gomory cut: x{fracVar+1} <= {cutRhs}");

                    // Add cut to problem
                    current = AddCut(current, fracVar, cutRhs);
                    cutCount++;
                }

                _log.Log($"\nReached maximum number of cuts ({maxCuts})");
                var finalResult = _solver.Solve(current);
                finalResult.Status = "Cut limit reached";
                finalResult.Iterations = cutCount;
                return finalResult;
            }
            catch (Exception ex)
            {
                _log.Log($"Error in Cutting Plane: {ex.Message}");
                return new SolveResult { Status = "Error" };
            }
        }

        private int FindMostFractionalVariable(double[] solution, double tolerance)
        {
            int mostFractional = -1;
            double maxFractionalPart = 0;
            
            for (int i = 0; i < solution.Length; i++)
            {
                double fractionalPart = Math.Abs(solution[i] - Math.Round(solution[i]));
                if (fractionalPart > tolerance && fractionalPart > maxFractionalPart)
                {
                    maxFractionalPart = fractionalPart;
                    mostFractional = i;
                }
            }
            
            return mostFractional;
        }

        private CanonicalForm AddCut(CanonicalForm cf, int cutVar, double cutRhs)
        {
            int m = cf.M;
            int n = cf.N;
            
            // Create new constraint matrix with one additional row
            var newA = new double[m + 1, n];
            var newb = new double[m + 1];
            var newSigns = new ConstraintSign[m + 1];
            
            // Copy existing constraints
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                    newA[i, j] = cf.A[i, j];
                newb[i] = cf.b[i];
                newSigns[i] = cf.Signs[i];
            }
            
            // Add Gomory cut: x_cutVar <= cutRhs
            newA[m, cutVar] = 1.0;
            newb[m] = cutRhs;
            newSigns[m] = ConstraintSign.LE;
            
            return new CanonicalForm
            {
                Sense = cf.Sense,
                A = newA,
                b = newb,
                c = (double[])cf.c.Clone(),
                Signs = newSigns,
                VariableTypes = (VarType[])cf.VariableTypes.Clone(),
                VariableNames = cf.VariableNames?.ToArray()
            };
        }
    }
}
=======
    /// <summary>
    /// Very simple Gomory-style cutting plane for all-integer (binary) models.
    /// Uses the SAME IIterationLogger as the rest of the UI so text appears in the RichTextBox.
    /// We repeatedly:
    ///   1) Solve LP relaxation with PrimalSimplex (product-form displayed by that solver)
    ///   2) If some integer var is fractional: add cut x_j <= floor(x_j*)  (for binary this becomes x_j <= 0)
    ///   3) Loop until all integer vars integral or we hit limits.
    /// </summary>
    public sealed class CuttingPlane
    {
        private readonly PrimalSimplex _lp;     // reuse your existing solver so the tableau prints the same way
        private readonly IIterationLogger _log;

        public CuttingPlane(PrimalSimplex primalSimplex, IIterationLogger logger)
        {
            _lp = primalSimplex;
            _log = logger;
        }

        private const double EPS = 1e-9;
        private const int MAX_CUTS = 25;

        // ---- Public entry ----
        public SolveResult Solve(CanonicalForm baseModel, HashSet<int> integerVars, int decisionCount = -1)
        {
            _log.LogHeader("=== Cutting Plane Algorithm ===");
            _log.Log("Displaying product form and price-out via Primal Simplex at each sub-problem.\n");

            // we’ll keep a working copy of the model and keep appending rows
            var work = baseModel.Clone();
            if (decisionCount <= 0 || decisionCount > work.N) decisionCount = work.N;

            // normalize: add non-negativity and binary upper bounds for decision variables
            work = AddBounds(work, decisionCount);

            SolveResult last = null;

            for (int k = 0; k <= MAX_CUTS; k++)
            {
                var subLabel = (k == 0) ? "Sub Problem 0" : $"Sub Problem {k}";
                _log.LogHeader(subLabel);

                // 1) Solve LP relaxation with the regular primal simplex (this prints the tableau)
                last = _lp.Solve(work);

                if (!string.Equals(last.Status, "Optimal", StringComparison.OrdinalIgnoreCase))
                {
                    _log.Log($"Status: {last.Status}. Stopping.");
                    break;
                }

                // 2) Check integrality on requested integer vars
                int jFrac = FindFractional(last.X, integerVars);
                if (jFrac == -1)
                {
                    _log.Log($"All integer variables integral. Z = {last.Objective:0.###}");
                    last.Status = "Optimal (Integer)";
                    return last;
                }

                // 3) Add a simple (binary) cut: x_j <= floor(x_j*)
                double v = last.X[jFrac];
                int rhs = (int)Math.Floor(v);
                _log.Log($"Cut {k + 1}: branch/cut on x{jFrac + 1} (fractional {v:0.###})");
                _log.Log($"New constraint (c{work.M + 1}):  x{jFrac + 1} <= {rhs}");

                work = AddRow(work, jFrac, rhs, ConstraintSign.LE);

                // Also show the “pivot information placeholders” your sheet expects:
                // We don’t compute exact pivot row/col for the cut here; we just echo the first basic var column
                // to guide the student sheet fields.
                _log.Log($"First Pivot Column for the cut: x{jFrac + 1}");
                _log.Log($"First Pivot Row for the cut constraint: c{work.M}"); // the row we just added is last
                _log.Log(""); // spacer
            }

            if (last == null)
                return new SolveResult { Status = "No Solution" };

            last.Status = "CutLimit";
            return last;
        }

        // ---- helpers ----

        private static int FindFractional(double[] x, HashSet<int> intVars, double tol = EPS)
        {
            if (x == null) return -1;
            foreach (var j in intVars.OrderBy(t => t))
            {
                if (j < 0 || j >= x.Length) continue;
                double frac = Math.Abs(x[j] - Math.Round(x[j]));
                if (frac > tol) return j;
            }
            return -1;
        }

        // add row a*x <= rhs where a is 1 at column j, otherwise 0 (simple single-var cut)
        private static CanonicalForm AddRow(CanonicalForm cf, int j, double rhs, ConstraintSign sign)
        {
            var m0 = cf.M;
            var n = cf.N;

            var A2 = new double[m0 + 1, n];
            var b2 = new double[m0 + 1];
            var s2 = new ConstraintSign[m0 + 1];

            // copy existing
            for (int i = 0; i < m0; i++)
            {
                for (int c = 0; c < n; c++) A2[i, c] = cf.A[i, c];
                b2[i] = cf.b[i];
                s2[i] = cf.Signs[i];
            }

            // new row
            if (j >= 0 && j < n) A2[m0, j] = 1.0;
            b2[m0] = rhs;
            s2[m0] = sign;

            cf = cf.Clone();
            cf.A = A2;
            cf.b = b2;
            cf.Signs = s2;
            return cf;
        }

        // add nonnegativity and (for binaries) x<=1 rows for first decisionCount columns
        private static CanonicalForm AddBounds(CanonicalForm cf, int decisionCount)
        {
            var m0 = cf.M;
            var n = cf.N;
            int add = decisionCount * 2; // -x <= 0   and   x <= 1

            var A2 = new double[m0 + add, n];
            var b2 = new double[m0 + add];
            var s2 = new ConstraintSign[m0 + add];

            // copy existing
            for (int i = 0; i < m0; i++)
            {
                for (int c = 0; c < n; c++) A2[i, c] = cf.A[i, c];
                b2[i] = cf.b[i];
                s2[i] = cf.Signs[i];
            }

            int r = m0;

            // -x_j <= 0
            for (int j = 0; j < decisionCount; j++)
            {
                A2[r, j] = -1.0;
                b2[r] = 0.0;
                s2[r] = ConstraintSign.LE;
                r++;
            }

            // x_j <= 1
            for (int j = 0; j < decisionCount; j++)
            {
                A2[r, j] = 1.0;
                b2[r] = 1.0;
                s2[r] = ConstraintSign.LE;
                r++;
            }

            var outCf = cf.Clone();
            outCf.A = A2;
            outCf.b = b2;
            outCf.Signs = s2;
            return outCf;
        }
    }
}
>>>>>>> Stashed changes
