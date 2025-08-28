using System;
using System.Collections.Generic;
using System.Linq;
using LPR381.Core;

namespace LPR381_Solver.Algorithms
{
    public class RevisedSimplex
    {
        private readonly LPR381.Core.IIterationLogger _log;
        private readonly double _eps;
        
        public RevisedSimplex(LPR381.Core.IIterationLogger logger, double eps = 1e-9)
        {
            _log = logger;
            _eps = eps;
        }

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

                    for (int i = 0; i < m; i++)
                    {
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
                }
            }
        }

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