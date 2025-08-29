using System;
using System.Collections.Generic;
using System.Linq;
using LPR381.Core;

namespace LPR381_Solver.Algorithms
{
<<<<<<< Updated upstream
    public class PrimalSimplex 
    {
        private readonly LPR381.Core.IIterationLogger _log;
        private readonly double _M;
        private readonly double _eps;
        
        public PrimalSimplex(LPR381.Core.IIterationLogger logger, double bigM = 1e6, double eps = 1e-9)
=======
    public class PrimalSimplex
    {
        private readonly IIterationLogger _log;
        private readonly double _M;
        private readonly double _eps;

        public PrimalSimplex(IIterationLogger logger, double bigM = 1e6, double eps = 1e-9)
>>>>>>> Stashed changes
        {
            _log = logger;
            _M = bigM;
            _eps = eps;
        }

<<<<<<< Updated upstream
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
=======
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
>>>>>>> Stashed changes
            }
            catch (Exception ex)
            {
                _log.Log($"Error in PrimalSimplex: {ex.Message}");
                res.Status = "Error";
                return res;
            }
        }

<<<<<<< Updated upstream
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
=======
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
                if (enterCol < 0) break; // optimal

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

            // Extract solution
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
                    headerList.Add($"c{++slackIdx}");    // **was s -> now c**
                    basis[i] = headerList.Count - 1;
                }
                else if (cf.Signs[i] == ConstraintSign.GE)
                {
                    headerList.Add($"c{++surplusIdx}");  // surplus shown as c#
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
            int slacks = cf.Signs.Count(s => s == ConstraintSign.LE);
            int surplus = cf.Signs.Count(s => s == ConstraintSign.GE);

            int totalCols = n + slacks + surplus + 1;
            int totalRows = m + 1;

            T = new double[totalRows, totalCols];
            basis = new int[m];

            var headerList = new List<string>();
            for (int j = 0; j < n; j++) headerList.Add($"x{j + 1}");

            int slackIdx = 0, surplusIdx = 0;
            for (int i = 0; i < m; i++)
            {
                if (cf.Signs[i] == ConstraintSign.LE)
                {
                    headerList.Add($"c{++slackIdx}");   // **was s -> now c**
                    basis[i] = headerList.Count - 1;
                }
                else if (cf.Signs[i] == ConstraintSign.GE)
                {
                    headerList.Add($"c{++surplusIdx}"); // **was s -> now c**
                    basis[i] = n; // placeholder
                }
                else
                {
                    basis[i] = n; // placeholder
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
>>>>>>> Stashed changes
        }

        private int ChooseEntering(double[,] T, int objRow)
        {
            int cols = T.GetLength(1) - 1;
<<<<<<< Updated upstream
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
=======
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
>>>>>>> Stashed changes
        }

        private int ChooseLeaving(double[,] T, int enterCol, int rhsCol, out bool unbounded)
        {
<<<<<<< Updated upstream
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
=======
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
>>>>>>> Stashed changes
        }

        private void Pivot(double[,] T, int pivotRow, int pivotCol)
        {
            int rows = T.GetLength(0);
            int cols = T.GetLength(1);
<<<<<<< Updated upstream
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
=======
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
>>>>>>> Stashed changes
        }
    }
}
