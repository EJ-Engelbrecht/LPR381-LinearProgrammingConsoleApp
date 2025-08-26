using System;
using System.Collections.Generic;
using System.Linq;

namespace LPR381_Solver.Algorithms
{
    // Local enums and classes for RevisedSimplex
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
    
    // Simple MatrixHelper methods
    public static class MatrixHelper
    {
        public static double[,] Invert(double[,] A)
        {
            // Placeholder - return identity matrix
            int n = A.GetLength(0);
            var result = new double[n, n];
            for (int i = 0; i < n; i++) result[i, i] = 1.0;
            return result;
        }
        
        public static double[] MatVec(double[,] A, double[] x)
        {
            // Placeholder - return zero vector
            return new double[A.GetLength(0)];
        }
    }
    public class RevisedSimplex
    {
        private readonly IIterationLogger _log;
        private readonly double _eps;
        public RevisedSimplex(IIterationLogger logger, double eps = 1e-9)
        {
            _log = logger;
            _eps = eps;
        }

        public LPR381.Core.SolveResult Solve(RevisedCanonicalForm cf)
        {
            _log.LogHeader("Revised Simplex");
            var res = new LPR381.Core.SolveResult();
            try
            {
                BuildExpanded(cf, out var Aext, out var cext, out var b, out int[] basis, out string[] varNames);

                int m = b.Length;
                int nExt = Aext.GetLength(1);
                int iter = 0;

                while (true)
                {
                    iter++;
                    // Build basis matrices
                    var B = new double[m, m];
                    var cB = new double[m];
                    int[] nonBasic = Enumerable.Range(0, nExt).Where(j => !basis.Contains(j)).ToArray();
                    var N = new double[m, nonBasic.Length];
                    var cN = new double[nonBasic.Length];

                    for (int i = 0; i < m; i++)
                    {
                        for (int k = 0; k < m; k++) B[i, k] = Aext[i, basis[k]];
                        cB[i] = cext[basis[i]];
                    }
                    for (int j = 0; j < nonBasic.Length; j++)
                    {
                        int col = nonBasic[j];
                        for (int i = 0; i < m; i++) N[i, j] = Aext[i, col];
                        cN[j] = cext[col];
                    }

                    var Binv = MatrixHelper.Invert(B);

                    // Reduced costs
                    var xBcur = MatrixHelper.MatVec(Binv, b);
                    var pi = MatrixHelper.MatVec(MatrixHelper.Invert(Transpose(B)), cB);
                    var rN = new double[nonBasic.Length];
                    for (int j = 0; j < nonBasic.Length; j++)
                    {
                        double sum = 0;
                        for (int i = 0; i < m; i++) sum += pi[i] * N[i, j];
                        rN[j] = cN[j] - sum;
                    }

                    int eIdx = -1;
                    double minRC = 0.0;
                    for (int j = 0; j < rN.Length; j++)
                        if (rN[j] < minRC - 1e-12) { minRC = rN[j]; eIdx = j; }

                    if (eIdx == -1)
                    {
                        res.Status = "Optimal";
                        var x = new double[nExt];
                        for (int i = 0; i < m; i++) x[basis[i]] = xBcur[i];
                        double z = 0;
                        for (int j = 0; j < nExt; j++) z += cext[j] * x[j];
                        res.X = x.Take(cf.N).Select(v => Math.Round(v, 3)).ToArray();
                        res.Objective = Math.Round(z, 3);
                        _log.Log($"Optimal after {iter} iterations. Z = {res.Objective:0.###}");
                        for (int i = 0; i < res.X.Length; i++) _log.Log($"x{i+1} = {res.X[i]:0.###}");
                        return res;
                    }

                    int entering = nonBasic[eIdx];
                    var a_enter = new double[m];
                    for (int i = 0; i < m; i++) a_enter[i] = Aext[i, entering];
                    var d = MatrixHelper.MatVec(Binv, a_enter);

                    int leaveRow = -1;
                    double bestTheta = double.PositiveInfinity;
                    for (int i = 0; i < m; i++)
                    {
                        if (d[i] > _eps)
                        {
                            double theta = xBcur[i] / d[i];
                            if (theta < bestTheta) { bestTheta = theta; leaveRow = i; }
                        }
                    }
                    if (leaveRow == -1)
                    {
                        res.Status = "Unbounded";
                        _log.Log("Detected unbounded in revised simplex.");
                        return res;
                    }

                    int leaving = basis[leaveRow];
                    basis[leaveRow] = entering;
                    _log.Log($"Iter {iter}: Enter = {varNames[entering]}, Leave = {varNames[leaving]}, θ = {bestTheta:0.###}");
                }
            }
            catch (Exception ex)
            {
                _log.Log("Error in RevisedSimplex: " + ex.Message);
                res.Status = "Error";
                return res;
            }
        }

        private void BuildExpanded(RevisedCanonicalForm cf, out double[,] Aext, out double[] cext, out double[] b, out int[] basis, out string[] varNames)
        {
            int m = cf.M, n = cf.N;
            int ext = n;
            for (int i = 0; i < m; i++)
            {
                if (cf.Signs[i] == RevisedConstraintSign.LE) ext += 1;
                else if (cf.Signs[i] == RevisedConstraintSign.GE) ext += 2;
                else if (cf.Signs[i] == RevisedConstraintSign.EQ) ext += 1;
            }
            Aext = new double[m, ext];
            cext = new double[ext];
            b = cf.b.ToArray();
            varNames = new string[ext];
            for (int j = 0; j < n; j++) { varNames[j] = $"x{j+1}"; cext[j] = cf.Sense == RevisedProblemSense.Max ? cf.c[j] : -cf.c[j]; }
            basis = new int[m];
            int cur = n;
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++) Aext[i, j] = cf.A[i, j];
                if (cf.Signs[i] == RevisedConstraintSign.LE)
                {
                    Aext[i, cur] = 1; varNames[cur] = $"s{i+1}"; basis[i] = cur; cur++;
                }
                else if (cf.Signs[i] == RevisedConstraintSign.GE)
                {
                    Aext[i, cur] = -1; varNames[cur] = $"e{i+1}"; cur++;
                    Aext[i, cur] = 1; varNames[cur] = $"a{i+1}"; basis[i] = cur; cur++;
                }
                else
                {
                    Aext[i, cur] = 1; varNames[cur] = $"a{i+1}"; basis[i] = cur; cur++;
                }
            }
        }

        private static double[,] Transpose(double[,] A)
        {
            int m = A.GetLength(0), n = A.GetLength(1);
            var T = new double[n, m];
            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    T[j, i] = A[i, j];
            return T;
        }
    }
}