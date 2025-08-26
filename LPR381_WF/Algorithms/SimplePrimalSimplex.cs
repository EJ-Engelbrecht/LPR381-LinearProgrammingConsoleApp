using System;
using System.Collections.Generic;
using System.Linq;
using LPR381.Core;

namespace LPR381_Solver.Algorithms
{
    public class SimplePrimalSimplex
    {
        private readonly LPR381.Core.IIterationLogger _log;
        private readonly double _eps;

        public SimplePrimalSimplex(LPR381.Core.IIterationLogger logger, double eps = 1e-9)
        {
            _log = logger;
            _eps = eps;
        }

        public SolveResult Solve(CanonicalForm cf)
        {
            _log.LogHeader("Primal Simplex Algorithm");
            var result = new SolveResult();
            
            try
            {
                _log.Log("\n=== CANONICAL FORM ===");
                DisplayCanonicalForm(cf);
                
                _log.Log("\n=== TABLEAU ITERATIONS ===");
                
                // Create initial tableau
                var tableau = CreateInitialTableau(cf);
                int iteration = 0;
                
                _log.Log($"\nIteration {iteration}: Initial Tableau");
                DisplayTableau(tableau, cf.N);
                
                // Solve using simplex iterations
                while (iteration < 10) // Limit iterations for demo
                {
                    iteration++;
                    
                    // Check optimality
                    int enteringVar = FindEnteringVariable(tableau);
                    if (enteringVar == -1)
                    {
                        _log.Log("\nOptimal solution found!");
                        break;
                    }
                    
                    // Find leaving variable
                    int leavingVar = FindLeavingVariable(tableau, enteringVar);
                    if (leavingVar == -1)
                    {
                        result.Status = "Unbounded";
                        _log.Log("Problem is unbounded");
                        return result;
                    }
                    
                    _log.Log($"\nIteration {iteration}: Pivot on row {leavingVar}, column {enteringVar}");
                    
                    // Perform pivot operation
                    Pivot(tableau, leavingVar, enteringVar);
                    
                    // Display updated tableau
                    DisplayTableau(tableau, cf.N);
                }
                
                // Extract solution
                result = ExtractSolution(tableau, cf.N, iteration);
                
                _log.Log($"\n=== FINAL SOLUTION ===");
                _log.Log($"Status: {result.Status}");
                _log.Log($"Objective Value: {result.Objective:F3}");
                _log.Log($"Iterations: {result.Iterations}");
                
                if (result.X != null)
                {
                    for (int i = 0; i < result.X.Length; i++)
                    {
                        _log.Log($"x{i+1} = {result.X[i]:F3}");
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Log($"Error: {ex.Message}");
                result.Status = "Error";
            }
            
            return result;
        }
        
        private void DisplayCanonicalForm(CanonicalForm cf)
        {
            _log.Log($"Objective: {cf.Sense}");
            _log.Log($"Variables: {cf.N}, Constraints: {cf.M}");
            
            // Display objective coefficients
            string objStr = "Objective coefficients: ";
            for (int j = 0; j < cf.N; j++)
            {
                objStr += $"c{j+1}={cf.c[j]:F3} ";
            }
            _log.Log(objStr);
            
            // Display constraints
            for (int i = 0; i < cf.M; i++)
            {
                string constStr = $"Constraint {i+1}: ";
                for (int j = 0; j < cf.N; j++)
                {
                    constStr += $"{cf.A[i,j]:F3}*x{j+1} ";
                    if (j < cf.N-1) constStr += "+ ";
                }
                constStr += $"{GetSignString(cf.Signs[i])} {cf.b[i]:F3}";
                _log.Log(constStr);
            }
        }
        
        private string GetSignString(ConstraintSign sign)
        {
            return sign == ConstraintSign.LE ? "<=" :
                   sign == ConstraintSign.GE ? ">=" : "=";
        }
        
        private double[,] CreateInitialTableau(CanonicalForm cf)
        {
            // Simple tableau for demonstration - assumes all <= constraints
            int rows = cf.M + 1; // +1 for objective
            int cols = cf.N + cf.M + 1; // original vars + slack vars + RHS
            
            var tableau = new double[rows, cols];
            
            // Objective row
            for (int j = 0; j < cf.N; j++)
            {
                tableau[0, j] = cf.Sense == ProblemSense.Max ? -cf.c[j] : cf.c[j];
            }
            
            // Constraint rows
            for (int i = 0; i < cf.M; i++)
            {
                // Original variables
                for (int j = 0; j < cf.N; j++)
                {
                    tableau[i + 1, j] = cf.A[i, j];
                }
                // Slack variable
                tableau[i + 1, cf.N + i] = 1;
                // RHS
                tableau[i + 1, cols - 1] = cf.b[i];
            }
            
            return tableau;
        }
        
        private void DisplayTableau(double[,] tableau, int originalVars)
        {
            int rows = tableau.GetLength(0);
            int cols = tableau.GetLength(1);
            
            // Header
            string header = "     ";
            for (int j = 0; j < originalVars; j++) header += $"   x{j+1}  ";
            for (int j = originalVars; j < cols-1; j++) header += $"   s{j-originalVars+1}  ";
            header += "   RHS  ";
            _log.Log(header);
            
            // Rows
            for (int i = 0; i < rows; i++)
            {
                string rowStr = i == 0 ? "z  : " : $"s{i}: ";
                for (int j = 0; j < cols; j++)
                {
                    rowStr += $"{tableau[i, j],7:F2}";
                }
                _log.Log(rowStr);
            }
        }
        
        private int FindEnteringVariable(double[,] tableau)
        {
            int cols = tableau.GetLength(1);
            double mostNegative = 0;
            int enteringVar = -1;
            
            for (int j = 0; j < cols - 1; j++)
            {
                if (tableau[0, j] < mostNegative)
                {
                    mostNegative = tableau[0, j];
                    enteringVar = j;
                }
            }
            
            return enteringVar;
        }
        
        private int FindLeavingVariable(double[,] tableau, int enteringVar)
        {
            int rows = tableau.GetLength(0);
            int cols = tableau.GetLength(1);
            double minRatio = double.PositiveInfinity;
            int leavingVar = -1;
            
            for (int i = 1; i < rows; i++)
            {
                if (tableau[i, enteringVar] > 0)
                {
                    double ratio = tableau[i, cols - 1] / tableau[i, enteringVar];
                    if (ratio < minRatio)
                    {
                        minRatio = ratio;
                        leavingVar = i;
                    }
                }
            }
            
            return leavingVar;
        }
        
        private void Pivot(double[,] tableau, int pivotRow, int pivotCol)
        {
            int rows = tableau.GetLength(0);
            int cols = tableau.GetLength(1);
            double pivot = tableau[pivotRow, pivotCol];
            
            // Normalize pivot row
            for (int j = 0; j < cols; j++)
            {
                tableau[pivotRow, j] /= pivot;
            }
            
            // Eliminate column
            for (int i = 0; i < rows; i++)
            {
                if (i != pivotRow)
                {
                    double multiplier = tableau[i, pivotCol];
                    for (int j = 0; j < cols; j++)
                    {
                        tableau[i, j] -= multiplier * tableau[pivotRow, j];
                    }
                }
            }
        }
        
        private SolveResult ExtractSolution(double[,] tableau, int originalVars, int iterations)
        {
            var result = new SolveResult();
            result.Status = "Optimal";
            result.Iterations = iterations;
            
            int cols = tableau.GetLength(1);
            result.Objective = Math.Round(tableau[0, cols - 1], 3);
            
            result.X = new double[originalVars];
            // For simplicity, assume basic solution
            for (int j = 0; j < originalVars; j++)
            {
                result.X[j] = 0; // Non-basic variables are 0
            }
            
            return result;
        }
    }
}