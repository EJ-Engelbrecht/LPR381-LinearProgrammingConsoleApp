public class TestCaseManager
{
    public static void RunEdgeCases()
    {
        Console.WriteLine("Running infeasible model test...");
        var infeasibleModel = new LPModel
        {
            ObjectiveCoefficients = new List<double> { 1, 2 },
            Constraints = new List<Constraint>
            {
                new Constraint { Coefficients = new List<double>{1, 1}, Relation = "<=", RHS = 5 },
                new Constraint { Coefficients = new List<double>{1, 1}, Relation = ">=", RHS = 10 }
            }
        };

        if (ErrorDetector.IsInfeasible(infeasibleModel))
            Console.WriteLine("✅ Detected infeasible model.");

        Console.WriteLine("Running unbounded model test...");
        var unboundedModel = new LPModel
        {
            ObjectiveCoefficients = new List<double> { 1, 2 },
            Constraints = new List<Constraint>
            {
                new Constraint { Coefficients = new List<double>{-1, -1}, Relation = "<=", RHS = -100000 }
            }
        };

        if (ErrorDetector.IsUnbounded(unboundedModel))
            Console.WriteLine("✅ Detected unbounded model.");
    }
}