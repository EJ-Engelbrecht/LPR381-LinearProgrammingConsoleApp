using System;
using System.Collections.Generic;
using System.Linq;

namespace LPR381.Core
{
    public enum ProblemSense { Max, Min }
    public enum ConstraintSign { LE, GE, EQ }
    public enum VarType { URS, Plus, Minus, Int, Bin }

    public class CanonicalForm
    {
        // Maximize/Minimize: c^T x  subject to  A x (<=,=,>=) b ; var types
        public ProblemSense Sense { get; set; } = ProblemSense.Max;
        public double[,] A { get; set; }       // m x n
        public double[] b { get; set; }        // m
        public double[] c { get; set; }        // n
        public ConstraintSign[] Signs { get; set; } // m
        public VarType[] VariableTypes { get; set; } // n

        public string[] VariableNames { get; set; } // optional pretty names

        public int M => A.GetLength(0);
        public int N => A.GetLength(1);

        public CanonicalForm Clone()
        {
            var m = M; var n = N;
            var A2 = new double[m, n];
            Array.Copy(A, A2, A.Length);
            return new CanonicalForm {
                Sense = Sense,
                A = A2,
                b = (double[])b.Clone(),
                c = (double[])c.Clone(),
                Signs = (ConstraintSign[])Signs.Clone(),
                VariableTypes = (VarType[])VariableTypes.Clone(),
                VariableNames = VariableNames == null ? null : (string[])VariableNames.Clone()
            };
        }
    }

    public class SolveResult
    {
        public string Status { get; set; } = "Unknown"; // Optimal, Infeasible, Unbounded
        public double Objective { get; set; }
        public double[] X { get; set; } = Array.Empty<double>();
        public List<string> LogLines { get; set; } = new List<string>();
        public int Iterations { get; set; } = 0;
    }

    public interface IIterationLogger
    {
        void Log(string line);
        void LogHeader(string title);
        void LogMatrix(string title, double[,] mat, int round = 3, string[] colNames = null, string[] rowNames = null);
        void LogVector(string title, double[] vec, int round = 3, string[] names = null);
    }

    public static class Rounding
    {
        public static double R3(this double x) => Math.Round(x, 3, MidpointRounding.AwayFromZero);
        public static double[] R3(this double[] v) => v.Select(R3).ToArray();
        public static double[,] R3(this double[,] A)
        {
            var m = A.GetLength(0);
            var n = A.GetLength(1);
            var B = new double[m, n];
            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    B[i, j] = A[i, j].R3();
            return B;
        }
    }
}