using System;
using System.Collections.Generic;
using System.Linq;

namespace LPR381_Solver.Models
{
    public enum OptimizationType { Maximize, Minimize }
    public enum Sense { Max, Min }

    public class LPModel
    {
        public Dictionary<string, double> ObjectiveFunction { get; set; }
        public OptimizationType OptimizationType { get; set; }
        public List<Constraint> Constraints { get; set; }
        public List<Variable> Variables { get; set; }
        public double? OptimalValue { get; set; }
        public bool IsFeasible { get; set; } = true;
        public string Name { get; set; }
        public Sense Sense { get; set; }

        public LPModel()
        {
            ObjectiveFunction = new Dictionary<string, double>();
            Constraints = new List<Constraint>();
            Variables = new List<Variable>();
            Name = "Model";
        }

        public LPModel(string name, Sense sense)
        {
            Name = name ?? "Model";
            Sense = sense;
            ObjectiveFunction = new Dictionary<string, double>();
            Constraints = new List<Constraint>();
            Variables = new List<Variable>();
        }

        public int M => Constraints.Count; // number of constraints
        public int N => Variables.Count;   // number of variables

        public object ObjectiveCoefficients { get; internal set; }

        public LPModel Clone()
        {
            var clone = new LPModel
            {
                ObjectiveFunction = new Dictionary<string, double>(ObjectiveFunction),
                OptimizationType = OptimizationType,
                OptimalValue = OptimalValue,
                IsFeasible = IsFeasible,
                Name = Name,
                Sense = Sense
            };

            foreach (var constraint in Constraints)
            {
                clone.Constraints.Add(new Constraint
                {
                    Coefficients = new Dictionary<string, double>(constraint.Coefficients),
                    Type = constraint.Type,
                    RightHandSide = constraint.RightHandSide,
                    Name = constraint.Name
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

        public Variable AddVariable(string name, double cost, VarSign sign = VarSign.GE0, VarKind kind = VarKind.Continuous)
        {
            var v = new Variable(name, false);
            Variables.Add(v);
            return v;
        }

        public Constraint AddConstraint(string name, Dictionary<string, double> coefficients, ConstraintType type, double rhs)
        {
            var c = new Constraint(name)
            {
                Coefficients = coefficients,
                Type = type,
                RightHandSide = rhs
            };
            Constraints.Add(c);
            return c;
        }

        public override string ToString()
        {
            return $"{Name}: {Sense}, m={M}, n={N}";
        }
    }
}