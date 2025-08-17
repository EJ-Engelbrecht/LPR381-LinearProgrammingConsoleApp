using System.Collections.Generic;

namespace LPR381_Solver.Models
{
    public enum ConstraintType { LessEqual, GreaterEqual, Equal }

    public class Constraint
    {
        public Dictionary<string, double> Coefficients { get; set; }
        public ConstraintType Type { get; set; }
        public double RightHandSide { get; set; }

        public Constraint()
        {
            Coefficients = new Dictionary<string, double>();
        }
    }
}