<<<<<<< Updated upstream
using LPR381_Solver.Models;
using LPR381_Solver.Utils;
=======
>>>>>>> Stashed changes
using System;
using System.Collections.Generic;
using System.Linq;

namespace LPR381_Solver.Algorithms
{
<<<<<<< Updated upstream
    public class KnapsackBranchBound
    {
        private static LPModel GetKnapsackTestCase(int testNumber)
        {
            string testId = $"K{testNumber}";
            string testCaseText = CanonicalFormConverter.GetTestCaseById(testId);
            
            if (testCaseText == null)
            {
                Console.WriteLine($"Knapsack test case K{testNumber} not found.");
                return null;
            }

            return CanonicalFormConverter.ParseFromText(testCaseText);
        }

        public static void Solve(int testNumber)
        {
            var model = GetKnapsackTestCase(testNumber);
            if (model == null) return;

            Console.WriteLine($"Solving Knapsack Test Case K{testNumber}");
            
            double[] values = new double[model.Variables.Count];
            double[] weights = new double[model.Variables.Count];
            
            for (int i = 0; i < model.Variables.Count; i++)
            {
                string varName = model.Variables[i].Name;
                values[i] = model.ObjectiveFunction[varName];
                weights[i] = model.Constraints[0].Coefficients[varName];
            }
            
            double capacity = model.Constraints[0].RightHandSide;
            
            var result = SolveKnapsack(values, weights, capacity);
            
            Console.WriteLine($"Optimal Value: {result.Item1}");
            Console.WriteLine($"Selected Items: {string.Join(", ", result.Item2)}");
        }

        private static Tuple<double, List<int>> SolveKnapsack(double[] values, double[] weights, double capacity)
        {
            int n = values.Length;
            double bestValue = 0;
            List<int> bestSolution = new List<int>();
            
            Console.WriteLine("\nBranch and Bound Tree:");
            BranchAndBound(0, 0, 0, new List<int>(), values, weights, capacity, ref bestValue, ref bestSolution, "1");
            
            return new Tuple<double, List<int>>(bestValue, bestSolution);
        }

        private static void BranchAndBound(int level, double currentValue, double currentWeight, 
            List<int> currentSolution, double[] values, double[] weights, double capacity, 
            ref double bestValue, ref List<int> bestSolution, string nodeId)
        {
            // Display current table
            string branchInfo = GetBranchInfo(level, currentSolution);
            DisplayTable(nodeId, branchInfo, level, currentSolution, values, weights, capacity, currentValue, currentWeight);
            
            if (level == values.Length)
            {
                if (currentValue > bestValue)
                {
                    bestValue = currentValue;
                    bestSolution = new List<int>(currentSolution);
                    CenterText($"*** New Best Solution: {bestValue} ***");
                    Console.WriteLine();
                }
                return;
            }

            double upperBound = currentValue + GetUpperBound(level, currentWeight, values, weights, capacity);
            
            if (upperBound <= bestValue)
            {
                CenterText($"Bound exceeded (UB: {upperBound:F2} <= Best: {bestValue:F2})");
                Console.WriteLine();
                return;
            }

            // Get fractional value for branching display
            double remaining = capacity - currentWeight;
            double fractional = Math.Min(1.0, remaining / weights[level]);
            string fractionalDisplay = fractional == 1.0 ? "1" : $"{fractional:F2}";
            
            CenterText($"Branch on x{level + 1} = {fractionalDisplay} ({remaining:F0}/{weights[level]:F0}):");
            Console.WriteLine();

            // Include current item
            if (currentWeight + weights[level] <= capacity)
            {
                currentSolution.Add(level);
                BranchAndBound(level + 1, currentValue + values[level], currentWeight + weights[level], 
                    currentSolution, values, weights, capacity, ref bestValue, ref bestSolution, nodeId + ".1");
                currentSolution.RemoveAt(currentSolution.Count - 1);
            }
            else
            {
                CenterText($"Table {nodeId}.1 (x{level + 1} = 1) - Infeasible (exceeds capacity)");
                Console.WriteLine();
            }

            // Exclude current item
            BranchAndBound(level + 1, currentValue, currentWeight, currentSolution, 
                values, weights, capacity, ref bestValue, ref bestSolution, nodeId + ".2");
        }

        private static string GetBranchInfo(int level, List<int> solution)
        {
            if (level == 0) return "";
            
            var branches = new List<string>();
            for (int i = 0; i < level; i++)
            {
                int value = solution.Contains(i) ? 1 : 0;
                branches.Add($"x{i + 1} = {value}");
            }
            return $" ({string.Join(", ", branches)})";
        }

        private static void DisplayTable(string nodeId, string branchInfo, int level, List<int> solution, double[] values, 
            double[] weights, double capacity, double currentValue, double currentWeight)
        {
            CenterText($"Table {nodeId}{branchInfo}");
            CenterText("┌─────────┬─────────┐");
            CenterText("│ Variable│  Value  │");
            CenterText("├─────────┼─────────┤");
            
            for (int i = 0; i < values.Length; i++)
            {
                string value;
                if (solution.Contains(i))
                    value = "1";
                else if (i < level)
                    value = "0";
                else if (i == level)
                {
                    double remaining = capacity - currentWeight;
                    double fractional = Math.Min(1.0, remaining / weights[i]);
                    value = fractional == 1.0 ? "1" : $"{fractional:F2}";
                }
                else
                    value = "-";
                    
                CenterText($"│   x{i + 1}   │  {value,5}  │");
            }
            
            CenterText("├─────────┼─────────┤");
            CenterText($"│  Value  │ {currentValue,7:F1} │");
            CenterText($"│ Weight  │ {currentWeight,7:F1} │");
            CenterText($"│Capacity │ {capacity,7:F1} │");
            CenterText("└─────────┴─────────┘");
            Console.WriteLine();
        }

        private static void CenterText(string text)
        {
            int consoleWidth = Console.WindowWidth;
            int padding = Math.Max(0, (consoleWidth - text.Length) / 2);
            Console.WriteLine(new string(' ', padding) + text);
        }

        private static double GetUpperBound(int level, double currentWeight, double[] values, double[] weights, double capacity)
        {
            double remainingCapacity = capacity - currentWeight;
            double upperBound = 0;

            for (int i = level; i < values.Length && remainingCapacity > 0; i++)
            {
                if (weights[i] <= remainingCapacity)
                {
                    upperBound += values[i];
                    remainingCapacity -= weights[i];
                }
                else
                {
                    upperBound += values[i] * (remainingCapacity / weights[i]);
                    break;
                }
            }

            return upperBound;
        }
    }
=======
    // Simple logger interface for knapsack algorithm
    public interface IKnapsackLogger
    {
        void Log(string message);
        void LogHeader(string title);
    }
    
    // Simple result class for knapsack algorithm
    public class KnapsackResult
    {
        public string Status { get; set; } = "Unknown";
        public double Objective { get; set; }
        public double[] X { get; set; } = new double[0];
        public int Iterations { get; set; } = 0;
    }

    // Simple class to represent a knapsack item
    public class KnapsackItem
    {
        public int Index { get; set; }      // Item number (0, 1, 2, ...)
        public double Weight { get; set; }   // How much it weighs
        public double Value { get; set; }    // How much it's worth
        public double Ratio => Value / Weight; // Value per weight (efficiency)
    }

    // Represents a node in our branch and bound tree
    public class KnapsackNode
    {
        public int Level { get; set; }           // Which item we're deciding on
        public double CurrentWeight { get; set; } // Total weight so far
        public double CurrentValue { get; set; }  // Total value so far
        public double UpperBound { get; set; }    // Best possible value from this node
        public bool[] Solution { get; set; }      // Which items we've taken (true/false)
        public bool Include { get; set; }         // Should we include the current item?
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
            
            // Step 1: Set up the problem
            _capacity = capacity;
            _bestValue = 0;
            _nodeCount = 0;
            
            // Create items and sort by value/weight ratio (greedy heuristic)
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
            
            // Sort items by efficiency (value/weight ratio) - best first
            _items = _items.OrderByDescending(item => item.Ratio).ToList();
            _bestSolution = new bool[_items.Count];
            
            _logger.Log($"Knapsack capacity: {capacity}");
            _logger.Log("Items sorted by value/weight ratio:");
            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                _logger.Log($"  Item {item.Index}: weight={item.Weight}, value={item.Value}, ratio={item.Ratio:F3}");
            }
            
            // Step 2: Start branch and bound
            var rootNode = new KnapsackNode
            {
                Level = -1,
                CurrentWeight = 0,
                CurrentValue = 0,
                Solution = new bool[_items.Count]
            };
            
            rootNode.UpperBound = CalculateUpperBound(rootNode);
            
            // Use a priority queue (simulate with list and sorting)
            var queue = new List<KnapsackNode> { rootNode };
            
            _logger.Log($"Starting branch and bound with upper bound: {rootNode.UpperBound:F3}");
            
            // Step 3: Process nodes until queue is empty
            while (queue.Count > 0)
            {
                // Get node with highest upper bound
                queue = queue.OrderByDescending(n => n.UpperBound).ToList();
                var currentNode = queue[0];
                queue.RemoveAt(0);
                _nodeCount++;
                
                _logger.Log($"Node {_nodeCount}: Level {currentNode.Level}, Value {currentNode.CurrentValue:F3}, Weight {currentNode.CurrentWeight:F3}, UB {currentNode.UpperBound:F3}");
                
                // If this node can't beat our best solution, skip it
                if (currentNode.UpperBound <= _bestValue)
                {
                    _logger.Log("  Pruned: Upper bound <= best value");
                    continue;
                }
                
                // If we've decided on all items, this is a leaf node
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
                
                // Create two child nodes: include next item and exclude next item
                int nextLevel = currentNode.Level + 1;
                var nextItem = _items[nextLevel];
                
                // Child 1: Include the next item
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
                
                // Child 2: Exclude the next item
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
            
            // Step 4: Build the result
            var result = new KnapsackResult
            {
                Status = "Optimal",
                Objective = Math.Round(_bestValue, 3),
                X = new double[weights.Length],
                Iterations = _nodeCount
            };
            
            // Map solution back to original item order
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
            _logger.Log($"Total weight used: {totalWeight} / {capacity}");
            
            return result;
        }
        
        // Calculate upper bound using fractional knapsack (greedy)
        private double CalculateUpperBound(KnapsackNode node)
        {
            double remainingCapacity = _capacity - node.CurrentWeight;
            double upperBound = node.CurrentValue;
            
            // Add items greedily (fractionally if needed)
            for (int i = node.Level + 1; i < _items.Count; i++)
            {
                var item = _items[i];
                
                if (item.Weight <= remainingCapacity)
                {
                    // Take the whole item
                    upperBound += item.Value;
                    remainingCapacity -= item.Weight;
                }
                else
                {
                    // Take fraction of the item
                    upperBound += (remainingCapacity / item.Weight) * item.Value;
                    break; // Can't take any more
                }
            }
            
            return upperBound;
        }
    }
>>>>>>> Stashed changes
}