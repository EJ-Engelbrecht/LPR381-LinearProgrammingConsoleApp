using System;
using System.Collections.Generic;
using System.Linq;
using LPR381.Core;

namespace LPR381_Solver.Algorithms
{
    internal sealed class BranchAndBound
    {
        private readonly PrimalSimplex _simplex;
        private readonly IIterationLogger _log;

        public BranchAndBound(PrimalSimplex simplex, IIterationLogger logger)
        {
            _simplex = simplex;
            _log = logger;
        }

        private const double TOL = 1e-9;
        private const int DEFAULT_MAX_NODES = 5000;
        private const double DEFAULT_TIME_LIMIT_SEC = 5.0;

        private sealed class SubProblem
        {
            public List<Bnd> Cons = new List<Bnd>();
            public int Depth;
            public string Label = "0";
            public string BranchNote = "";
            public double Obj;
            public double[] X = Array.Empty<double>();
            public string Status = "";
        }

        private sealed class Bnd
        {
            public int j;
            public double? L;
            public double? U;
            public override string ToString() => $"x{j + 1}:{L?.ToString() ?? ""}:{U?.ToString() ?? ""}";
        }

        public SolveResult Solve(CanonicalForm baseCf, double intTol = 1e-6)
        {
            bool isMax = baseCf.Sense == ProblemSense.Max;
            int decisionCount = baseCf.N;

            double incumbent = isMax ? double.NegativeInfinity : double.PositiveInfinity;
            double[] bestX = null;

            var start = DateTime.UtcNow;
            var timeLimit = TimeSpan.FromSeconds(DEFAULT_TIME_LIMIT_SEC);
            int maxNodes = DEFAULT_MAX_NODES;

            var stack = new Stack<SubProblem>();
            stack.Push(new SubProblem { Depth = 0, Label = "0", BranchNote = "" });

            int createdCounter = 0;
            int explored = 0;
            var seen = new HashSet<string>();

            _log?.LogHeader("=== Branch and Bound (binary) ===");

            while (stack.Count > 0)
            {
                if ((DateTime.UtcNow - start) > timeLimit) { _log?.Log("Stopped: time limit"); break; }
                if (explored >= maxNodes) { _log?.Log($"Stopped: node limit {maxNodes}"); break; }

                var sp = stack.Pop();
                explored++;

                var sig = string.Join("|", sp.Cons.Select(c => c.ToString()));
                if (!seen.Add(sig)) continue;

                _log?.LogHeader($"Sub Problem {sp.Label}");
                if (!string.IsNullOrWhiteSpace(sp.BranchNote))
                    _log?.Log(sp.BranchNote);

                var cfChild = BuildChildModel(baseCf, sp.Cons, decisionCount);
                var res = _simplex.Solve(cfChild);

                sp.Status = res.Status;
                sp.Obj = res.Objective;
                sp.X = res.X ?? Array.Empty<double>();

                _log?.Log($"Status: {sp.Status}, Bound z={sp.Obj:F6}");

                if (!string.Equals(res.Status, "Optimal", StringComparison.OrdinalIgnoreCase))
                {
                    _log?.Log("Pruned: infeasible/unbounded LP");
                    continue;
                }

                if (isMax)
                {
                    if (sp.Obj <= incumbent + TOL)
                    {
                        _log?.Log($"Pruned by bound: {sp.Obj:F6} <= incumbent {incumbent:F6}");
                        continue;
                    }
                }
                else
                {
                    if (sp.Obj >= incumbent - TOL)
                    {
                        _log?.Log($"Pruned by bound: {sp.Obj:F6} >= incumbent {incumbent:F6}");
                        continue;
                    }
                }

                if (IsIntegral01(sp.X, decisionCount, intTol))
                {
                    if ((isMax && sp.Obj > incumbent + TOL) ||
                        (!isMax && sp.Obj < incumbent - TOL))
                    {
                        incumbent = sp.Obj;
                        bestX = sp.X.ToArray();
                        _log?.Log($"New incumbent z* = {incumbent:F6}");
                    }
                    _log?.Log("Pruned (integer leaf)");
                    continue;
                }

                int j = MostFractionalIndex(sp.X, intTol, decisionCount);
                if (j == -1)
                {
                    _log?.Log("No fractional variables found; pruning");
                    continue;
                }

                double v = sp.X[j];
                var right = new SubProblem
                {
                    Depth = sp.Depth + 1,
                    Label = $"{++createdCounter}.{sp.Depth + 1}",
                    BranchNote = $"x{j + 1} ≥ 1 (RIGHT)",
                    Cons = new List<Bnd>(sp.Cons) { new Bnd { j = j, L = 1 } }
                };

                var left = new SubProblem
                {
                    Depth = sp.Depth + 1,
                    Label = $"{++createdCounter}.{sp.Depth + 1}",
                    BranchNote = $"x{j + 1} ≤ 0 (LEFT)",
                    Cons = new List<Bnd>(sp.Cons) { new Bnd { j = j, U = 0 } }
                };

                _log?.Log($"Branch on x{j + 1} = {v:F6} → LEFT: x{j + 1} ≤ 0  |  RIGHT: x{j + 1} ≥ 1");

                stack.Push(right);
                stack.Push(left);
            }

            _log?.Log($"\nExplored {explored} sub problems in {(DateTime.UtcNow - start).TotalSeconds:F2}s.");

            if (bestX != null)
                return new SolveResult
                {
                    Status = "Optimal",
                    Objective = incumbent,
                    X = bestX
                };

            return new SolveResult
            {
                Status = "No integer solution",
                Objective = 0.0,
                X = Array.Empty<double>()
            };
        }

        private static bool IsIntegral01(double[] x, int decisionCount, double tol = 1e-6)
        {
            if (x == null) return false;
            int n = Math.Min(decisionCount, x.Length);
            for (int j = 0; j < n; j++)
            {
                double r = Math.Round(x[j]);
                if (Math.Abs(x[j] - r) > tol) return false;
                if (r != 0.0 && r != 1.0) return false;
            }
            return true;
        }

        private static int MostFractionalIndex(double[] x, double tol, int decisionCount)
        {
            if (x == null) return -1;
            int n = Math.Min(decisionCount, x.Length);

            int idx = -1;
            double maxFrac = 0.0;

            for (int j = 0; j < n; j++)
            {
                double val = x[j];
                if (Math.Abs(val - 0.0) <= tol || Math.Abs(val - 1.0) <= tol) continue;
                double frac = Math.Abs(val - Math.Round(val));
                if (frac > tol && frac > maxFrac)
                {
                    maxFrac = frac;
                    idx = j;
                }
            }
            return idx;
        }

        private static CanonicalForm BuildChildModel(
            CanonicalForm baseCf,
            List<Bnd> cons,
            int decisionCount)
        {
            var cf = baseCf.Clone();
            int m0 = cf.M;
            int n = cf.N;

            if (decisionCount <= 0 || decisionCount > n)
                decisionCount = n;

            int addLower = decisionCount;
            int addUpper = decisionCount;
            int addBranch = (cons?.Count(c => c.U.HasValue) ?? 0) +
                            (cons?.Count(c => c.L.HasValue) ?? 0);

            int add = addLower + addUpper + addBranch;

            var A2 = new double[m0 + add, n];
            var b2 = new double[m0 + add];
            var s2 = new ConstraintSign[m0 + add];

            for (int i = 0; i < m0; i++)
            {
                for (int j = 0; j < n; j++) A2[i, j] = cf.A[i, j];
                b2[i] = cf.b[i];
                s2[i] = cf.Signs[i];
            }

            int r = m0;

            for (int j = 0; j < decisionCount; j++)
            {
                A2[r, j] = -1.0;
                b2[r] = 0.0;
                s2[r] = ConstraintSign.LE;
                r++;
            }

            for (int j = 0; j < decisionCount; j++)
            {
                A2[r, j] = 1.0;
                b2[r] = 1.0;
                s2[r] = ConstraintSign.LE;
                r++;
            }

            if (cons != null)
            {
                foreach (var c in cons)
                {
                    if (c.j < 0 || c.j >= decisionCount) continue;

                    if (c.U.HasValue)
                    {
                        A2[r, c.j] = 1.0;
                        b2[r] = c.U.Value;
                        s2[r] = ConstraintSign.LE;
                        r++;
                    }
                    if (c.L.HasValue)
                    {
                        A2[r, c.j] = -1.0;
                        b2[r] = -c.L.Value;
                        s2[r] = ConstraintSign.LE;
                        r++;
                    }
                }
            }

            cf.A = A2;
            cf.b = b2;
            cf.Signs = s2;
            return cf;
        }
    }
}