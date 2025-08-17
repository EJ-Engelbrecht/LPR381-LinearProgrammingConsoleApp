using LPR381_Solver.Models;
using LPR381_Solver.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LPR381_Solver.Algorithms
{
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
}