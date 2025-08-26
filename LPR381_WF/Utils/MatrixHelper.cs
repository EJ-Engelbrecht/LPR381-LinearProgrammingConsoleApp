using System;
using System.Linq;

namespace LPR381_WF.Utils
{
    public static class MatrixHelper
    {
        public static double[,] Identity(int n)
        {
            var I = new double[n, n];
            for (int i = 0; i < n; i++) I[i, i] = 1.0;
            return I;
        }

        public static double[] MatVec(double[,] A, double[] x)
        {
            int m = A.GetLength(0), n = A.GetLength(1);
            var y = new double[m];
            for (int i = 0; i < m; i++)
            {
                double s = 0;
                for (int j = 0; j < n; j++) s += A[i, j] * x[j];
                y[i] = s;
            }
            return y;
        }

        public static double[,] MatMul(double[,] A, double[,] B)
        {
            int m = A.GetLength(0), k = A.GetLength(1), n = B.GetLength(1);
            var C = new double[m, n];
            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                {
                    double s = 0;
                    for (int t = 0; t < k; t++) s += A[i, t] * B[t, j];
                    C[i, j] = s;
                }
            return C;
        }

        public static double[,] Invert(double[,] A)
        {
            int n = A.GetLength(0);
            var aug = new double[n, 2 * n];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    aug[i, j] = A[i, j];
            for (int i = 0; i < n; i++)
                aug[i, n + i] = 1.0;

            for (int i = 0; i < n; i++)
            {
                int piv = i;
                double best = Math.Abs(aug[i, i]);
                for (int r = i + 1; r < n; r++)
                {
                    double v = Math.Abs(aug[r, i]);
                    if (v > best) { best = v; piv = r; }
                }
                if (Math.Abs(best) < 1e-12) throw new InvalidOperationException("Singular matrix.");
                if (piv != i) SwapRows(aug, i, piv);

                double diag = aug[i, i];
                for (int j = 0; j < 2 * n; j++) aug[i, j] /= diag;

                for (int r = 0; r < n; r++)
                {
                    if (r == i) continue;
                    double f = aug[r, i];
                    if (Math.Abs(f) < 1e-15) continue;
                    for (int j = 0; j < 2 * n; j++) aug[r, j] -= f * aug[i, j];
                }
            }

            var inv = new double[n, n];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    inv[i, j] = aug[i, n + j];
            return inv;
        }

        static void SwapRows(double[,] A, int r1, int r2)
        {
            int m = A.GetLength(1);
            for (int j = 0; j < m; j++)
            {
                double t = A[r1, j];
                A[r1, j] = A[r2, j];
                A[r2, j] = t;
            }
        }
    }
}
