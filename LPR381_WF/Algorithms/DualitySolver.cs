using LPR381_Solver.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LPR381_Solver.Algorithms
{
    internal static class DualitySolver
    {
        internal class DualBuildResult
        {
            public LPModel Dual { get; set; }
            public string MappingNote { get; set; }
        }

        public static DualBuildResult BuildDual(LPModel P)
        {
            int m = P.M, n = P.N;
            
            var D = new LPModel("Dual(" + P.Name + ")", P.Sense == Sense.Max ? Sense.Min : Sense.Max);

            for (int i = 0; i < m; i++)
            {
                D.Variables.Add(new Variable("y" + (i + 1), false));
            }

            for (int j = 0; j < n; j++)
            {
                var constraint = new Constraint("dual_constr_" + (j + 1));
                constraint.Type = ConstraintType.GreaterEqual;
                
                string varName = P.Variables[j].Name;
                constraint.RightHandSide = P.ObjectiveFunction.ContainsKey(varName) ? P.ObjectiveFunction[varName] : 0;
                
                for (int i = 0; i < m; i++)
                {
                    double coeff = P.Constraints[i].Coefficients.ContainsKey(varName) ? P.Constraints[i].Coefficients[varName] : 0;
                    constraint.Coefficients["y" + (i + 1)] = coeff;
                }
                D.Constraints.Add(constraint);
            }

            var result = new DualBuildResult();
            result.Dual = D;
            result.MappingNote = "Dual built via standard transformation";

            return result;
        }

        public static Tuple<bool, bool, string> VerifyDuality(Sense primalSense, double zPrimal, double zDual, double tol = 1e-6)
        {
            bool weak, strong;
            if (primalSense == Sense.Max)
            {
                weak = zPrimal <= zDual + tol;
                strong = Math.Abs(zPrimal - zDual) <= tol && weak;
            }
            else
            {
                weak = zPrimal >= zDual - tol;
                strong = Math.Abs(zPrimal - zDual) <= tol && weak;
            }

            string note;
            if (strong) note = "Strong Duality verified (within tolerance).";
            else if (weak) note = "Weak Duality holds but not equal within tolerance.";
            else note = "Weak Duality violated (check feasibility).";

            return new Tuple<bool, bool, string>(weak, strong, note);
        }

        public static double R3(double v)
        {
            return Math.Round(v, 3);
        }
    }
}