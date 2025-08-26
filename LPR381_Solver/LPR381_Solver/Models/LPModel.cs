using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LPR381_Solver.Models
{
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
        }
    }
}
