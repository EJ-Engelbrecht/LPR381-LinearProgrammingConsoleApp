using System;
using System.Collections.Generic;
using System.Linq;
using LPR381.Core;

namespace LPR381_Solver.Algorithms
{
    public class KnapsackItem
    {
        public int Index { get; set; }
        public double Weight { get; set; }
        public double Value { get; set; }
        public double Ratio => Weight > 0 ? Value / Weight : 0;
    }

    public class KnapsackNode
    {
        public int Level { get; set; }
        public double CurrentWeight { get; set; }
        public double CurrentValue { get; set; }
        public double UpperBound { get; set; }
        public bool[] Solution { get; set; }
        public string Path { get; set; }
    }

    public class KnapsackBranchBound
    {
        private readonly LPR381.Core.IIterationLogger _log;
        private double _capacity;
        private List<KnapsackItem> _items;
        private double _bestValue;
        private bool[] _bestSolution;
        private int _nodeCount;

        public KnapsackBranchBound(LPR381.Core.IIterationLogger logger)
        {
            _log = logger;
        }

        public SolveResult Solve(CanonicalForm cf)
        {
            _log.LogHeader("Knapsack Branch and Bound Algorithm");
            
            try
            {
                // Extract knapsack problem from canonical form
                if (!ExtractKnapsackProblem(cf, out double capacity, out double[] weights, out double[] values))
                {
                    _log.Log("Problem is not in knapsack form");
                    return new SolveResult { Status = "Error" };
                }

                return SolveKnapsack(capacity, weights, values);
            }
            catch (Exception ex)
            {
                _log.Log($"Error in Knapsack Branch and Bound: {ex.Message}");
                return new SolveResult { Status = "Error" };
            }
        }

        public SolveResult SolveKnapsack(double capacity, double[] weights, double[] values)
        {
            _capacity = capacity;
            _bestValue = 0;
            _nodeCount = 0;
            
            _items = new List<KnapsackItem>();
            for (int i = 0; i < weights.Length; i++)
            {
                _items.Add(new KnapsackItem 
                { 
                    Index = i, 
                    Weight = weights[i], 
                    Value = values[i] 
                });
            }
            
            // Sort by value/weight ratio (descending)
            _items = _items.OrderByDescending(item => item.Ratio).ToList();
            _bestSolution = new bool[_items.Count];
            
            _log.Log($"Knapsack capacity: {capacity}");
            _log.Log("Items sorted by value/weight ratio:");
            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                _log.Log($"  Item {item.Index}: weight={item.Weight:F1}, value={item.Value:F1}, ratio={item.Ratio:F3}");
            }
            
            var rootNode = new KnapsackNode
            {
                Level = -1,
                CurrentWeight = 0,
                CurrentValue = 0,
                Solution = new bool[_items.Count],
                Path = "Root"
            };
            
            rootNode.UpperBound = CalculateUpperBound(rootNode);
            
            var queue = new List<KnapsackNode> { rootNode };
            
            _log.Log($"\nStarting branch and bound with upper bound: {rootNode.UpperBound:F3}");
            _log.Log("\n=== BRANCH AND BOUND TREE ===");
            
            while (queue.Count > 0)
            {
                // Select node with best upper bound
                queue = queue.OrderByDescending(n => n.UpperBound).ToList();
                var currentNode = queue[0];
                queue.RemoveAt(0);
                _nodeCount++;
                
                _log.Log($"\nNode {_nodeCount} ({currentNode.Path}):");
                _log.Log($"  Level: {currentNode.Level}, Value: {currentNode.CurrentValue:F1}, Weight: {currentNode.CurrentWeight:F1}");
                _log.Log($"  Upper Bound: {currentNode.UpperBound:F3}");
                
                // Prune if upper bound <= best known value
                if (currentNode.UpperBound <= _bestValue + 1e-9)
                {
                    _log.Log("  PRUNED: Upper bound <= best value");
                    continue;
                }
                
                // Check if we've processed all items
                if (currentNode.Level == _items.Count - 1)
                {
                    if (currentNode.CurrentValue > _bestValue)
                    {
                        _bestValue = currentNode.CurrentValue;
                        _bestSolution = (bool[])currentNode.Solution.Clone();
                        _log.Log($"  NEW BEST SOLUTION: Value = {_bestValue:F1}");
                    }
                    continue;
                }
                
                int nextLevel = currentNode.Level + 1;
                var nextItem = _items[nextLevel];
                
                // Try including the next item
                if (currentNode.CurrentWeight + nextItem.Weight <= _capacity)
                {
                    var includeNode = new KnapsackNode
                    {
                        Level = nextLevel,
                        CurrentWeight = currentNode.CurrentWeight + nextItem.Weight,
                        CurrentValue = currentNode.CurrentValue + nextItem.Value,
                        Solution = (bool[])currentNode.Solution.Clone(),
                        Path = currentNode.Path + ".I"
                    };
                    includeNode.Solution[nextLevel] = true;
                    includeNode.UpperBound = CalculateUpperBound(includeNode);
                    
                    if (includeNode.UpperBound > _bestValue + 1e-9)
                    {
                        queue.Add(includeNode);
                        _log.Log($"    INCLUDE Item {nextItem.Index}: New value = {includeNode.CurrentValue:F1}, UB = {includeNode.UpperBound:F3}");
                    }
                    else
                    {
                        _log.Log($"    INCLUDE Item {nextItem.Index}: PRUNED (UB = {includeNode.UpperBound:F3})");
                    }
                }
                else
                {
                    _log.Log($"    INCLUDE Item {nextItem.Index}: INFEASIBLE (weight would exceed capacity)");
                }
                
                // Try excluding the next item
                var excludeNode = new KnapsackNode
                {
                    Level = nextLevel,
                    CurrentWeight = currentNode.CurrentWeight,
                    CurrentValue = currentNode.CurrentValue,
                    Solution = (bool[])currentNode.Solution.Clone(),
                    Path = currentNode.Path + ".E"
                };
                excludeNode.Solution[nextLevel] = false;
                excludeNode.UpperBound = CalculateUpperBound(excludeNode);
                
                if (excludeNode.UpperBound > _bestValue + 1e-9)
                {
                    queue.Add(excludeNode);
                    _log.Log($"    EXCLUDE Item {nextItem.Index}: UB = {excludeNode.UpperBound:F3}");
                }
                else
                {
                    _log.Log($"    EXCLUDE Item {nextItem.Index}: PRUNED (UB = {excludeNode.UpperBound:F3})");
                }
            }
            
            var result = new SolveResult
            {
                Status = "Optimal",
                Objective = Math.Round(_bestValue, 3),
                X = new double[weights.Length],
                Iterations = _nodeCount
            };
            
            // Map solution back to original item indices
            for (int i = 0; i < _bestSolution.Length; i++)
            {
                if (_bestSolution[i])
                {
                    result.X[_items[i].Index] = 1.0;
                }
            }
            
            _log.Log("\n=== OPTIMAL SOLUTION ===");
            _log.Log($"Optimal value: {result.Objective}");
            _log.Log($"Nodes explored: {_nodeCount}");
            _log.Log("Items selected:");
            
            double totalWeight = 0;
            for (int i = 0; i < result.X.Length; i++)
            {
                if (result.X[i] > 0)
                {
                    _log.Log($"  Item {i}: weight={weights[i]:F1}, value={values[i]:F1}");
                    totalWeight += weights[i];
                }
            }
            _log.Log($"Total weight used: {totalWeight:F1} / {_capacity:F1}");
            
            return result;
        }
        
        private double CalculateUpperBound(KnapsackNode node)
        {
            double remainingCapacity = _capacity - node.CurrentWeight;
            double upperBound = node.CurrentValue;
            
            // Use fractional knapsack for upper bound
            for (int i = node.Level + 1; i < _items.Count; i++)
            {
                var item = _items[i];
                
                if (item.Weight <= remainingCapacity)
                {
                    // Take the whole item
                    upperBound += item.Value;
                    remainingCapacity -= item.Weight;
                }
                else if (remainingCapacity > 0)
                {
                    // Take fractional part
                    upperBound += (remainingCapacity / item.Weight) * item.Value;
                    break;
                }
            }
            
            return upperBound;
        }

        private bool ExtractKnapsackProblem(CanonicalForm cf, out double capacity, out double[] weights, out double[] values)
        {
            capacity = 0;
            weights = null;
            values = null;
            
            // For demonstration, assume first constraint is knapsack constraint
            if (cf.M >= 1 && cf.Signs[0] == ConstraintSign.LE)
            {
                capacity = cf.b[0];
                weights = new double[cf.N];
                values = new double[cf.N];
                
                for (int j = 0; j < cf.N; j++)
                {
                    weights[j] = cf.A[0, j];
                    values[j] = cf.c[j];
                }
                
                return true;
            }
            
            return false;
        }
    }
}