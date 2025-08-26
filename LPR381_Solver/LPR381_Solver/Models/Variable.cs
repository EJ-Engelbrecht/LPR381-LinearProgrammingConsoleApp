using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LPR381_Solver.Models
{
    /// Sign restriction of a variable.
    /// "+" (>= 0), "-" (<= 0), "urs" (unrestricted/free).
    public enum VarSign
    {
        GE0,   // x >= 0
        LE0,   // x <= 0
        Free   // unrestricted
    }

    
    /// Kind of variable: Continuous, Integer, or Binary.
    public enum VarKind
    {
        Continuous,
        Integer,
        Binary
    }


    /// Represents a decision variable in the LP model.
    internal class Variable
    {
        public string Name { get; }
        public double Cost { get; set; }   // Objective coefficient c_j
        public VarSign Sign { get; }
        public VarKind Kind { get; }

        public Variable(string name, double cost, VarSign sign = VarSign.GE0, VarKind kind = VarKind.Continuous)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Cost = cost;
            Sign = sign;
            Kind = kind;
        }

        public override string ToString()
        {
            return $"{Name} (c={Cost}, sign={Sign}, kind={Kind})";
        }
    }
}
