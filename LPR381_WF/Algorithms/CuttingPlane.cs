using System;
using System.Collections.Generic;
using System.Linq;
using LPR381.Core;

namespace LPR381_Solver.Algorithms
{
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