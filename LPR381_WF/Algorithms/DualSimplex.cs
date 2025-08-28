using System;
using System.Collections.Generic;
using System.Linq;
using LPR381.Core;

namespace LPR381_Solver.Algorithms
{
    public class DualSimplex
    {
        private readonly LPR381.Core.IIterationLogger _log;
        private readonly double _eps;

        public DualSimplex(LPR381.Core.IIterationLogger logger, double eps = 1e-9)
        {
            _log = logger;
            _eps = eps;
        }

        public SolveResult Solve(CanonicalForm cf)
        {
            _log.LogHeader("Dual Simplex Algorithm");
            var res = new SolveResult();
            
            try
            {
                BuildTableau(cf, out var T, out var basis, out var varNames, out int objRow, out int rhsCol);
                PrintTableau(T, objRow, rhsCol, varNames, basis, 0);

                int it = 0;
                while (it < 100)
                {
                    it++;
                    
                    // Find leaving variable (most negative RHS)
                    int leavingRow = FindLeavingVariable(T, rhsCol);
                    if (leavingRow == -1)
                    {
                        // All RHS >= 0, solution is feasible and optimal
                        break;
                    }

                    // Find entering variable (dual ratio test)
                    int enteringCol = FindEnteringVariable(T, leavingRow, objRow);
                    if (enteringCol == -1)
                    {
                        res.Status = "Infeasible";
                        _log.Log("Problem is infeasible (no entering variable found)");
                        return res;
                    }

                    _log.Log($"Iteration {it}: Leave={varNames[basis[leavingRow]]}, Enter={varNames[enteringCol]}");
                    
                    // Perform pivot operation
                    Pivot(T, leavingRow + 1, enteringCol); // +1 for tableau indexing
                    basis[leavingRow] = enteringCol;
                    
                    PrintTableau(T, objRow, rhsCol, varNames, basis, it);
                }

                res.Status = "Optimal";
                res.Iterations = it;
                var x = new double[cf.N];
                
                // Extract solution
                for (int i = 0; i < basis.Length; i++)
                {
                    int col = basis[i];
                    if (col < cf.N) 
                        x[col] = T[i + 1, rhsCol];
                }

                double z = T[objRow, rhsCol];
                if (cf.Sense == ProblemSense.Min) z = -z;

                res.X = x.Select(v => Math.Round(v, 3)).ToArray();
                res.Objective = Math.Round(z, 3);
                
                _log.Log($"\nFinal Solution: Status={res.Status}, Z={res.Objective:F3}");
                for (int i = 0; i < x.Length; i++) 
                    _log.Log($"x{i+1} = {res.X[i]:F3}");
                    
                return res;
            }
            catch (Exception ex)
            {
                _log.Log($"Error in DualSimplex: {ex.Message}");
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
            
            // Objective row (for dual simplex, we need dual feasibility)
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

        private int FindLeavingVariable(double[,] T, int rhsCol)
        {
            int rows = T.GetLength(0);
            double mostNegative = 0;
            int leavingRow = -1;
            
            for (int i = 1; i < rows; i++)
            {
                if (T[i, rhsCol] < mostNegative - _eps)
                {
                    mostNegative = T[i, rhsCol];
                    leavingRow = i - 1; // Adjust for basis indexing
                }
            }
            
            return leavingRow;
        }

        private int FindEnteringVariable(double[,] T, int leavingRow, int objRow)
        {
            int cols = T.GetLength(1) - 1; // Exclude RHS column
            double bestRatio = double.PositiveInfinity;
            int enteringCol = -1;
            
            int pivotRow = leavingRow + 1; // Adjust for tableau indexing
            
            for (int j = 0; j < cols; j++)
            {
                double a_rj = T[pivotRow, j];
                if (a_rj < -_eps) // Negative coefficient in leaving row
                {
                    double c_j = T[objRow, j]; // Reduced cost
                    double ratio = Math.Abs(c_j / a_rj);
                    
                    if (ratio < bestRatio - _eps)
                    {
                        bestRatio = ratio;
                        enteringCol = j;
                    }
                }
            }
            
            return enteringCol;
        }

        private void Pivot(double[,] T, int pivotRow, int pivotCol)
        {
            int rows = T.GetLength(0);
            int cols = T.GetLength(1);
            
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
            
            _log.Log($"\nIteration {iteration} Tableau:");
            
            // Header
            string header = "     ";
            for (int j = 0; j < cols - 1; j++)
                header += $"{varNames[j],8}";
            header += "     RHS";
            _log.Log(header);
            
            // Objective row
            string objStr = "z  : ";
            for (int j = 0; j < cols; j++)
                objStr += $"{T[objRow, j],8:F2}";
            _log.Log(objStr);
            
            // Constraint rows
            for (int i = 1; i < rows; i++)
            {
                string rowStr = $"{varNames[basis[i-1]],3}: ";
                for (int j = 0; j < cols; j++)
                    rowStr += $"{T[i, j],8:F2}";
                _log.Log(rowStr);
            }
        }
    }
}