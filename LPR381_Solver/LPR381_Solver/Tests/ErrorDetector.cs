public class ErrorDetector
{
    public static bool IsUnbounded(LPModel model)
    {
        // Simple check: if no constraints limit growth of objective
        return model.Constraints.All(c => c.Relation == "<=" && c.RHS > 100000);
    }

    public static bool IsInfeasible(LPModel model)
    {
        // Check for contradictory constraints
        return model.Constraints.Any(c => c.RHS < 0 && c.Relation == ">=");
    }
}