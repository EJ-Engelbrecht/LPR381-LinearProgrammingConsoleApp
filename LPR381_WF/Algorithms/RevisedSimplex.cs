using System;
using System.Collections.Generic;
using System.Linq;
using LPR381.Core;

namespace LPR381_Solver.Algorithms
{
    public enum RevisedConstraintSign { LE, GE, EQ }
    public enum RevisedProblemSense { Max, Min }

    public class RevisedCanonicalForm
    {
        public RevisedProblemSense Sense { get; set; }
        public double[,] A { get; set; }
        public double[] b { get; set; }
        public double[] c { get; set; }
        public RevisedConstraintSign[] Signs { get; set; }
        public int M => A.GetLength(0);
        public int N => A.GetLength(1);
    }

    internal static class MatrixHelper
    {
        private const double EPS = 1e-12;

        public static double[,] Invert(double[,] A)
        {
            int n = A.GetLength(0);
            if (n != A.GetLength(1)) throw new ArgumentException("Matrix must be square.");

            var aug = new double[n, 2 * n];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++) aug[i, j] = A[i, j];
                aug[i, n + i] = 1.0;
            }

            for (int k = 0; k < n; k++)
            {
                int piv = k;
                double best = Math.Abs(aug[piv, k]);
                for (int r = k + 1; r < n; r++)
                {
                    double v = Math.Abs(aug[r, k]);
                    if (v > best) { best = v; piv = r; }
                }
                if (best < EPS) throw new InvalidOperationException("Singular matrix.");

                if (piv != k) SwapRows(aug, piv, k);

                double p = aug[k, k];
                for (int j = 0; j < 2 * n; j++) aug[k, j] /= p;

                for (int r = 0; r < n; r++)
                {
                    if (r == k) continue;
                    double mult = aug[r, k];
                    if (Math.Abs(mult) < EPS) continue;
                    for (int j = 0; j < 2 * n; j++)
                        aug[r, j] -= mult * aug[k, j];
                }
            }

            var inv = new double[n, n];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    inv[i, j] = aug[i, n + j];
            return inv;
        }

        public static double[] MatVec(double[,] A, double[] x)
        {
            int m = A.GetLength(0), n = A.GetLength(1);
            if (x.Length != n) throw new ArgumentException("Dimension mismatch.");
            var y = new double[m];
            for (int i = 0; i < m; i++)
            {
                double s = 0;
                for (int j = 0; j < n; j++) s += A[i, j] * x[j];
                y[i] = s;
            }
            return y;
        }

        public static double[,] Transpose(double[,] A)
        {
            int m = A.GetLength(0), n = A.GetLength(1);
            var T = new double[n, m];
            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    T[j, i] = A[i, j];
            return T;
        }

        private static void SwapRows(double[,] M, int r1, int r2)
        {
            if (r1 == r2) return;
            int cols = M.GetLength(1);
            for (int j = 0; j < cols; j++)
            {
                double t = M[r1, j];
                M[r1, j] = M[r2, j];
                M[r2, j] = t;
            }
        }
    }
    public class RevisedSimplex
    {
        private readonly LPR381.Core.IIterationLogger _log;
        private readonly double _eps;
        
        public RevisedSimplex(LPR381.Core.IIterationLogger logger, double eps = 1e-9)
        {
            _log = logger;
            _eps = eps;
        }

        public SolveResult Solve(RevisedCanonicalForm cf)
        {
            _log.LogHeader("=== Revised Simplex ===");
            var result = new SolveResult();

            try
            {
                BuildExpanded(
                    cf,
                    out var Aext,
                    out var b,
                    out var basis,
                    out var varNames,
                    out var artificialCols,
                    out var cPhaseII);

                if (artificialCols.Count > 0)
                {
                    var cPhaseI = new double[Aext.GetLength(1)];
                    foreach (var j in artificialCols) cPhaseI[j] = 1.0;

                    _log.LogHeader("Phase I (minimize sum of artificials)");
                    var p1 = SolveCore(Aext, b, cPhaseI, ref basis, varNames, isMax: false);
                    if (p1.Status != "Optimal" || p1.Objective > 1e-7)
                    {
                        _log.Log($"Phase I objective = {p1.Objective:0.###} > 0 -> Infeasible.");
                        return new SolveResult { Status = "Infeasible" };
                    }
                    _log.Log("Phase I complete. Starting Phase II…");
                }

                _log.LogHeader("Phase II");
                bool isMax = (cf.Sense == RevisedProblemSense.Max);
                var p2 = SolveCore(Aext, b, cPhaseII, ref basis, varNames, isMax);

                result.Status = p2.Status;
                result.Objective = Math.Round(p2.Objective, 3);

                var xFull = p2.XFull;
                var xOrig = new double[cf.N];
                Array.Copy(xFull, xOrig, xOrig.Length);
                for (int i = 0; i < xOrig.Length; i++) xOrig[i] = Math.Round(xOrig[i], 3);
                result.X = xOrig;

                _log.Log($"Optimal after {p2.Iterations} iterations. Z = {result.Objective:0.###}");
                for (int i = 0; i < result.X.Length; i++)
                    _log.Log($"x{i + 1} = {result.X[i]:0.###}");

                return result;
            }
            catch (Exception ex)
            {
                _log.Log("Error in RevisedSimplex: " + ex.Message);
                result.Status = "Error";
                return result;
            }
        }

        private (string Status, double Objective, double[] XFull, int Iterations) SolveCore(
            double[,] Aext,
            double[] b,
            double[] c,
            ref int[] basis,
            string[] varNames,
            bool isMax)
        {
            int m = b.Length;
            int nExt = Aext.GetLength(1);
            int it = 0;

            while (true)
            {
                it++;

                var B = new double[m, m];
                var cB = new double[m];
                var nonBasic = new List<int>();
                for (int j = 0; j < nExt; j++)
                {
                    bool isB = false;
                    for (int row = 0; row < m; row++) if (basis[row] == j) { isB = true; break; }
                    if (!isB) nonBasic.Add(j);
                }
                for (int i = 0; i < m; i++)
                {
                    cB[i] = c[basis[i]];
                    for (int k = 0; k < m; k++) B[i, k] = Aext[i, basis[k]];
                }

                var Binv = MatrixHelper.Invert(B);
                var xB = MatrixHelper.MatVec(Binv, b);
                var pi = MatrixHelper.MatVec(MatrixHelper.Transpose(Binv), cB);

                var r = new double[nExt];
                for (int j = 0; j < nExt; j++)
                {
                    double dot = 0;
                    for (int i = 0; i < m; i++) dot += pi[i] * Aext[i, j];
                    r[j] = c[j] - dot;
                }

                int entering = -1;
                if (isMax)
                {
                    double best = 0;
                    foreach (var j in nonBasic)
                        if (r[j] > best + _eps) { best = r[j]; entering = j; }
                }
                else
                {
                    double best = 0;
                    foreach (var j in nonBasic)
                        if (r[j] < -_eps && r[j] < best) { best = r[j]; entering = j; }
                }

                // optimal?
                if (entering == -1)
                {
                    var xFull = new double[nExt];
                    for (int i = 0; i < m; i++) xFull[basis[i]] = xB[i];

                    double z = 0;
                    for (int j = 0; j < nExt; j++) z += c[j] * xFull[j];

                    return ("Optimal", z, xFull, it - 1);
                }

                // direction d = B^{-1} a_enter
                var aEnter = new double[m];
                for (int i = 0; i < m; i++) aEnter[i] = Aext[i, entering];
                var d = MatrixHelper.MatVec(Binv, aEnter);

                // ratio test
                int leaveRow = -1;
                double theta = double.PositiveInfinity;
                for (int i = 0; i < m; i++)
                {
                    if (d[i] > _eps)
                    {
                        double t = xB[i] / d[i];
                        if (t < theta) { theta = t; leaveRow = i; }
                    }
                }
                if (leaveRow == -1)
                {
                    _log.Log("Detected unbounded in revised simplex.");
                    return ("Unbounded", double.NaN, new double[nExt], it);
                }

                int leaving = basis[leaveRow];
                basis[leaveRow] = entering;
                _log.Log($"Iter {it}: Enter = {varNames[entering]}, Leave = {varNames[leaving]}, θ = {theta:0.###}");
            }
        }

        private void BuildExpanded(
            RevisedCanonicalForm cf,
            out double[,] Aext,
            out double[] b,
            out int[] basis,
            out string[] varNames,
            out List<int> artificialCols,
            out double[] cPhaseII)
        {
            int m = cf.M, n = cf.N;

            int extra = 0;
            for (int i = 0; i < m; i++)
            {
                if (cf.Signs[i] == RevisedConstraintSign.LE) extra += 1;      // +c_i
                else if (cf.Signs[i] == RevisedConstraintSign.GE) extra += 2; // +c_i + a_i
                else if (cf.Signs[i] == RevisedConstraintSign.EQ) extra += 1; // +a_i
            }

            int nExt = n + extra;
            Aext = new double[m, nExt];
            b = cf.b.ToArray();
            basis = new int[m];
            varNames = new string[nExt];
            cPhaseII = new double[nExt];
            artificialCols = new List<int>();

            // original vars and costs
            for (int j = 0; j < n; j++)
            {
                varNames[j] = $"x{j + 1}";
                cPhaseII[j] = cf.c[j]; // Max/Min handled in SolveCore
            }

            // copy A
            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    Aext[i, j] = cf.A[i, j];

            // add c_i / a_i columns
            int cur = n;
            for (int i = 0; i < m; i++)
            {
                switch (cf.Signs[i])
                {
                    case RevisedConstraintSign.LE:
                        Aext[i, cur] = 1.0;               // slack
                        varNames[cur] = $"c{i + 1}";
                        cPhaseII[cur] = 0.0;
                        basis[i] = cur;
                        cur++;
                        break;

                    case RevisedConstraintSign.GE:
                        Aext[i, cur] = -1.0;              // surplus
                        varNames[cur] = $"c{i + 1}";
                        cPhaseII[cur] = 0.0;
                        cur++;

                        Aext[i, cur] = 1.0;               // artificial
                        varNames[cur] = $"a{i + 1}";
                        cPhaseII[cur] = 0.0;
                        basis[i] = cur;
                        artificialCols.Add(cur);
                        cur++;
                        break;

                    case RevisedConstraintSign.EQ:
                        Aext[i, cur] = 1.0;               // artificial
                        varNames[cur] = $"a{i + 1}";
                        cPhaseII[cur] = 0.0;
                        basis[i] = cur;
                        artificialCols.Add(cur);
                        cur++;
                        break;
                }
            }
        }
    }
}
