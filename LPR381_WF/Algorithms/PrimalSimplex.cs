using System;
using System.Collections.Generic;
using System.Linq;
using LPR381.Core;

namespace LPR381_Solver.Algorithms
{
    public class PrimalSimplex
    {
        private readonly IIterationLogger _log;
        private readonly double _M;
        private readonly double _eps;

        public PrimalSimplex(IIterationLogger logger, double bigM = 1e6, double eps = 1e-9)
        {
            _log = logger;
            _M = bigM;
            _eps = eps;
        }

        public static bool IsInteger(double v, double eps = 1e-9) => Math.Abs(v - Math.Round(v)) <= eps;

        public static int ChooseMostFractional(double[] x, IEnumerable<int> integerVarIdxs)
        {
            int best = -1; double bestDist = -1;
            foreach (var k in integerVarIdxs)
            {
                if (k >= x.Length) continue;
                double f = x[k] - Math.Floor(x[k]);
                double d = Math.Min(f, 1 - f);
                if (d > bestDist && d > 1e-9) { bestDist = d; best = k; }
            }
            return best;
        }

        public SolveResult Solve(CanonicalForm cf)
        {
            _log.LogHeader("Primal Simplex (Two-Phase)");
            var res = new SolveResult();
            try
            {
                bool needsPhaseI = cf.Signs.Any(s => s == ConstraintSign.GE || s == ConstraintSign.EQ);

                if (needsPhaseI)
                {
                    _log.Log("Phase I: Minimizing sum of artificial variables");
                    var phaseIResult = SolvePhaseI(cf);
                    if (phaseIResult.Status != "Optimal" || Math.Abs(phaseIResult.Objective) > _eps)
                    {
                        res.Status = "Infeasible";
                        _log.Log($"Phase I result: {phaseIResult.Objective:F6} > 0 -> Infeasible");
                        return res;
                    }
                    _log.Log("Phase I completed successfully. Starting Phase II.");
                }

                return SolvePhaseII(cf);
            }
            catch (Exception ex)
            {
                _log.Log($"Error in PrimalSimplex: {ex.Message}");
                res.Status = "Error";
                return res;
            }
        }

        private SolveResult SolvePhaseI(CanonicalForm cf)
        {
            BuildPhaseITableau(cf, out var T, out var basis, out var headers, out int objRow, out int rhsCol);
            return SolveTableau(T, basis, headers, objRow, rhsCol, "Phase I");
        }

        private SolveResult SolvePhaseII(CanonicalForm cf)
        {
            BuildPhaseIITableau(cf, out var T, out var basis, out var headers, out int objRow, out int rhsCol);
            return SolveTableau(T, basis, headers, objRow, rhsCol, "Phase II");
        }

        private SolveResult SolveTableau(double[,] T, int[] basis, string[] headers, int objRow, int rhsCol, string phase)
        {
            var res = new SolveResult();
            PrintTableau(T, objRow, rhsCol, headers, basis, phase);

            int it = 0;
            while (true)
            {
                it++;
                int enterCol = ChooseEntering(T, objRow);
                if (enterCol < 0) break;

                int leaveRow = ChooseLeaving(T, enterCol, rhsCol, out bool unbounded);
                if (unbounded)
                {
                    res.Status = "Unbounded";
                    _log.Log("Detected unbounded: no positive entry in entering column.");
                    return res;
                }

                string leavingVar = GetBasisVariableName(basis, leaveRow, headers);
                _log.Log($"Entering variable: {headers[enterCol]}; Leaving variable: {leavingVar}; Pivot element: ({leaveRow}, {enterCol})");
                _log.Log($"Pivot Column: {headers[enterCol]}");
                _log.Log($"Pivot Row: {leavingVar}");

                Pivot(T, leaveRow, enterCol);
                basis[leaveRow] = enterCol;

                PrintTableau(T, objRow, rhsCol, headers, basis, $"{phase} - Iteration {it}");
                if (it > 10000) throw new Exception("Iteration limit exceeded.");
            }

            res.Status = "Optimal";
            res.Iterations = it;

            int numOrigVars = headers.Count(h => h.StartsWith("x"));
            var x = new double[numOrigVars];
            for (int i = 0; i < basis.Length; i++)
            {
                int col = basis[i];
                if (col < headers.Length && headers[col].StartsWith("x"))
                {
                    int varIndex = int.Parse(headers[col].Substring(1)) - 1;
                    if (varIndex < x.Length) x[varIndex] = T[i, rhsCol];
                }
            }

            double z = T[objRow, rhsCol];
            res.X = x.ToArray();
            res.Objective = z;

            _log.Log($"\nFinal Solution Summary:");
            _log.Log($"Status: {res.Status}, Z = {res.Objective:0.###}");
            for (int i = 0; i < x.Length; i++)
                _log.Log($"x{i + 1} = {res.X[i]:0.###}");

            return res;
        }

        private void BuildPhaseITableau(CanonicalForm cf, out double[,] T, out int[] basis, out string[] headers, out int objRow, out int rhsCol)
        {
            int m = cf.M, n = cf.N;
            int artificials = cf.Signs.Count(s => s == ConstraintSign.GE || s == ConstraintSign.EQ);
            int slacks = cf.Signs.Count(s => s == ConstraintSign.LE);
            int surplus = cf.Signs.Count(s => s == ConstraintSign.GE);

            int totalCols = n + slacks + surplus + artificials + 1;
            int totalRows = m + 1;

            T = new double[totalRows, totalCols];
            basis = new int[m];

            var headerList = new List<string>();
            for (int j = 0; j < n; j++) headerList.Add($"x{j + 1}");

            int slackIdx = 0, surplusIdx = 0, artIdx = 0;
            for (int i = 0; i < m; i++)
            {
                if (cf.Signs[i] == ConstraintSign.LE)
                {
                    headerList.Add($"c{++slackIdx}");
                    basis[i] = headerList.Count - 1;
                }
                else if (cf.Signs[i] == ConstraintSign.GE)
                {
                    headerList.Add($"c{++surplusIdx}");
                    headerList.Add($"a{++artIdx}");
                    basis[i] = headerList.Count - 1;
                }
                else
                {
                    headerList.Add($"a{++artIdx}");
                    basis[i] = headerList.Count - 1;
                }
            }
            headerList.Add("RHS");
            headers = headerList.ToArray();

            int colIdx = n;
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++) T[i, j] = cf.A[i, j];

                if (cf.Signs[i] == ConstraintSign.LE)
                {
                    T[i, colIdx++] = 1;
                }
                else if (cf.Signs[i] == ConstraintSign.GE)
                {
                    T[i, colIdx++] = -1;
                    T[i, colIdx++] = 1;
                }
                else
                {
                    T[i, colIdx++] = 1;
                }

                T[i, totalCols - 1] = cf.b[i];
            }

            objRow = m;
            rhsCol = totalCols - 1;
            for (int j = 0; j < headers.Length - 1; j++)
                if (headers[j].StartsWith("a")) T[objRow, j] = 1;

            for (int i = 0; i < m; i++)
            {
                if (headers[basis[i]].StartsWith("a"))
                {
                    for (int j = 0; j < totalCols; j++)
                        T[objRow, j] -= T[i, j];
                }
            }
        }

        private void BuildPhaseIITableau(CanonicalForm cf, out double[,] T, out int[] basis, out string[] headers, out int objRow, out int rhsCol)
        {
            int m = cf.M, n = cf.N;
            int cCount = cf.Signs.Count(s => s == ConstraintSign.LE) + cf.Signs.Count(s => s == ConstraintSign.GE);

            int totalCols = n + cCount + 1;
            int totalRows = m + 1;

            T = new double[totalRows, totalCols];
            basis = new int[m];

            var headerList = new List<string>();
            for (int j = 0; j < n; j++) headerList.Add($"x{j + 1}");

            int cIdx = 0;
            for (int i = 0; i < m; i++)
            {
                if (cf.Signs[i] == ConstraintSign.LE)
                {
                    headerList.Add($"c{++cIdx}");
                    basis[i] = headerList.Count - 1;
                }
                else if (cf.Signs[i] == ConstraintSign.GE)
                {
                    headerList.Add($"c{++cIdx}");
                    basis[i] = headerList.Count - 1;
                }
                else
                {
                    basis[i] = n;
                }
            }
            headerList.Add("RHS");
            headers = headerList.ToArray();

            int colIdx = n;
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++) T[i, j] = cf.A[i, j];

                if (cf.Signs[i] == ConstraintSign.LE)
                {
                    T[i, colIdx++] = 1;
                }
                else if (cf.Signs[i] == ConstraintSign.GE)
                {
                    T[i, colIdx++] = -1;
                }

                T[i, totalCols - 1] = cf.b[i];
            }

            objRow = m;
            rhsCol = totalCols - 1;
            for (int j = 0; j < n; j++)
                T[objRow, j] = (cf.Sense == ProblemSense.Max) ? -cf.c[j] : cf.c[j];
        }

        private int ChooseEntering(double[,] T, int objRow)
        {
            int cols = T.GetLength(1) - 1;
            int bestCol = -1;
            double mostNegative = 0;

            for (int j = 0; j < cols; j++)
            {
                if (T[objRow, j] < mostNegative)
                {
                    mostNegative = T[objRow, j];
                    bestCol = j;
                }
            }
            return bestCol;
        }

        private int ChooseLeaving(double[,] T, int enterCol, int rhsCol, out bool unbounded)
        {
            int rows = T.GetLength(0) - 1;
            int bestRow = -1;
            double minRatio = double.MaxValue;
            unbounded = true;

            for (int i = 0; i < rows; i++)
            {
                double aij = T[i, enterCol];
                if (aij > _eps)
                {
                    unbounded = false;
                    double ratio = T[i, rhsCol] / aij;
                    if (ratio < minRatio)
                    {
                        minRatio = ratio;
                        bestRow = i;
                    }
                }
            }
            return bestRow;
        }

        private void Pivot(double[,] T, int pivotRow, int pivotCol)
        {
            int rows = T.GetLength(0);
            int cols = T.GetLength(1);
            double pivot = T[pivotRow, pivotCol];

            for (int j = 0; j < cols; j++)
                T[pivotRow, j] /= pivot;

            for (int i = 0; i < rows; i++)
            {
                if (i == pivotRow) continue;
                double mult = T[i, pivotCol];
                for (int j = 0; j < cols; j++)
                    T[i, j] -= mult * T[pivotRow, j];
            }
        }

        private string GetBasisVariableName(int[] basis, int row, string[] headers)
        {
            if (row < basis.Length && basis[row] < headers.Length - 1)
                return headers[basis[row]];
            return $"Row{row}";
        }

        private void PrintTableau(double[,] T, int objRow, int rhsCol, string[] headers, int[] basis, string title)
        {
            _log.Log($"\n=== {title} ===");

            string headerStr = "Basis\t";
            for (int j = 0; j < headers.Length; j++)
                headerStr += headers[j] + "\t";
            _log.Log(headerStr);

            for (int i = 0; i < T.GetLength(0); i++)
            {
                string rowStr = (i < basis.Length) ? headers[basis[i]] + "\t" : "z\t";
                for (int j = 0; j < T.GetLength(1); j++)
                    rowStr += $"{T[i, j]:F3}\t";
                _log.Log(rowStr);
            }

            _log.Log($"Current z = {T[objRow, rhsCol]:F3}");
        }
    }
}