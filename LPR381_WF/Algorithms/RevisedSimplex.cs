using System;
using System.Collections.Generic;
using System.Linq;
using LPR381.Core;

namespace LPR381_Solver.Algorithms
{
<<<<<<< Updated upstream
=======
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

    // ==== real linear algebra helpers ====
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

>>>>>>> Stashed changes
    public class RevisedSimplex
    {
        private readonly LPR381.Core.IIterationLogger _log;
        private readonly double _eps;
        
        public RevisedSimplex(LPR381.Core.IIterationLogger logger, double eps = 1e-9)
        {
            _log = logger;
            _eps = eps;
        }

<<<<<<< Updated upstream
        public SolveResult Solve(CanonicalForm cf)
        {
            _log.LogHeader("Revised Simplex Algorithm");
            var res = new SolveResult();
            
            try
            {
                BuildExpanded(cf, out var Aext, out var cext, out var b, out int[] basis, out string[] varNames);
                
                int m = b.Length;
                int nExt = Aext.GetLength(1);
                int iter = 0;
                
                _log.Log("Initial basis variables:");
                for (int i = 0; i < m; i++)
                    _log.Log($"  {varNames[basis[i]]} = {b[i]:F3}");

                while (iter < 50)
                {
                    iter++;
                    
                    // Build basis matrix B and non-basic matrix N
                    var B = new double[m, m];
                    var cB = new double[m];
                    var nonBasic = Enumerable.Range(0, nExt).Where(j => !basis.Contains(j)).ToArray();
                    var N = new double[m, nonBasic.Length];
                    var cN = new double[nonBasic.Length];
=======
        public SolveResult Solve(RevisedCanonicalForm cf)
        {
            _log.LogHeader("=== Revised Simplex ===");
            var result = new SolveResult();

            try
            {
                // Build extended form and starting basis
                BuildExpanded(
                    cf,
                    out var Aext,
                    out var b,
                    out var basis,
                    out var varNames,
                    out var artificialCols,
                    out var cPhaseII);

                // ---- Phase I: minimize sum of artificials (if any) ----
                if (artificialCols.Count > 0)
                {
                    var cPhaseI = new double[Aext.GetLength(1)];
                    foreach (var j in artificialCols) cPhaseI[j] = 1.0;
>>>>>>> Stashed changes

                    _log.LogHeader("Phase I (minimize sum of artificials)");
                    var p1 = SolveCore(Aext, b, cPhaseI, ref basis, varNames, isMax: false);
                    if (p1.Status != "Optimal" || p1.Objective > 1e-7)
                    {
<<<<<<< Updated upstream
                        for (int k = 0; k < m; k++) 
                            B[i, k] = Aext[i, basis[k]];
                        cB[i] = cext[basis[i]];
                    }
                    
                    for (int j = 0; j < nonBasic.Length; j++)
                    {
                        int col = nonBasic[j];
                        for (int i = 0; i < m; i++) 
                            N[i, j] = Aext[i, col];
                        cN[j] = cext[col];
                    }

                    // Solve B^-1 * b for current basic solution
                    var Binv = InvertMatrix(B);
                    var xB = MatrixVectorMultiply(Binv, b);
                    
                    // Calculate dual variables: π = c_B^T * B^-1
                    var pi = MatrixVectorMultiply(Transpose(Binv), cB);
                    
                    // Calculate reduced costs: r_N = c_N - π^T * N
                    var rN = new double[nonBasic.Length];
                    for (int j = 0; j < nonBasic.Length; j++)
                    {
                        double sum = 0;
                        for (int i = 0; i < m; i++) 
                            sum += pi[i] * N[i, j];
                        rN[j] = cN[j] - sum;
                    }

                    _log.Log($"\n=== ITERATION {iter} ===");
                    _log.Log($"Basis: [{string.Join(", ", basis.Select(idx => varNames[idx]))}]");
                    _log.Log("Basic solution:");
                    for (int i = 0; i < m; i++)
                        _log.Log($"  {varNames[basis[i]]} = {xB[i]:F2}");
                    
                    _log.Log("Reduced costs:");
                    for (int j = 0; j < nonBasic.Length; j++)
                        _log.Log($"  {varNames[nonBasic[j]]}: {rN[j]:F2}");

                    // Check optimality
                    int eIdx = -1;
                    double minRC = 0.0;
                    for (int j = 0; j < rN.Length; j++)
                    {
                        if (rN[j] < minRC - _eps) 
                        { 
                            minRC = rN[j]; 
                            eIdx = j; 
                        }
                    }

                    if (eIdx == -1)
                    {
                        res.Status = "Optimal";
                        var x = new double[nExt];
                        for (int i = 0; i < m; i++) 
                            x[basis[i]] = xB[i];
                        
                        double z = 0;
                        for (int j = 0; j < nExt; j++) 
                            z += cext[j] * x[j];
                        
                        res.X = x.Take(cf.N).Select(v => Math.Round(v, 3)).ToArray();
                        res.Objective = Math.Round(z, 3);
                        res.Iterations = iter;
                        
                        _log.Log("\n=== FINAL SOLUTION ===");
                        _log.Log($"Status         : {res.Status}");
                        _log.Log($"Objective Value: {res.Objective:F2}");
                        _log.Log($"Iterations     : {iter}");
                        _log.Log("Variables      :");
                        for (int i = 0; i < res.X.Length; i++) 
                            _log.Log($"  x{i+1} = {res.X[i]:F2}");
                        
                        return res;
                    }

                    int entering = nonBasic[eIdx];
                    _log.Log($"Entering variable: {varNames[entering]}");
                    
                    // Calculate direction vector: d = B^-1 * A_entering
                    var a_enter = new double[m];
                    for (int i = 0; i < m; i++) 
                        a_enter[i] = Aext[i, entering];
                    var d = MatrixVectorMultiply(Binv, a_enter);

                    // Minimum ratio test
                    int leaveRow = -1;
                    double bestTheta = double.PositiveInfinity;
                    for (int i = 0; i < m; i++)
                    {
                        if (d[i] > _eps)
                        {
                            double theta = xB[i] / d[i];
                            if (theta < bestTheta) 
                            { 
                                bestTheta = theta; 
                                leaveRow = i; 
                            }
                        }
                    }
                    
                    if (leaveRow == -1)
                    {
                        res.Status = "Unbounded";
                        _log.Log("Problem is unbounded");
                        return res;
                    }

                    int leaving = basis[leaveRow];
                    basis[leaveRow] = entering;
                    
                    _log.Log($"Leaving variable: {varNames[leaving]}");
                    _log.Log($"Step size θ = {bestTheta:F3}");
                }
                
                res.Status = "Iteration limit reached";
                return res;
            }
            catch (Exception ex)
            {
                _log.Log($"Error in RevisedSimplex: {ex.Message}");
                res.Status = "Error";
                return res;
            }
        }

        private void BuildExpanded(CanonicalForm cf, out double[,] Aext, out double[] cext, out double[] b, out int[] basis, out string[] varNames)
        {
            int m = cf.M, n = cf.N;
            int slacks = cf.Signs.Count(s => s == ConstraintSign.LE);
            int ext = n + slacks;
            
            Aext = new double[m, ext];
            cext = new double[ext];
            b = cf.b.ToArray();
            varNames = new string[ext];
            basis = new int[m];
            
            for (int j = 0; j < n; j++) 
            { 
                varNames[j] = $"x{j+1}"; 
                cext[j] = cf.Sense == ProblemSense.Max ? cf.c[j] : -cf.c[j]; 
            }
            
            int cur = n;
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++) 
                    Aext[i, j] = cf.A[i, j];
                    
                if (cf.Signs[i] == ConstraintSign.LE)
                {
                    Aext[i, cur] = 1; 
                    varNames[cur] = $"s{i+1}"; 
                    basis[i] = cur; 
                    cur++;
=======
                        _log.Log($"Phase I objective = {p1.Objective:0.###} > 0 -> Infeasible.");
                        return new SolveResult { Status = "Infeasible" };
                    }
                    _log.Log("Phase I complete. Starting Phase II…");
                }

                // ---- Phase II: original objective ----
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

                // B, cB and nonbasic set
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

                // pi = (B^{-T}) cB = Transpose(B^{-1}) * cB
                var pi = MatrixHelper.MatVec(MatrixHelper.Transpose(Binv), cB);

                // reduced costs r_j = c_j - pi^T a_j
                var r = new double[nExt];
                for (int j = 0; j < nExt; j++)
                {
                    double dot = 0;
                    for (int i = 0; i < m; i++) dot += pi[i] * Aext[i, j];
                    r[j] = c[j] - dot;
                }

                // choose entering
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
>>>>>>> Stashed changes
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

<<<<<<< Updated upstream
        private double[,] InvertMatrix(double[,] A)
        {
            int n = A.GetLength(0);
            var result = new double[n, n];
            var augmented = new double[n, 2 * n];
            
            // Create augmented matrix [A | I]
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                    augmented[i, j] = A[i, j];
                augmented[i, i + n] = 1.0;
            }
            
            // Gauss-Jordan elimination
            for (int i = 0; i < n; i++)
            {
                // Find pivot
                double pivot = augmented[i, i];
                if (Math.Abs(pivot) < 1e-10) pivot = 1e-10;
                
                // Scale row
                for (int j = 0; j < 2 * n; j++)
                    augmented[i, j] /= pivot;
                
                // Eliminate column
                for (int k = 0; k < n; k++)
                {
                    if (k != i)
                    {
                        double factor = augmented[k, i];
                        for (int j = 0; j < 2 * n; j++)
                            augmented[k, j] -= factor * augmented[i, j];
                    }
                }
            }
            
            // Extract inverse
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    result[i, j] = augmented[i, j + n];
            
            return result;
        }

        private double[] MatrixVectorMultiply(double[,] A, double[] x)
        {
            int m = A.GetLength(0);
            int n = A.GetLength(1);
            var result = new double[m];
            
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                    result[i] += A[i, j] * x[j];
            }
            
            return result;
        }

        private double[,] Transpose(double[,] A)
=======
        // Build extended (standard-form) model; slacks/surplus are named c1,c2,…; return Phase II costs and artificial column indices.
        private void BuildExpanded(
            RevisedCanonicalForm cf,
            out double[,] Aext,
            out double[] b,
            out int[] basis,
            out string[] varNames,
            out List<int> artificialCols,
            out double[] cPhaseII)
>>>>>>> Stashed changes
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
