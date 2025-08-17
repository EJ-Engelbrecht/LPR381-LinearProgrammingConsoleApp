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
}