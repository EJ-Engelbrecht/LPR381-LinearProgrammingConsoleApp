using System.Collections.Generic;

namespace LPR381_Solver.Models
{
    public enum OptimizationType { Maximize, Minimize }

    public class LPModel
    {
        public Dictionary<string, double> ObjectiveFunction { get; set; }
        public OptimizationType OptimizationType { get; set; }
        public List<Constraint> Constraints { get; set; }
        public List<Variable> Variables { get; set; }
        public double? OptimalValue { get; set; }
        public bool IsFeasible { get; set; } = true;

        public LPModel()
        {
            ObjectiveFunction = new Dictionary<string, double>();
            Constraints = new List<Constraint>();
            Variables = new List<Variable>();
        }

        public LPModel Clone()
        {
            var clone = new LPModel
            {
                ObjectiveFunction = new Dictionary<string, double>(ObjectiveFunction),
                OptimizationType = OptimizationType,
                OptimalValue = OptimalValue,
                IsFeasible = IsFeasible
            };

            foreach (var constraint in Constraints)
            {
                clone.Constraints.Add(new Constraint
                {
                    Coefficients = new Dictionary<string, double>(constraint.Coefficients),
                    Type = constraint.Type,
                    RightHandSide = constraint.RightHandSide
                });
            }

            foreach (var variable in Variables)
            {
                clone.Variables.Add(new Variable(variable.Name, variable.IsInteger)
                {
                    Value = variable.Value,
                    LowerBound = variable.LowerBound,
                    UpperBound = variable.UpperBound
                });
            }

            return clone;
        }
    }
}