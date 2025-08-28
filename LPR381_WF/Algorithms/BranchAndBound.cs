using System;
using System.Collections.Generic;
using System.Linq;
using LPR381.Core;

namespace LPR381_Solver.Algorithms
{
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
            }
            return true;
        }

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
        }
    }
}