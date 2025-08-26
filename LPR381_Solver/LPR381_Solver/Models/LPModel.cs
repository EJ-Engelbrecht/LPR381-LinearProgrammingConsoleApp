using System.Collections.Generic;

namespace LPR381_Solver.Models
{
<<<<<<< HEAD
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
=======
    public enum Sense
    {
        Max,
        Min
    }

   
    /// Represents a linear programming model with variables and constraints.
    internal class LPModel
    {
        public string Name { get; }
        public Sense Sense { get; }
        public List<Variable> Variables { get; } = new List<Variable>();
        public List<Constraint> Constraints { get; } = new List<Constraint>();

        public LPModel(string name, Sense sense)
        {
            Name = name ?? "Model";
            Sense = sense;
        }

        public int M => Constraints.Count; // number of constraints
        public int N => Variables.Count;   // number of variables

        public Variable AddVariable(string name, double cost, VarSign sign = VarSign.GE0, VarKind kind = VarKind.Continuous)
        {
            var v = new Variable(name, cost, sign, kind);
            Variables.Add(v);
            return v;
        }

        public Constraint AddConstraint(string name, double[] coefficients, Rel relation, double rhs)
        {
            if (coefficients.Length != N)
                throw new ArgumentException($"Coefficient length {coefficients.Length} must equal number of variables {N}.");

            var c = new Constraint(name, coefficients.ToArray(), relation, rhs);
            Constraints.Add(c);
            return c;
        }


        /// Returns dense (A, b, c) arrays aligned to variable order.
        public (double[,] A, double[] b, double[] c) ToMatrices()
        {
            var A = new double[M, N];
            var b = new double[M];
            var c = new double[N];

            for (int j = 0; j < N; j++)
                c[j] = Variables[j].Cost;

            for (int i = 0; i < M; i++)
            {
                var row = Constraints[i].Coefficients;
                for (int j = 0; j < N; j++)
                    A[i, j] = row[j];
                b[i] = Constraints[i].Rhs;
            }

            return (A, b, c);
        }

        public override string ToString()
        {
            return $"{Name}: {Sense}, m={M}, n={N}";
>>>>>>> main
        }
    }
}