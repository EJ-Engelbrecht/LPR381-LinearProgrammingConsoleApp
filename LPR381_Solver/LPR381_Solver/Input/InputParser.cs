using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LPR381_Solver.Input
{
    public static LPModel LoadModel(string path)
    {
        var lines = File.ReadAllLines(path);
        var model = new LPModel();

        string[] objectiveParts = lines[0].Split(' ');
        model.IsMaximization = objectiveParts[0].ToLower() == "max";

        for (int i = 1; i < objectiveParts.Length; i += 2)
        {
            double coeff = double.Parse(objectiveParts[i + 1]);
            model.ObjectiveCoefficients.Add(objectiveParts[i] == "+" ? coeff : -coeff);
        }

        for (int i = 1; i < lines.Length - 1; i++)
        {
            string[] parts = lines[i].Split(' ');
            var constraint = new Constraint();

            for (int j = 0; j < model.ObjectiveCoefficients.Count; j++)
            {
                double coeff = double.Parse(parts[2 * j + 1]);
                constraint.Coefficients.Add(parts[2 * j] == "+" ? coeff : -coeff);
            }

            constraint.Relation = parts[^2];
            constraint.RHS = double.Parse(parts[^1]);
            model.Constraints.Add(constraint);
        }

        model.SignRestrictions = lines[^1].Split(' ').ToList();
        return model;
    }
}
