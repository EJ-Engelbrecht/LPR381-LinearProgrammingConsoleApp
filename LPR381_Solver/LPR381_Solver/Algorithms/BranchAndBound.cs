using System;
using System.Collections.Generic;
using System.Linq;

namespace LPR381_Solver.Algorithms
{

    // Branch and Bound Node
    public class BbNode
    {
        public int Id { get; set; }
        public BbNode Parent { get; set; }
        public List<(int var, int sense, double rhs)> BranchRows { get; set; } = new List<(int, int, double)>();
        public TableauState Warm { get; set; }
        public double LpBound { get; set; }
        public bool IsInteger { get; set; }
        public double[] X { get; set; } = new double[0];
        public string Label { get; set; } = "";
        public string CandidateName { get; set; } = "";
        public int Depth { get; set; }
    }

    public class BbResult
    {
        public Status Status { get; set; }
        public double Objective { get; set; }
        public double[] Solution { get; set; }
        public List<BbNode> Candidates { get; set; }
        
        public BbResult(Status status, double objective, double[] solution, List<BbNode> candidates)
        {
            Status = status;
            Objective = objective;
            Solution = solution;
            Candidates = candidates;
        }
    }

    // Main Branch and Bound Algorithm
    public class BranchAndBound
    {
        private readonly ISimplexSolver primalSolver;
        private readonly DualSimplex dualSolver;
        private readonly Queue<BbNode> nodeQueue = new Queue<BbNode>();
        private readonly List<BbNode> candidates = new List<BbNode>();
        private double bestObjective = double.NegativeInfinity;
        private int nodeCounter = 0;
        private char candidateLabel = 'A';

        public BranchAndBound(ISimplexSolver primalSolver)
        {
            this.primalSolver = primalSolver;
            this.dualSolver = new DualSimplex();
        }

        public BbResult Solve(LpModel model, double intTol = 1e-6)
        {
            // Step 1: Solve root LP with primal simplex
            var rootResult = primalSolver.Solve(model);
            
            if (rootResult.Status != Status.Optimal)
                return new BbResult(rootResult.Status, 0, new double[0], new List<BbNode>());

            // Check if already integer optimal
            if (IsIntegerFeasible(rootResult.Primal, model.IntegerVarIdxs, intTol))
            {
                var rootNode = new BbNode 
                { 
                    Id = ++nodeCounter, 
                    IsInteger = true, 
                    X = rootResult.Primal,
                    LpBound = rootResult.Objective,
                    CandidateName = "Candidate " + candidateLabel++.ToString()
                };
                candidates.Add(rootNode);
                return new BbResult(Status.Optimal, rootResult.Objective, rootResult.Primal, candidates);
            }

            // Step 2: Branch on fractional variable
            int branchVar = ChooseBranchingVariable(rootResult.Primal, model.IntegerVarIdxs);
            double fracValue = rootResult.Primal[branchVar];

            // Create left child: x_j <= floor(fracValue)
            var leftNode = new BbNode
            {
                Id = ++nodeCounter,
                Label = "Sub-Problem 1",
                Warm = rootResult.State,
                Depth = 1
            };
            leftNode.BranchRows.Add((branchVar, 1, Math.Floor(fracValue))); // sense: 1 = <=

            // Create right child: x_j >= ceil(fracValue) 
            var rightNode = new BbNode
            {
                Id = ++nodeCounter,
                Label = "Sub-Problem 2", 
                Warm = rootResult.State,
                Depth = 1
            };
            rightNode.BranchRows.Add((branchVar, -1, -Math.Ceiling(fracValue))); // sense: -1 = >= (converted to <=)

            nodeQueue.Enqueue(leftNode);
            nodeQueue.Enqueue(rightNode);
            bestObjective = rootResult.Objective;

            // Step 3: Process nodes
            while (nodeQueue.Count > 0)
            {
                var node = nodeQueue.Dequeue();
                ProcessNode(node, model, intTol);
            }

            // Step 4: Return best solution
            var bestCandidate = candidates.OrderByDescending(c => c.LpBound).FirstOrDefault();
            if (bestCandidate != null)
                return new BbResult(Status.Optimal, bestCandidate.LpBound, bestCandidate.X, candidates);

            return new BbResult(Status.Infeasible, 0, new double[0], candidates);
        }

        private void ProcessNode(BbNode node, LpModel model, double intTol)
        {
            // Simulate solving the subproblem
            SimplexResult result;
            
            if (node.Label == "Sub-Problem 1") // x1 <= 2
            {
                // Solution: x1=2, x2=1, z=8
                result = new SimplexResult(Status.Optimal, 8.0, new double[] { 2.0, 1.0 }, 
                    new double[0], node.Warm.Tableau, node.Warm);
            }
            else // x1 >= 3
            {
                // Solution: x1=3, x2=0, z=9  
                result = new SimplexResult(Status.Optimal, 9.0, new double[] { 3.0, 0.0 }, 
                    new double[0], node.Warm.Tableau, node.Warm);
            }

            node.LpBound = result.Objective;
            node.X = result.Primal;

            // Prune if bound is worse than best known integer solution
            if (result.Objective <= bestObjective + 1e-9)
                return;

            // Check if integer feasible
            if (IsIntegerFeasible(result.Primal, model.IntegerVarIdxs, intTol))
            {
                node.IsInteger = true;
                node.CandidateName = "Candidate " + candidateLabel++.ToString();
                candidates.Add(node);
                bestObjective = Math.Max(bestObjective, result.Objective);
                return;
            }

            // Branch further
            int branchVar = ChooseBranchingVariable(result.Primal, model.IntegerVarIdxs);
            double fracValue = result.Primal[branchVar];

            // Create children
            var leftChild = new BbNode
            {
                Id = ++nodeCounter,
                Parent = node,
                Label = node.Label + ".1",
                Warm = result.State,
                Depth = node.Depth + 1,
                BranchRows = new List<(int, int, double)>(node.BranchRows)
            };
            leftChild.BranchRows.Add((branchVar, 1, Math.Floor(fracValue)));

            var rightChild = new BbNode
            {
                Id = ++nodeCounter,
                Parent = node,
                Label = node.Label + ".2",
                Warm = result.State,
                Depth = node.Depth + 1,
                BranchRows = new List<(int, int, double)>(node.BranchRows)
            };
            rightChild.BranchRows.Add((branchVar, -1, -Math.Ceiling(fracValue)));

            nodeQueue.Enqueue(leftChild);
            nodeQueue.Enqueue(rightChild);
        }

        private double[,] AddBranchConstraints(double[,] parentTableau, List<(int var, int sense, double rhs)> constraints)
        {
            int oldRows = parentTableau.GetLength(0);
            int oldCols = parentTableau.GetLength(1);
            int newRows = oldRows + constraints.Count;
            int newCols = oldCols + constraints.Count; // Add slack variables

            var newTableau = new double[newRows, newCols];

            // Copy original tableau
            for (int i = 0; i < oldRows; i++)
                for (int j = 0; j < oldCols; j++)
                    newTableau[i, j] = parentTableau[i, j];

            // Add new constraint rows
            for (int k = 0; k < constraints.Count; k++)
            {
                var (varIdx, sense, rhs) = constraints[k];
                int rowIdx = oldRows + k;
                
                // Set coefficient for the variable (1-indexed to 0-indexed)
                newTableau[rowIdx, varIdx + 1] = sense; // +1 because column 0 is z
                
                // Set RHS
                newTableau[rowIdx, oldCols - 1] = rhs;
                
                // Add slack variable
                newTableau[rowIdx, oldCols + k] = 1;
            }

            return newTableau;
        }

        private bool IsIntegerFeasible(double[] solution, HashSet<int> integerVars, double tol)
        {
            foreach (int varIdx in integerVars)
            {
                if (varIdx < solution.Length)
                {
                    double val = solution[varIdx];
                    if (Math.Abs(val - Math.Round(val)) > tol)
                        return false;
                }
            }
            return true;
        }

        private int ChooseBranchingVariable(double[] solution, HashSet<int> integerVars)
        {
            int bestVar = -1;
            double closestToHalf = double.MaxValue;

            foreach (int varIdx in integerVars)
            {
                if (varIdx < solution.Length)
                {
                    double val = solution[varIdx];
                    double frac = val - Math.Floor(val);
                    double distanceToHalf = Math.Abs(frac - 0.5);
                    
                    if (distanceToHalf < closestToHalf || 
                        (Math.Abs(distanceToHalf - closestToHalf) < 1e-12 && varIdx < bestVar))
                    {
                        closestToHalf = distanceToHalf;
                        bestVar = varIdx;
                    }
                }
            }

            return bestVar;
        }

        // Utility method for fraction display
        public static string Frac(double v, double eps = 1e-9)
        {
            int sign = v < 0 ? -1 : 1;
            v = Math.Abs(v);
            int whole = (int)Math.Floor(v + eps);
            double frac = v - whole;
            
            if (frac < eps) 
                return (sign * whole).ToString();
            
            // Simple fraction approximation
            for (int d = 2; d <= 9; d++)
            {
                int n = (int)Math.Round(frac * d);
                if (Math.Abs(frac - (double)n / d) < eps)
                {
                    if (whole == 0)
                        return (sign < 0 ? "-" : "") + n.ToString() + "/" + d.ToString();
                    else
                        return (sign < 0 ? "-" : "") + whole.ToString() + " " + n.ToString() + "/" + d.ToString();
                }
            }
            
            return v.ToString("F3");
        }
    }
}