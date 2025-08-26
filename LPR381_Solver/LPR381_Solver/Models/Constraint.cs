using System.Collections.Generic;

namespace LPR381_Solver.Models
{
<<<<<<< HEAD
    public enum ConstraintType { LessEqual, GreaterEqual, Equal }

    public class Constraint
    {
        public Dictionary<string, double> Coefficients { get; set; }
        public ConstraintType Type { get; set; }
        public double RightHandSide { get; set; }

        public Constraint()
        {
            Coefficients = new Dictionary<string, double>();
=======
  
    /// Relation type for a constraint: <=, >=, or =.
    public enum Rel
    {
        LE, // <=
        GE, // >=
        EQ  // =
    }

    
    /// Represents a linear constraint of the form a·x (<=,=,>=) b.
    internal class Constraint
    {
        public string Name { get; }
        public double[] Coefficients { get; } // aligned with LPModel.Variables order
        public Rel Relation { get; }
        public double Rhs { get; set; } // right-hand side

        public Constraint(string name, double[] coefficients, Rel relation, double rhs)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Coefficients = coefficients ?? throw new ArgumentNullException(nameof(coefficients));
            Relation = relation;
            Rhs = rhs;
        }

        public override string ToString()
        {
            string relStr;
            switch (Relation)
            {
                case Rel.LE:
                    relStr = "<=";
                    break;
                case Rel.GE:
                    relStr = ">=";
                    break;
                case Rel.EQ:
                    relStr = "=";
                    break;
                default:
                    relStr = "?";
                    break;
            }
            return $"{Name}: (a·x) {relStr} {Rhs}";
>>>>>>> main
        }
    }
}