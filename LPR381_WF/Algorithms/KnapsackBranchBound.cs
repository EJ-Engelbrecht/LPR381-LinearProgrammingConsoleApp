using System;
using System.Collections.Generic;
using System.Linq;

namespace LPR381_Solver.Algorithms
{
    public interface IKnapsackLogger
    {
        void Log(string message);
        void LogHeader(string title);
    }
    
    public class KnapsackResult
    {
        public string Status { get; set; } = "Unknown";
        public double Objective { get; set; }
        public double[] X { get; set; } = new double[0];
        public int Iterations { get; set; } = 0;
    }

    public class KnapsackItem
    {
        public int Index { get; set; }
        public double Weight { get; set; }
        public double Value { get; set; }
        public double Ratio => Value / Weight;
    }

    public class KnapsackNode
    {
        public int Level { get; set; }
        public double CurrentWeight { get; set; }
        public double CurrentValue { get; set; }
        public double UpperBound { get; set; }
        public bool[] Solution { get; set; }
        public bool Include { get; set; }
    }

    public class KnapsackBranchBound
    {
        private readonly IKnapsackLogger _logger;
        private double _capacity;
        private List<KnapsackItem> _items;
        private double _bestValue;
        private bool[] _bestSolution;
        private int _nodeCount;

        public KnapsackBranchBound(IKnapsackLogger logger)
        {
            _logger = logger;
        }

        public KnapsackResult Solve(double capacity, double[] weights, double[] values)
        {
            _logger.LogHeader("Knapsack Branch and Bound");
            
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
            
            _items = _items.OrderByDescending(item => item.Ratio).ToList();
            _bestSolution = new bool[_items.Count];
            
            _logger.Log($"Knapsack capacity: {capacity}");
            _logger.Log("Items sorted by value/weight ratio:");
            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                _logger.Log($"  Item {item.Index}: weight={item.Weight}, value={item.Value}, ratio={item.Ratio:F3}");
            }
            
            var rootNode = new KnapsackNode
            {
                Level = -1,
                CurrentWeight = 0,
                CurrentValue = 0,
                Solution = new bool[_items.Count]
            };
            
            rootNode.UpperBound = CalculateUpperBound(rootNode);
            
            var queue = new List<KnapsackNode> { rootNode };
            
            _logger.Log($"Starting branch and bound with upper bound: {rootNode.UpperBound:F3}");
            
            while (queue.Count > 0)
            {
                queue = queue.OrderByDescending(n => n.UpperBound).ToList();
                var currentNode = queue[0];
                queue.RemoveAt(0);
                _nodeCount++;
                
                _logger.Log($"Node {_nodeCount}: Level {currentNode.Level}, Value {currentNode.CurrentValue:F3}, Weight {currentNode.CurrentWeight:F3}, UB {currentNode.UpperBound:F3}");
                
                if (currentNode.UpperBound <= _bestValue)
                {
                    _logger.Log("  Pruned: Upper bound <= best value");
                    continue;
                }
                
                if (currentNode.Level == _items.Count - 1)
                {
                    if (currentNode.CurrentValue > _bestValue)
                    {
                        _bestValue = currentNode.CurrentValue;
                        _bestSolution = (bool[])currentNode.Solution.Clone();
                        _logger.Log($"  New best solution found! Value: {_bestValue:F3}");
                    }
                    continue;
                }
                
                int nextLevel = currentNode.Level + 1;
                var nextItem = _items[nextLevel];
                
                if (currentNode.CurrentWeight + nextItem.Weight <= _capacity)
                {
                    var includeNode = new KnapsackNode
                    {
                        Level = nextLevel,
                        CurrentWeight = currentNode.CurrentWeight + nextItem.Weight,
                        CurrentValue = currentNode.CurrentValue + nextItem.Value,
                        Solution = (bool[])currentNode.Solution.Clone(),
                        Include = true
                    };
                    includeNode.Solution[nextLevel] = true;
                    includeNode.UpperBound = CalculateUpperBound(includeNode);
                    
                    if (includeNode.UpperBound > _bestValue)
                    {
                        queue.Add(includeNode);
                        _logger.Log($"  Added INCLUDE node: Value {includeNode.CurrentValue:F3}, UB {includeNode.UpperBound:F3}");
                    }
                    else
                    {
                        _logger.Log($"  INCLUDE node pruned: UB {includeNode.UpperBound:F3} <= best {_bestValue:F3}");
                    }
                }
                else
                {
                    _logger.Log($"  INCLUDE node infeasible: weight would be {currentNode.CurrentWeight + nextItem.Weight:F3} > {_capacity}");
                }
                
                var excludeNode = new KnapsackNode
                {
                    Level = nextLevel,
                    CurrentWeight = currentNode.CurrentWeight,
                    CurrentValue = currentNode.CurrentValue,
                    Solution = (bool[])currentNode.Solution.Clone(),
                    Include = false
                };
                excludeNode.Solution[nextLevel] = false;
                excludeNode.UpperBound = CalculateUpperBound(excludeNode);
                
                if (excludeNode.UpperBound > _bestValue)
                {
                    queue.Add(excludeNode);
                    _logger.Log($"  Added EXCLUDE node: Value {excludeNode.CurrentValue:F3}, UB {excludeNode.UpperBound:F3}");
                }
                else
                {
                    _logger.Log($"  EXCLUDE node pruned: UB {excludeNode.UpperBound:F3} <= best {_bestValue:F3}");
                }
            }
            
            var result = new KnapsackResult
            {
                Status = "Optimal",
                Objective = Math.Round(_bestValue, 3),
                X = new double[weights.Length],
                Iterations = _nodeCount
            };
            
            for (int i = 0; i < _bestSolution.Length; i++)
            {
                if (_bestSolution[i])
                {
                    result.X[_items[i].Index] = 1.0;
                }
            }
            
            _logger.Log("Final Solution:");
            _logger.Log($"Optimal value: {result.Objective}");
            _logger.Log($"Nodes explored: {_nodeCount}");
            _logger.Log("Items selected:");
            
            double totalWeight = 0;
            for (int i = 0; i < result.X.Length; i++)
            {
                if (result.X[i] > 0)
                {
                    _logger.Log($"  Item {i}: weight={weights[i]}, value={values[i]}");
                    totalWeight += weights[i];
                }
            }
            _logger.Log($"Total weight used: {totalWeight} / {_capacity}");
            
            return result;
        }
        
        private double CalculateUpperBound(KnapsackNode node)
        {
            double remainingCapacity = _capacity - node.CurrentWeight;
            double upperBound = node.CurrentValue;
            
            for (int i = node.Level + 1; i < _items.Count; i++)
            {
                var item = _items[i];
                
                if (item.Weight <= remainingCapacity)
                {
                    upperBound += item.Value;
                    remainingCapacity -= item.Weight;
                }
                else
                {
                    upperBound += (remainingCapacity / item.Weight) * item.Value;
                    break;
                }
            }
            
            return upperBound;
        }
    }
}