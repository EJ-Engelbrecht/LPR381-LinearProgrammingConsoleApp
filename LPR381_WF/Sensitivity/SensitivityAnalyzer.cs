using LPR381_Solver.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LPR381_Solver.Sensitivity
{
    
    /// Represents the final simplex state needed for sensitivity analysis.
    internal class SimplexState
    {
        public int[] B { get; set; } = new int[0];          // basic variable indices
        public int[] N { get; set; } = new int[0];          // non-basic variable indices
        public double[,] Binv { get; set; } = new double[0, 0]; // inverse of basis matrix
        public double[] xB { get; set; } = new double[0];   // basic values
        public double[] y { get; set; } = new double[0];    // shadow prices
        public double[] rN { get; set; } = new double[0];   // reduced costs
        public double z { get; set; }                       // objective value
        public Sense Sense { get; set; } = Sense.Max;       // problem sense
    }


    /// Provides sensitivity analysis operations.
    internal static class SensitivityAnalyzer
    {
        private static double Dot(double[] a, double[] b)
        {
            double sum = 0;
            for (int i = 0; i < a.Length; i++) sum += a[i] * b[i];
            return sum;
        }

        private static double[] GetColumn(double[,] M, int col)
        {
            int m = M.GetLength(0);
            var v = new double[m];
            for (int i = 0; i < m; i++) v[i] = M[i, col];
            return v;
        }

        private static double[] MultiplyRowByMatrix(double[,] Binv, int rowIndex, double[,] A)
        {
            int m = Binv.GetLength(0);
            int n = A.GetLength(1);
            var res = new double[n];
            for (int j = 0; j < n; j++)
            {
                double s = 0;
                for (int k = 0; k < m; k++) s += Binv[rowIndex, k] * A[k, j];
                res[j] = s;
            }
            return res;
        }

        /// Range for a non-basic variable's objective coefficient.
        public static Tuple<double, double?> ObjectiveCoeffRangeNonbasic(int varIndex, LPModel model, SimplexState S, double[,] A)
        {
            int pos = Array.IndexOf(S.N, varIndex);
            if (pos < 0) throw new ArgumentException("Variable is not non-basic.");
            double rj = S.rN[pos];

            if (S.Sense == Sense.Max)
                return new Tuple<double, double?>(-rj, null); // [ -rj , +∞ )
            else
                return new Tuple<double, double?>(double.NegativeInfinity, rj); // ( -∞ , rj ]
        }

        
        /// RHS sensitivity for constraint k.
        public static Tuple<double, double> RhsRange(int constraintIndex, SimplexState S)
        {
            var u = GetColumn(S.Binv, constraintIndex);
            double min = double.NegativeInfinity, max = double.PositiveInfinity;
            for (int i = 0; i < S.xB.Length; i++)
            {
                if (u[i] > 1e-12) min = Math.Max(min, -S.xB[i] / u[i]);
                else if (u[i] < -1e-12) max = Math.Min(max, -S.xB[i] / u[i]);
            }
            return new Tuple<double, double>(min, max);
        }

        /// Reduced cost of a new variable (activity).
        public static double ReducedCostForNewVar(double[] newColumn, double newCost, SimplexState S)
        {
            return newCost - Dot(S.y, newColumn);
        }
    }
}
