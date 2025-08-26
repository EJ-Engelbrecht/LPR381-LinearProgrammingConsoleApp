using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LPR381_Solver.Output
{
    public static void Export(string path)
    {
        using StreamWriter writer = new StreamWriter(path);
        writer.WriteLine("=== Canonical Form ===");
        writer.WriteLine(Solver.CanonicalForm);

        writer.WriteLine("\n=== Iterations ===");
        foreach (var iteration in Solver.Iterations)
        {
            writer.WriteLine(iteration);
        }

        writer.WriteLine($"\nOptimal Value: {Solver.OptimalValue:F3}");
    }

}
