using System;
using System.Linq;

namespace LPR381_Solver.Algorithms
{
    public class CuttingPlane
    {
        private readonly RevisedSimplex _lpSolver;
        private readonly IIterationLogger _log;

        public CuttingPlane(RevisedSimplex lpSolver, IIterationLogger logger)
        {
            _lpSolver = lpSolver;
            _log = logger;
        }

        public SolveResult Solve(CanonicalForm cf)
        {
            _log.LogHeader("Cutting Plane (Gomory)");

            var intMask = cf.VariableTypes.Select(t => t == VarType.Int || t == VarType.Bin).ToArray();
            var current = cf.Clone();

            for (int iter = 1; iter <= 50; iter++)
            {
                var lp = _lpSolver.Solve(current);
                if (lp.Status != "Optimal")
                {
                    _log.Log($"LP status: {lp.Status}. Abort.");
                    return lp;
                }

                int fracIndex = -1;
                for (int j = 0; j < current.N; j++)
                {
                    if (intMask[j])
                    {
                        var frac = Math.Abs(lp.X[j] - Math.Round(lp.X[j]));
                        if (frac > 1e-6) { fracIndex = j; break; }
                    }
                }
                if (fracIndex == -1)
                {
                    lp.Status = "Optimal (Integer)";
                    _log.Log($"All integers integral after {iter-1} cuts. Z = {lp.Objective:0.###}");
                    return lp;
                }

                double rhs = Math.Floor(lp.X[fracIndex]);
                _log.Log($"Cut {iter}: x{fracIndex+1} <= {rhs} (from fractional {lp.X[fracIndex]:0.###})");

                var A2 = new double[current.M + 1, current.N];
                for (int i = 0; i < current.M; i++)
                    for (int j = 0; j < current.N; j++)
                        A2[i, j] = current.A[i, j];
                A2[current.M, fracIndex] = 1.0;

                var b2 = new double[current.M + 1];
                Array.Copy(current.b, b2, current.M);
                b2[current.M] = rhs;

                var signs2 = new ConstraintSign[current.M + 1];
                Array.Copy(current.Signs, signs2, current.M);
                signs2[current.M] = ConstraintSign.LE;

                current = new CanonicalForm
                {
                    Sense = cf.Sense,
                    A = A2,
                    b = b2,
                    c = cf.c.ToArray(),
                    Signs = signs2,
                    VariableTypes = cf.VariableTypes.ToArray(),
                    VariableNames = cf.VariableNames?.ToArray()
                };
            }

            return new SolveResult { Status = "CutLimit" };
        }
    }
}
