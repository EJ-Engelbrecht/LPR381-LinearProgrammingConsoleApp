using System;

namespace LPR381_Solver.Models
{
    public class Variable
    {
        public string Name { get; set; }
        public bool IsInteger { get; set; }
        public double Value { get; set; }
        public double LowerBound { get; set; } = 0;
        public double UpperBound { get; set; } = double.MaxValue;

        public Variable(string name, bool isInteger)
        {
            Name = name;
            IsInteger = isInteger;
        }
    }

    /// <summary>
    /// Sign restriction of a variable.
    /// </summary>
    public enum VarSign
    {
        GE0,   // x >= 0
        LE0,   // x <= 0
        Free   // unrestricted
    }

    /// <summary>
    /// Kind of variable: Continuous, Integer, or Binary.
    /// </summary>
    public enum VarKind
    {
        Continuous,
        Integer,
        Binary
    }

    /// <summary>
    /// Relation type for a constraint: <=, >=, or =.
    /// </summary>
    public enum Rel
    {
        LE, // <=
        GE, // >=
        EQ  // =
    }
}