using System;
using System.Collections.Generic;
using System.Linq;
using LPR381.Core;

namespace LPR381_Solver.Algorithms
{
<<<<<<< Updated upstream
    public class BranchAndBoundNode
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public double LowerBound { get; set; }
        public double[] Solution { get; set; }
        public bool IsInteger { get; set; }
        public List<(int var, double bound, bool isUpper)> Constraints { get; set; } = new List<(int, double, bool)>();
        public string Label { get; set; }
    }

    public class BranchAndBound
    {
        private readonly LPR381.Core.IIterationLogger _log;
        private readonly PrimalSimplex _solver;
        private double _bestObjective = double.NegativeInfinity;
        private double[] _bestSolution;
        private int _nodeCount = 0;

        public BranchAndBound(LPR381.Core.IIterationLogger logger)
        {
            _log = logger;
            _solver = new PrimalSimplex(logger);
        }

        public SolveResult Solve(CanonicalForm cf, double intTol = 1e-6)
        {
            _log.LogHeader("Branch and Bound Algorithm");
            
            try
            {
                // Solve root LP relaxation
                var rootResult = _solver.Solve(cf);
                
                if (rootResult.Status != "Optimal")
                {
                    _log.Log($"Root LP is {rootResult.Status}");
                    return rootResult;
                }

                _log.Log($"\nRoot LP Solution: Z = {rootResult.Objective:F3}");
                for (int i = 0; i < rootResult.X.Length; i++)
                    _log.Log($"x{i+1} = {rootResult.X[i]:F3}");

                // Check if already integer
                if (IsIntegerSolution(rootResult.X, intTol))
                {
                    _log.Log("Root solution is already integer optimal!");
                    return rootResult;
                }

                // Initialize branch and bound
                var queue = new List<BranchAndBoundNode>();
                var rootNode = new BranchAndBoundNode
                {
                    Id = ++_nodeCount,
                    Level = 0,
                    LowerBound = rootResult.Objective,
                    Solution = rootResult.X,
                    IsInteger = false,
                    Label = "Root"
                };
                
                queue.Add(rootNode);
                _bestObjective = double.NegativeInfinity;

                _log.Log("\n=== BRANCH AND BOUND TREE ===");

                while (queue.Count > 0 && _nodeCount < 20)
                {
                    // Select node with best bound (best-first search)
                    queue = queue.OrderByDescending(n => n.LowerBound).ToList();
                    var currentNode = queue[0];
                    queue.RemoveAt(0);

                    _log.Log($"\nProcessing Node {currentNode.Id} ({currentNode.Label}):");
                    _log.Log($"  Level: {currentNode.Level}, Bound: {currentNode.LowerBound:F3}");

                    // Prune if bound is worse than best known integer solution
                    if (currentNode.LowerBound <= _bestObjective + 1e-9)
                    {
                        _log.Log("  PRUNED: Bound <= best integer solution");
                        continue;
                    }

                    // Check if integer feasible
                    if (IsIntegerSolution(currentNode.Solution, intTol))
                    {
                        if (currentNode.LowerBound > _bestObjective)
                        {
                            _bestObjective = currentNode.LowerBound;
                            _bestSolution = (double[])currentNode.Solution.Clone();
                            _log.Log($"  NEW BEST INTEGER SOLUTION: Z = {_bestObjective:F3}");
                        }
                        continue;
                    }

                    // Branch on most fractional variable
                    int branchVar = FindMostFractionalVariable(currentNode.Solution);
                    double fracValue = currentNode.Solution[branchVar];
                    
                    _log.Log($"  Branching on x{branchVar+1} = {fracValue:F3}");

                    // Create left child: x_j <= floor(fracValue)
                    var leftBound = Math.Floor(fracValue);
                    var leftChild = CreateChildNode(cf, currentNode, branchVar, leftBound, true, "L");
                    if (leftChild != null) queue.Add(leftChild);

                    // Create right child: x_j >= ceil(fracValue)
                    var rightBound = Math.Ceiling(fracValue);
                    var rightChild = CreateChildNode(cf, currentNode, branchVar, rightBound, false, "R");
                    if (rightChild != null) queue.Add(rightChild);
                }

                var result = new SolveResult();
                if (_bestSolution != null)
                {
                    result.Status = "Optimal";
                    result.Objective = Math.Round(_bestObjective, 3);
                    result.X = _bestSolution.Select(v => Math.Round(v, 3)).ToArray();
                    result.Iterations = _nodeCount;
                    
                    _log.Log($"\n=== OPTIMAL INTEGER SOLUTION ===");
                    _log.Log($"Objective: {result.Objective:F3}");
                    _log.Log($"Nodes explored: {_nodeCount}");
                    for (int i = 0; i < result.X.Length; i++)
                        _log.Log($"x{i+1} = {result.X[i]:F3}");
                }
                else
                {
                    result.Status = "No integer solution found";
                    _log.Log("No integer solution found");
                }

                return result;
            }
            catch (Exception ex)
            {
                _log.Log($"Error in Branch and Bound: {ex.Message}");
                return new SolveResult { Status = "Error" };
            }
        }

        private BranchAndBoundNode CreateChildNode(CanonicalForm cf, BranchAndBoundNode parent, int branchVar, double bound, bool isUpperBound, string side)
        {
            var childCf = cf.Clone();
            
            // Add branching constraint
            int m = childCf.M;
            int n = childCf.N;
            
            // Expand constraint matrix
            var newA = new double[m + 1, n];
            var newb = new double[m + 1];
            var newSigns = new ConstraintSign[m + 1];
            
            // Copy existing constraints
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                    newA[i, j] = childCf.A[i, j];
                newb[i] = childCf.b[i];
                newSigns[i] = childCf.Signs[i];
            }
            
            // Add branching constraint
            newA[m, branchVar] = 1;
            newb[m] = bound;
            newSigns[m] = isUpperBound ? ConstraintSign.LE : ConstraintSign.GE;
            
            childCf.A = newA;
            childCf.b = newb;
            childCf.Signs = newSigns;

            // Solve child subproblem
            var childResult = _solver.Solve(childCf);
            
            string constraintStr = isUpperBound ? $"x{branchVar+1} <= {bound}" : $"x{branchVar+1} >= {bound}";
            
            if (childResult.Status == "Optimal")
            {
                var child = new BranchAndBoundNode
                {
                    Id = ++_nodeCount,
                    Level = parent.Level + 1,
                    LowerBound = childResult.Objective,
                    Solution = childResult.X,
                    IsInteger = IsIntegerSolution(childResult.X, 1e-6),
                    Label = $"{parent.Label}.{side}",
                    Constraints = new List<(int, double, bool)>(parent.Constraints)
                };
                child.Constraints.Add((branchVar, bound, isUpperBound));
                
                _log.Log($"    Child {child.Id} ({constraintStr}): Z = {child.LowerBound:F3}");
                return child;
            }
            else
            {
                _log.Log($"    Child ({constraintStr}): {childResult.Status}");
                return null;
            }
        }

        private bool IsIntegerSolution(double[] solution, double tolerance)
        {
            foreach (double val in solution)
            {
                if (Math.Abs(val - Math.Round(val)) > tolerance)
                    return false;
=======
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

        // ---------- Sub Problem + Bound structures ----------
        private sealed class SubProblem
        {
            public List<Bnd> Cons = new List<Bnd>();  // per-variable L/U bounds from branching
            public int Depth;                          // tree depth (root=0)
            public string Label = "0";                 // "0", "1.1", "2.1", ...
            public string BranchNote = "";             // e.g., "x3 ≤ 0 (LEFT)"
            public double Obj;
            public double[] X = Array.Empty<double>();
            public string Status = "";
        }

        private sealed class Bnd
        {
            public int j;        // variable index (0-based)
            public double? L;    // lower bound (xj >= L)
            public double? U;    // upper bound (xj <= U)
            public override string ToString() => $"x{j + 1}:{L?.ToString() ?? ""}:{U?.ToString() ?? ""}";
        }

        // ---------- Public solve ----------
        public SolveResult Solve(CanonicalForm baseCf, double intTol = 1e-6)
        {
            // Treat ALL first N variables as binary decision vars: 0/1
            bool isMax = baseCf.Sense == ProblemSense.Max;
            int decisionCount = baseCf.N;

            double incumbent = isMax ? double.NegativeInfinity : double.PositiveInfinity;
            double[] bestX = null;

            var start = DateTime.UtcNow;
            var timeLimit = TimeSpan.FromSeconds(DEFAULT_TIME_LIMIT_SEC);
            int maxNodes = DEFAULT_MAX_NODES;

            var stack = new Stack<SubProblem>();
            // Root sub problem
            stack.Push(new SubProblem { Depth = 0, Label = "0", BranchNote = "" });

            int createdCounter = 0; // for “X.Y” labels (X = creation order, Y = depth)
            int explored = 0;
            var seen = new HashSet<string>();

            _log?.LogHeader("=== Branch and Bound (binary) ===");

            while (stack.Count > 0)
            {
                if ((DateTime.UtcNow - start) > timeLimit) { _log?.Log("Stopped: time limit"); break; }
                if (explored >= maxNodes) { _log?.Log($"Stopped: node limit {maxNodes}"); break; }

                var sp = stack.Pop();
                explored++;

                // make a signature from bounds to avoid revisiting equivalent subproblems
                var sig = string.Join("|", sp.Cons.Select(c => c.ToString()));
                if (!seen.Add(sig)) continue;

                // ----- announce sub problem + branch at TOP of the simplex output -----
                _log?.LogHeader($"Sub Problem {sp.Label}");
                if (!string.IsNullOrWhiteSpace(sp.BranchNote))
                    _log?.Log(sp.BranchNote);

                // Solve LP relaxation for this sub problem (add 0/1 box + branch bounds)
                var cfChild = BuildChildModel(baseCf, sp.Cons, decisionCount);
                var res = _simplex.Solve(cfChild);

                sp.Status = res.Status;
                sp.Obj = res.Objective;
                sp.X = res.X ?? Array.Empty<double>();

                _log?.Log($"Status: {sp.Status}, Bound z={sp.Obj:F6}");

                // Infeasible/unbounded → prune
                if (!string.Equals(res.Status, "Optimal", StringComparison.OrdinalIgnoreCase))
                {
                    _log?.Log("Pruned: infeasible/unbounded LP");
                    continue;
                }

                // Bound pruning
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

                // 0/1 integrality on decision vars
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

                // pick a fractional decision var to branch on
                int j = MostFractionalIndex(sp.X, intTol, decisionCount);
                if (j == -1)
                {
                    _log?.Log("No fractional variables found; pruning");
                    continue;
                }

                double v = sp.X[j];
                // Create RIGHT then LEFT so LEFT is processed next (depth-first feel)
                // RIGHT: x_j ≥ 1
                var right = new SubProblem
                {
                    Depth = sp.Depth + 1,
                    Label = $"{++createdCounter}.{sp.Depth + 1}",
                    BranchNote = $"x{j + 1} ≥ 1 (RIGHT)",
                    Cons = new List<Bnd>(sp.Cons) { new Bnd { j = j, L = 1 } }
                };

                // LEFT: x_j ≤ 0
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

        // ---------- helpers ----------
        private static bool IsIntegral01(double[] x, int decisionCount, double tol = 1e-6)
        {
            if (x == null) return false;
            int n = Math.Min(decisionCount, x.Length);
            for (int j = 0; j < n; j++)
            {
                double r = Math.Round(x[j]);
                if (Math.Abs(x[j] - r) > tol) return false;
                if (r != 0.0 && r != 1.0) return false;
>>>>>>> Stashed changes
            }
            return true;
        }

<<<<<<< Updated upstream
        private int FindMostFractionalVariable(double[] solution)
        {
            int mostFractional = 0;
            double maxFractionalPart = 0;
            
            for (int i = 0; i < solution.Length; i++)
            {
                double fractionalPart = Math.Abs(solution[i] - Math.Round(solution[i]));
                if (fractionalPart > maxFractionalPart)
                {
                    maxFractionalPart = fractionalPart;
                    mostFractional = i;
                }
            }
            
            return mostFractional;
=======
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

        // Add 0≤x≤1 box + branch bounds as <= rows
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

            int addLower = decisionCount; // -x <= 0
            int addUpper = decisionCount; //  x <= 1
            int addBranch = (cons?.Count(c => c.U.HasValue) ?? 0) +
                            (cons?.Count(c => c.L.HasValue) ?? 0);

            int add = addLower + addUpper + addBranch;

            var A2 = new double[m0 + add, n];
            var b2 = new double[m0 + add];
            var s2 = new ConstraintSign[m0 + add];

            // copy base
            for (int i = 0; i < m0; i++)
            {
                for (int j = 0; j < n; j++) A2[i, j] = cf.A[i, j];
                b2[i] = cf.b[i];
                s2[i] = cf.Signs[i];
            }

            int r = m0;

            // -x <= 0  (x >= 0)
            for (int j = 0; j < decisionCount; j++)
            {
                A2[r, j] = -1.0;
                b2[r] = 0.0;
                s2[r] = ConstraintSign.LE;
                r++;
            }

            //  x <= 1
            for (int j = 0; j < decisionCount; j++)
            {
                A2[r, j] = 1.0;
                b2[r] = 1.0;
                s2[r] = ConstraintSign.LE;
                r++;
            }

            // branch bounds
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
>>>>>>> Stashed changes
        }
    }
}
