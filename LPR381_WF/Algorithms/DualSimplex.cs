using System;
using System.Collections.Generic;

namespace LPR381_Solver.Algorithms
{
    // Core data structures for Dual Simplex
    public class IterationSnapshot
    {
        public int Iter { get; set; }
        public string Label { get; set; }
        public double[,] Tableau { get; set; }
        public int? EnteringCol { get; set; }
        public int? LeavingRow { get; set; }
        public int? PivotRow { get; set; }
        public int? PivotCol { get; set; }
        public double[] Theta { get; set; }
        public double Objective { get; set; }
        
        public IterationSnapshot(int iter, string label, double[,] tableau, int? enteringCol, int? leavingRow, int? pivotRow, int? pivotCol, double[] theta, double objective)
        {
            Iter = iter;
            Label = label;
            Tableau = tableau;
            EnteringCol = enteringCol;
            LeavingRow = leavingRow;
            PivotRow = pivotRow;
            PivotCol = pivotCol;
            Theta = theta;
            Objective = objective;
        }
    }

    public interface ISimplexSolver
    {
        event Action<IterationSnapshot> OnIter;
        SimplexResult Solve(LpModel model, TableauState warmStart = null, double tol = 1e-9);
    }

    public class SimplexResult
    {
        public Status Status { get; set; }
        public double Objective { get; set; }
        public double[] Primal { get; set; }
        public double[] ReducedCosts { get; set; }
        public double[,] FinalTableau { get; set; }
        public TableauState State { get; set; }
        
        public SimplexResult(Status status, double objective, double[] primal, double[] reducedCosts, double[,] finalTableau, TableauState state)
        {
            Status = status;
            Objective = objective;
            Primal = primal;
            ReducedCosts = reducedCosts;
            FinalTableau = finalTableau;
            State = state;
        }
    }

    public enum Status { Optimal, Infeasible, Unbounded }

    public class LpModel
    {
        public double[,] A { get; set; }
        public double[] b { get; set; }
        public double[] c { get; set; }
        public int nVars { get; set; }
        public HashSet<int> IntegerVarIdxs { get; set; }
        
        public LpModel(double[,] a, double[] b, double[] c, int nVars, HashSet<int> integerVarIdxs)
        {
            A = a;
            this.b = b;
            this.c = c;
            this.nVars = nVars;
            IntegerVarIdxs = integerVarIdxs;
        }
    }

    public class TableauState
    {
        public double[,] Tableau { get; set; }
        public int[] Basis { get; set; }
        
        public TableauState(double[,] tableau, int[] basis)
        {
            Tableau = tableau;
            Basis = basis;
        }
    }

    // Dual Simplex Implementation
    public sealed class DualSimplex : ISimplexSolver
    {
        public event Action<IterationSnapshot> OnIter;

        public SimplexResult Solve(LpModel model, TableauState warmStart = null, double tol = 1e-9)
        {
            if (warmStart == null)
                throw new ArgumentException("DualSimplex requires warm start");

            var T = (double[,])warmStart.Tableau.Clone();
            int rows = T.GetLength(0), cols = T.GetLength(1);
            int iter = 0;

            while (true)
            {
                // Find leaving row (most negative RHS)
                int rhsCol = cols - 1;
                int leavingRow = -1;
                double minRhs = 0;

                for (int i = 1; i < rows; i++)
                {
                    if (T[i, rhsCol] < minRhs - tol)
                    {
                        minRhs = T[i, rhsCol];
                        leavingRow = i;
                    }
                }

                if (leavingRow == -1)
                {
                    // Feasible - optimal
                    var primal = ExtractPrimal(T, model.nVars);
                    return new SimplexResult(Status.Optimal, T[0, rhsCol], primal, 
                        new double[0], T, warmStart);
                }

                // Find entering column (min ratio test)
                int enteringCol = -1;
                double bestRatio = double.PositiveInfinity;

                for (int j = 1; j < cols - 1; j++)
                {
                    double a = T[leavingRow, j];
                    if (a < -tol)
                    {
                        double reducedCost = T[0, j];
                        double ratio = reducedCost / Math.Abs(a);
                        
                        if (ratio < bestRatio - 1e-12 || 
                            (Math.Abs(ratio - bestRatio) < 1e-12 && j < enteringCol))
                        {
                            bestRatio = ratio;
                            enteringCol = j;
                        }
                    }
                }

                if (enteringCol == -1)
                {
                    // Infeasible
                    return new SimplexResult(Status.Infeasible, 0, new double[0], 
                        new double[0], T, warmStart);
                }

                // Pivot
                Pivot(T, leavingRow, enteringCol);

                // Fire iteration event
                var theta = ComputeTheta(T, enteringCol);
                if (OnIter != null)
                    OnIter.Invoke(new IterationSnapshot(++iter, "D-" + iter.ToString(), (double[,])T.Clone(),
                        enteringCol, leavingRow, leavingRow, enteringCol, theta, T[0, rhsCol]));
            }
        }

        private void Pivot(double[,] T, int pivotRow, int pivotCol)
        {
            int rows = T.GetLength(0), cols = T.GetLength(1);
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

        private double[] ComputeTheta(double[,] T, int enteringCol)
        {
            int rows = T.GetLength(0);
            var theta = new double[rows];
            int rhsCol = T.GetLength(1) - 1;

            for (int i = 1; i < rows; i++)
            {
                if (T[i, enteringCol] > 1e-9)
                    theta[i] = T[i, rhsCol] / T[i, enteringCol];
                else
                    theta[i] = double.PositiveInfinity;
            }
            return theta;
        }

        private double[] ExtractPrimal(double[,] T, int nVars)
        {
            var solution = new double[nVars];
            int rows = T.GetLength(0), cols = T.GetLength(1);
            int rhsCol = cols - 1;

            // Find basic variables
            for (int j = 1; j <= nVars && j < cols - 1; j++)
            {
                int basicRow = -1;
                bool isBasic = true;

                for (int i = 1; i < rows; i++)
                {
                    if (Math.Abs(T[i, j] - 1.0) < 1e-9)
                    {
                        if (basicRow == -1)
                            basicRow = i;
                        else
                        {
                            isBasic = false;
                            break;
                        }
                    }
                    else if (Math.Abs(T[i, j]) > 1e-9)
                    {
                        isBasic = false;
                        break;
                    }
                }

                if (isBasic && basicRow != -1)
                    solution[j - 1] = T[basicRow, rhsCol];
            }

            return solution;
        }
    }
}