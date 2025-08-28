using System;
using System.Collections.Generic;
using System.Linq;
using LPR381.Core;

namespace LPR381_Solver.Algorithms
{
    public class PrimalSimplex 
    {
        private readonly LPR381.Core.IIterationLogger _log;
        private readonly double _M;
        private readonly double _eps;
        
        public PrimalSimplex(LPR381.Core.IIterationLogger logger, double bigM = 1e6, double eps = 1e-9)
        {
            _log = logger;
            _M = bigM;
            _eps = eps;
        }

        public SolveResult Solve(CanonicalForm cf)
        {
            _log.LogHeader("Primal Simplex (Tableau)");
            var res = new SolveResult();
            
            try
            {
                BuildTableau(cf, out var T, out var basis, out var varNames, out int objRow, out int rhsCol);
                PrintTableau(T, objRow, rhsCol, varNames, basis, 0);

                int it = 0;
                while (it < 100)
                {
                    it++;
                    int enter = ChooseEntering(T, objRow);
                    if (enter < 0) break;

                    int leave = ChooseLeaving(T, enter, rhsCol, out bool unbounded);
                    if (unbounded)
                    {
                        res.Status = "Unbounded";
                        _log.Log("Problem is unbounded");
                        return res;
                    }

                    Pivot(T, leave, enter);
                    basis[leave] = enter;
                    _log.Log($"\nIteration {it} Summary:");
                    _log.Log($"  Entering variable: {varNames[enter]}");
                    _log.Log($"  Leaving variable : {varNames[basis[leave]]}");
                    _log.Log($"  Pivot element    : ({leave+1}, {enter+1}) = {T[leave+1, enter]:F2}");
                    PrintTableau(T, objRow, rhsCol, varNames, basis, it);
                }

                res.Status = "Optimal";
                res.Iterations = it;
                var x = new double[cf.N];
                
                for (int i = 0; i < basis.Length; i++)
                {
                    int col = basis[i];
                    if (col < cf.N) x[col] = T[i + 1, rhsCol];
                }

                double z = T[objRow, rhsCol];
                if (cf.Sense == ProblemSense.Min) z = -z;

                res.X = x.Select(v => Math.Round(v, 3)).ToArray();
                res.Objective = Math.Round(z, 3);
                
                _log.Log("\n=== FINAL SOLUTION ===");
                _log.Log($"Status         : {res.Status}");
                _log.Log($"Objective Value: {res.Objective:F2}");
                _log.Log($"Iterations     : {res.Iterations}");
                _log.Log("Variables      :");
                for (int i = 0; i < x.Length; i++) 
                    _log.Log($"  x{i+1} = {res.X[i]:F2}");
                    
                return res;
            }
            catch (Exception ex)
            {
                _log.Log($"Error in PrimalSimplex: {ex.Message}");
                res.Status = "Error";
                return res;
            }
        }

        private void BuildTableau(CanonicalForm cf, out double[,] T, out int[] basis, out string[] varNames, out int objRow, out int rhsCol)
        {
            int m = cf.M, n = cf.N;
            int slacks = cf.Signs.Count(s => s == ConstraintSign.LE);
            int totalVars = n + slacks;
            
            T = new double[m + 1, totalVars + 1];
            basis = new int[m];
            varNames = new string[totalVars];
            objRow = 0;
            rhsCol = totalVars;
            
            for (int j = 0; j < n; j++) varNames[j] = $"x{j+1}";
            for (int j = 0; j < slacks; j++) varNames[n + j] = $"s{j+1}";
            
            // Objective row
            for (int j = 0; j < n; j++)
                T[0, j] = cf.Sense == ProblemSense.Max ? -cf.c[j] : cf.c[j];
            
            // Constraint rows
            int slackIdx = 0;
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                    T[i + 1, j] = cf.A[i, j];
                    
                if (cf.Signs[i] == ConstraintSign.LE)
                {
                    T[i + 1, n + slackIdx] = 1;
                    basis[i] = n + slackIdx;
                    slackIdx++;
                }
                
                T[i + 1, rhsCol] = cf.b[i];
            }
        }

        private int ChooseEntering(double[,] T, int objRow)
        {
            int cols = T.GetLength(1) - 1;
            double mostNegative = 0;
            int enteringVar = -1;
            
            for (int j = 0; j < cols; j++)
            {
                if (T[objRow, j] < mostNegative - _eps)
                {
                    mostNegative = T[objRow, j];
                    enteringVar = j;
                }
            }
            
            return enteringVar;
        }

        private int ChooseLeaving(double[,] T, int enter, int rhsCol, out bool unbounded)
        {
            int rows = T.GetLength(0);
            double minRatio = double.PositiveInfinity;
            int leavingVar = -1;
            unbounded = false;
            
            for (int i = 1; i < rows; i++)
            {
                if (T[i, enter] > _eps)
                {
                    double ratio = T[i, rhsCol] / T[i, enter];
                    if (ratio < minRatio)
                    {
                        minRatio = ratio;
                        leavingVar = i - 1;
                    }
                }
            }
            
            if (leavingVar == -1) unbounded = true;
            return leavingVar;
        }

        private void Pivot(double[,] T, int pivotRow, int pivotCol)
        {
            int rows = T.GetLength(0);
            int cols = T.GetLength(1);
            pivotRow++; // Adjust for tableau indexing
            
            double pivot = T[pivotRow, pivotCol];
            
            // Normalize pivot row
            for (int j = 0; j < cols; j++)
                T[pivotRow, j] /= pivot;
            
            // Eliminate column
            for (int i = 0; i < rows; i++)
            {
                if (i != pivotRow)
                {
                    double multiplier = T[i, pivotCol];
                    for (int j = 0; j < cols; j++)
                        T[i, j] -= multiplier * T[pivotRow, j];
                }
            }
        }

        private void PrintTableau(double[,] T, int objRow, int rhsCol, string[] varNames, int[] basis, int iteration)
        {
            int rows = T.GetLength(0);
            int cols = T.GetLength(1);
            
            _log.Log($"\n=== ITERATION {iteration} ===");
            _log.Log($"Basis: [{string.Join(", ", basis.Select(b => varNames[b]))}]");
            _log.Log($"z-value: {T[objRow, rhsCol]:F2}");
            _log.Log(new string('-', 60));
            
            // Header
            string header = "      ";
            for (int j = 0; j < cols - 1; j++)
                header += $"{varNames[j],8}";
            header += "     RHS";
            _log.Log(header);
            
            // Objective row
            string objStr = "z   : ";
            for (int j = 0; j < cols; j++)
                objStr += $"{T[objRow, j],8:F2}";
            _log.Log(objStr);
            
            // Constraint rows
            for (int i = 1; i < rows; i++)
            {
                string rowStr = $"{varNames[basis[i-1]],4}: ";
                for (int j = 0; j < cols; j++)
                    rowStr += $"{T[i, j],8:F2}";
                _log.Log(rowStr);
            }
        }
    }
}