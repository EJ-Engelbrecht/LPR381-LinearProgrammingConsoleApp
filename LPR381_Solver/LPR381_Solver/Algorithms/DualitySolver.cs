using LPR381_Solver.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LPR381_Solver.Algorithms
{

    /// Builds the dual of a primal LP (relaxes ints/bins).
    /// Provides duality verification utilities.
 
    internal static class DualitySolver
    {
        internal class DualBuildResult
        {
            public LPModel Dual { get; set; }
            public string MappingNote { get; set; }
        }

        /// Build the dual LP following standard mapping rules.
        /// - If primal is Max, dual is Min (and vice versa).
        /// - Dual variables correspond to primal constraints.
        /// - Dual constraints correspond to primal variables.
        /// - Integers/binaries are relaxed to continuous in the dual.
        public static DualBuildResult BuildDual(LPModel P)
        {
            var mats = P.ToMatrices();
            var A = mats.Item1;
            var b = mats.Item2;
            var c = mats.Item3;
            int m = P.M, n = P.N;

            // Dual objective sense flips
            var D = new LPModel("Dual(" + P.Name + ")", P.Sense == Sense.Max ? Sense.Min : Sense.Max);

            // Add dual variables (from primal constraints)
            for (int i = 0; i < m; i++)
            {
                VarSign sign;
                switch (P.Constraints[i].Relation)
                {
                    case Rel.LE: sign = VarSign.GE0; break;
                    case Rel.GE: sign = VarSign.LE0; break;
                    case Rel.EQ: sign = VarSign.Free; break;
                    default: sign = VarSign.GE0; break;
                }

                D.AddVariable("y" + (i + 1), P.Constraints[i].Rhs, sign, VarKind.Continuous);
            }

            // Add dual constraints (from primal variables)
            for (int j = 0; j < n; j++)
            {
                var coeffs = new double[m];
                for (int i = 0; i < m; i++) coeffs[i] = A[i, j];

                Rel rel;
                switch (P.Variables[j].Sign)
                {
                    case VarSign.GE0: rel = Rel.GE; break;
                    case VarSign.LE0: rel = Rel.LE; break;
                    case VarSign.Free: rel = Rel.EQ; break;
                    default: rel = Rel.GE; break;
                }

                D.AddConstraint("dual_constr_" + (j + 1), coeffs, rel, P.Variables[j].Cost);
            }

            var result = new DualBuildResult();
            result.Dual = D;
            result.MappingNote = "Dual built via A^T; var signs from primal row relations; " +
                                 "constraint senses from primal var signs; ints/bins relaxed.";

            return result;
        }


        /// Verify weak and strong duality given primal and dual objective values.
        public static Tuple<bool, bool, string> VerifyDuality(Sense primalSense, double zPrimal, double zDual, double tol = 1e-6)
        {
            bool weak, strong;
            if (primalSense == Sense.Max)
            {
                weak = zPrimal <= zDual + tol;       // Weak duality: z_P <= z_D
                strong = Math.Abs(zPrimal - zDual) <= tol && weak;
            }
            else
            {
                weak = zPrimal >= zDual - tol;       // For Min primal, inequality reverses
                strong = Math.Abs(zPrimal - zDual) <= tol && weak;
            }

            string note;
            if (strong) note = "Strong Duality verified (within tolerance).";
            else if (weak) note = "Weak Duality holds but not equal within tolerance.";
            else note = "Weak Duality violated (check feasibility).";

            return new Tuple<bool, bool, string>(weak, strong, note);
        }

 
        /// Helper to round to 3 decimals for consistent output.
        public static double R3(double v)
        {
            return Math.Round(v, 3);
        }
    }
}
