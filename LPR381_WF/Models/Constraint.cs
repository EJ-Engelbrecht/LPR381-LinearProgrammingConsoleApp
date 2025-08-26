using System;
using System.Collections.Generic;

namespace LPR381_Solver.Models
{
    public enum ConstraintType { LessEqual, GreaterEqual, Equal }

    public class Constraint
    {
        public Dictionary<string, double> Coefficients { get; set; }
        public ConstraintType Type { get; set; }
        public double RightHandSide { get; set; }
        public string Name { get; set; }

        public Constraint()
        {
            Coefficients = new Dictionary<string, double>();
        }

        public Constraint(string name)
        {
            Name = name;
            Coefficients = new Dictionary<string, double>();
        }

        public override string ToString()
        {
            string relStr = Type == ConstraintType.LessEqual ? "<=" :
                           Type == ConstraintType.GreaterEqual ? ">=" : "=";
            return $"{Name}: {string.Join(" + ", Coefficients)} {relStr} {RightHandSide}";
        }
    }
}