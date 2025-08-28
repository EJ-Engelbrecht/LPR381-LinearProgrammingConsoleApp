using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LPR381_Solver.Models;

namespace LPR381_Solver.Input
{
    public class InputParser
    {
        public static LPModel ParseFromFile(string filePath)
        {
            string content = File.ReadAllText(filePath);
            return ParseFromText(content);
        }

        public static LPModel ParseFromText(string text)
        {
            var model = new LPModel();
            var lines = text.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToArray();

            if (lines.Length == 0) return model;

            // Parse objective function (first line)
            ParseObjectiveFunction(lines[0], model);

            // Parse constraints (middle lines)
            for (int i = 1; i < lines.Length - 1; i++)
            {
                ParseConstraintLine(lines[i], model);
            }

            // Parse sign restrictions (last line)
            if (lines.Length > 1)
            {
                ParseSignRestrictions(lines[lines.Length - 1], model);
            }

            return model;
        }

        private static void ParseObjectiveFunction(string line, LPModel model)
        {
            var parts = line.Split(' ');
            if (parts.Length < 2) return;

            // First part is max/min
            model.OptimizationType = parts[0].ToLower() == "max" ? OptimizationType.Maximize : OptimizationType.Minimize;

            // Remaining parts are coefficients with signs
            for (int i = 1; i < parts.Length; i++)
            {
                if (!string.IsNullOrEmpty(parts[i]))
                {
                    double coeff = double.Parse(parts[i], CultureInfo.InvariantCulture);
                    string varName = $"x{i}";
                    model.ObjectiveFunction[varName] = coeff;
                    model.Variables.Add(new Variable(varName, false));
                }
            }
        }

        private static void ParseConstraintLine(string line, LPModel model)
        {
            var parts = line.Split(' ');
            if (parts.Length < 3) return;

            var constraint = new Constraint();
            int varCount = model.Variables.Count;
            
            // Parse coefficients (first varCount parts)
            for (int i = 0; i < varCount && i < parts.Length; i++)
            {
                if (!string.IsNullOrEmpty(parts[i]))
                {
                    double coeff = double.Parse(parts[i], CultureInfo.InvariantCulture);
                    string varName = $"x{i + 1}";
                    constraint.Coefficients[varName] = coeff;
                }
            }

            // Parse operator and RHS (last two parts)
            if (parts.Length >= 2)
            {
                string op = parts[parts.Length - 2];
                constraint.Type = op == "<=" ? ConstraintType.LessEqual :
                                 op == ">=" ? ConstraintType.GreaterEqual : ConstraintType.Equal;
                
                constraint.RightHandSide = double.Parse(parts[parts.Length - 1], CultureInfo.InvariantCulture);
            }

            model.Constraints.Add(constraint);
        }

        private static void ParseSignRestrictions(string line, LPModel model)
        {
            var parts = line.Split(' ');
            for (int i = 0; i < parts.Length && i < model.Variables.Count; i++)
            {
                var variable = model.Variables[i];
                string restriction = parts[i].ToLower();
                
                if (restriction == "int" || restriction == "bin")
                {
                    variable.IsInteger = true;
                }
            }
        }
    }
}
