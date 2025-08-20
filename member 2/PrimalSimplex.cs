using System;
using System.Collections.Generic;
using System.Linq;

namespace LPR381.Core
{
    public class PrimalSimplex
    {
        private readonly IIterationLogger _log;
        private readonly double _M;
        private readonly double _eps;
        public PrimalSimplex(IIterationLogger logger, double bigM = 1e6, double eps = 1e-9)
        {
            _log = logger;
            _M = bigM;
            _eps = eps;
        }

        public SolveResult Solve(CanonicalForm cf)
        {
            _log.LogHeader("Primal Simplex (Tableau)");
            var res = new SolveResult();
            try
            {
                BuildTableau(cf, out var T, out var basis, out var varNames, out int objRow, out int rhsCol);
                PrintTableau(T, objRow, rhsCol, varNames, basis);

                int it = 0;
                while (true)
                {
                    it++;
                    int enter = ChooseEntering(T, objRow);
                    if (enter < 0) break; // optimal

                    int leave = ChooseLeaving(T, enter, rhsCol, out bool unbounded);
                    if (unbounded)
                    {
                        res.Status = "Unbounded";
                        _log.Log("Detected unbounded: no positive entry in entering column.");
                        return res;
                    }

                    Pivot(T, leave, enter);
                    basis[leave] = enter;
                    _log.Log($"Pivot: Row {leave} leaves, Col {enter} enters.");
                    PrintTableau(T, objRow, rhsCol, varNames, basis);
                    if (it > 10000) throw new Exception("Iteration limit exceeded.");
                }

                res.Status = "Optimal";
                res.Iterations = it;
                var x = new double[varNames.Length - 1];
                for (int i = 0; i < basis.Length; i++)
                {
                    int col = basis[i];
                    if (col < x.Length) x[col] = T[i, rhsCol];
                }

                double z = T[objRow, rhsCol];
                if (cf.Sense == ProblemSense.Min) z = -z;

                if (HasPositiveArtificial(varNames, basis, out int artIndex))
                {
                    res.Status = "Infeasible";
                    _log.Log("Artificial variable remains in basis at positive level -> Infeasible.");
                }

                res.X = x.Select(v => Math.Round(v, 3)).ToArray();
                res.Objective = Math.Round(z, 3);
                _log.Log($"Final Solution: Status={res.Status}, Z={res.Objective:0.###}");
                for (int i = 0; i < x.Length; i++) _log.Log($"x{i+1} = {res.X[i]:0.###}");
                return res;
            }
            catch (Exception ex)
            {
                _log.Log("Error in PrimalSimplex: " + ex.Message);
                res.Status = "Error";
                return res;
            }
        }

        // --- (helper methods omitted here due to length, same as in my zip version) ---
        // Includes: BuildTableau, ChooseEntering, ChooseLeaving, Pivot, HasPositiveArtificial, PrintTableau
    }
}
