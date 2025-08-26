using LPR381_Solver.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LPR381_Solver.Utils
{
    public class CanonicalFormConverter
    {
        public static string ShowConversion(string testId)
        {
            string testCaseText = GetTestCaseById(testId);
            if (testCaseText == null)
            {
                return "Test case not found.";
            }

            var result = "\nORIGINAL:\n" + testCaseText;
            var model = ParseFromText(testCaseText);
            result += "\n\nCANONICAL FORM:\n" + GetCanonicalFormString(model);
            return result;
        }

        public static string GetTestCaseById(string testId)
        {
            try
            {
                string path = FindTestCasesFile();
                if (path == null) return null;

                string content = File.ReadAllText(path);
                var sections = content.Split('#').Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

                foreach (var section in sections)
                {
                    var lines = section.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
                    if (lines.Length >= 3 && lines[0] == testId)
                    {
                        return string.Join("\n", lines.Skip(1));
                    }
                }
            }
            catch (Exception)
            {
                // Log error but don't use Console
            }
            return null;
        }

        private static string FindTestCasesFile()
        {
            string[] paths = {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Tests", "TestCases.txt"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tests", "TestCases.txt")
            };

            foreach (var path in paths)
            {
                if (File.Exists(path)) return path;
            }
            return null;
        }

        private static string GetCanonicalFormString(LPModel model)
        {
            // Variables
            var allVars = new List<string>(model.Variables.Select(v => v.Name));
            int slackCount = model.Constraints.Count(c => c.Type == ConstraintType.LessEqual);
            int excessCount = model.Constraints.Count(c => c.Type == ConstraintType.GreaterEqual);
            
            for (int i = 1; i <= slackCount; i++)
                allVars.Add($"s{i}");
            for (int i = 1; i <= excessCount; i++)
                allVars.Add($"e{i}");

            var result = $"Variables: {string.Join(", ", allVars)}\n";

            // Objective
            var objTerms = model.ObjectiveFunction.Select(kv => $"{-kv.Value}{kv.Key}");
            result += $"Objective: {string.Join(" ", objTerms)} = 0\n";

            // Constraints
            result += "Constraints:\n";
            int slack = 1;
            int excess = 1;
            
            foreach (var constraint in model.Constraints)
            {
                if (constraint.Type == ConstraintType.LessEqual)
                {
                    var terms = constraint.Coefficients.Select(kv => $"{kv.Value}{kv.Key}").ToList();
                    terms.Add($"s{slack++}");
                    result += $"  {string.Join(" + ", terms)} = {constraint.RightHandSide}\n";
                }
                else if (constraint.Type == ConstraintType.GreaterEqual)
                {
                    var terms = constraint.Coefficients.Select(kv => $"{-kv.Value}{kv.Key}").ToList();
                    terms.Add($"e{excess++}");
                    result += $"  {string.Join(" + ", terms)} = {-constraint.RightHandSide}\n";
                }
            }

            result += $"  {string.Join(", ", allVars)} >= 0\n";
            return result;
        }

        public static LPModel ParseFromText(string text)
        {
            var model = new LPModel();
            var lines = text.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToArray();

            foreach (var line in lines)
            {
                if (line.StartsWith("max") || line.StartsWith("min"))
                {
                    ParseObjective(line, model);
                }
                else if (line.Contains(":"))
                {
                    ParseConstraint(line, model);
                }
            }

            return model;
        }

        private static void ParseObjective(string line, LPModel model)
        {
            model.OptimizationType = line.StartsWith("max") ? OptimizationType.Maximize : OptimizationType.Minimize;

            var objPart = line.Substring(line.IndexOf('=') + 1).Trim();
            var terms = Regex.Matches(objPart, @"([+-]?\s*\d*)\s*x(\d+)");

            foreach (Match match in terms)
            {
                var coeff = match.Groups[1].Value.Replace(" ", "");
                if (string.IsNullOrEmpty(coeff) || coeff == "+") coeff = "1";
                if (coeff == "-") coeff = "-1";

                var varName = "x" + match.Groups[2].Value;
                model.ObjectiveFunction[varName] = double.Parse(coeff);

                if (!model.Variables.Any(v => v.Name == varName))
                    model.Variables.Add(new Variable(varName, false));
            }
        }

        private static void ParseConstraint(string line, LPModel model)
        {
            var parts = line.Split(':');
            var constraintPart = parts[1].Trim();

            var constraint = new Constraint();

            string op = "<=";
            if (constraintPart.Contains(">=")) op = ">=";
            else if (constraintPart.Contains("=") && !constraintPart.Contains("<=")) op = "=";

            constraint.Type = op == "<=" ? ConstraintType.LessEqual :
                             op == ">=" ? ConstraintType.GreaterEqual : ConstraintType.Equal;

            var sides = constraintPart.Split(new[] { "<=", ">=", "=" }, StringSplitOptions.None);
            var leftSide = sides[0].Trim();
            constraint.RightHandSide = double.Parse(sides[1].Trim());

            var terms = Regex.Matches(leftSide, @"([+-]?\s*\d*)\s*x(\d+)");

            foreach (Match match in terms)
            {
                var coeff = match.Groups[1].Value.Replace(" ", "");
                if (string.IsNullOrEmpty(coeff) || coeff == "+") coeff = "1";
                if (coeff == "-") coeff = "-1";

                var varName = "x" + match.Groups[2].Value;
                constraint.Coefficients[varName] = double.Parse(coeff);
            }

            model.Constraints.Add(constraint);
        }
    }
}