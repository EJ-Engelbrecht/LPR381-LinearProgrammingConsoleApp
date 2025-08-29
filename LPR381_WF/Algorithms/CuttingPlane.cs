using System;
using System.Collections.Generic;
using System.Linq;
using LPR381.Core;

namespace LPR381_Solver.Algorithms
{
    public sealed class CuttingPlane
    {
        private readonly PrimalSimplex _lp;
        private readonly IIterationLogger _log;

        public CuttingPlane(PrimalSimplex primalSimplex, IIterationLogger logger)
        {
            _lp = primalSimplex;
            _log = logger;
        }

        private const double EPS = 1e-9;
        private const int MAX_CUTS = 25;

        public SolveResult Solve(CanonicalForm baseModel, HashSet<int> integerVars, int decisionCount = -1)
        {
            _log.LogHeader("=== Cutting Plane Algorithm ===");
            _log.Log("Displaying product form and price-out via Primal Simplex at each sub-problem.\n");

            var work = baseModel.Clone();
            if (decisionCount <= 0 || decisionCount > work.N) decisionCount = work.N;

            work = AddBounds(work, decisionCount);

            SolveResult last = null;

            for (int k = 0; k <= MAX_CUTS; k++)
            {
                var subLabel = (k == 0) ? "Sub Problem 0" : $"Sub Problem {k}";
                _log.LogHeader(subLabel);

                last = _lp.Solve(work);

                if (!string.Equals(last.Status, "Optimal", StringComparison.OrdinalIgnoreCase))
                {
                    _log.Log($"Status: {last.Status}. Stopping.");
                    break;
                }

                int jFrac = FindFractional(last.X, integerVars);
                if (jFrac == -1)
                {
                    _log.Log($"All integer variables integral. Z = {last.Objective:0.###}");
                    last.Status = "Optimal (Integer)";
                    return last;
                }

                double v = last.X[jFrac];
                int rhs = (int)Math.Floor(v);
                _log.Log($"Cut {k + 1}: branch/cut on x{jFrac + 1} (fractional {v:0.###})");
                _log.Log($"New constraint (c{work.M + 1}):  x{jFrac + 1} <= {rhs}");

                work = AddRow(work, jFrac, rhs, ConstraintSign.LE);

                _log.Log($"First Pivot Column for the cut: x{jFrac + 1}");
                _log.Log($"First Pivot Row for the cut constraint: c{work.M}");
                _log.Log("");
            }

            if (last == null)
                return new SolveResult { Status = "No Solution" };

            last.Status = "CutLimit";
            return last;
        }

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

        private static CanonicalForm AddRow(CanonicalForm cf, int j, double rhs, ConstraintSign sign)
        {
            var m0 = cf.M;
            var n = cf.N;

            var A2 = new double[m0 + 1, n];
            var b2 = new double[m0 + 1];
            var s2 = new ConstraintSign[m0 + 1];

            for (int i = 0; i < m0; i++)
            {
                for (int c = 0; c < n; c++) A2[i, c] = cf.A[i, c];
                b2[i] = cf.b[i];
                s2[i] = cf.Signs[i];
            }

            if (j >= 0 && j < n) A2[m0, j] = 1.0;
            b2[m0] = rhs;
            s2[m0] = sign;

            cf = cf.Clone();
            cf.A = A2;
            cf.b = b2;
            cf.Signs = s2;
            return cf;
        }

        private static CanonicalForm AddBounds(CanonicalForm cf, int decisionCount)
        {
            var m0 = cf.M;
            var n = cf.N;
            int add = decisionCount * 2;

            var A2 = new double[m0 + add, n];
            var b2 = new double[m0 + add];
            var s2 = new ConstraintSign[m0 + add];

            for (int i = 0; i < m0; i++)
            {
                for (int c = 0; c < n; c++) A2[i, c] = cf.A[i, c];
                b2[i] = cf.b[i];
                s2[i] = cf.Signs[i];
            }

            int r = m0;

            for (int j = 0; j < decisionCount; j++)
            {
                A2[r, j] = -1.0;
                b2[r] = 0.0;
                s2[r] = ConstraintSign.LE;
                r++;
            }

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