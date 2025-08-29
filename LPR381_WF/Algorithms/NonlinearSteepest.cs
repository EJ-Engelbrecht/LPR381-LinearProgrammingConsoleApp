using System;
using System.Linq;
using LPR381.Core;

namespace LPR381_Solver.Algorithms
{
    public enum NlSense { Min, Max }

    public sealed class NonlinearProblem
    {
        public Func<double[], double> f { get; set; }
        public Func<double[], double[]> grad { get; set; }
        public Func<double[], double[,]> hess { get; set; }
        public NlSense Sense { get; set; } = NlSense.Min;
        public double[] x0 { get; set; }
        public (double a, double b) HBounds { get; set; }
        public int MaxIter { get; set; } = 50;
        public double GradTol { get; set; } = 1e-6;
        public double HTol { get; set; } = 1e-6;
    }

    public sealed class NonlinearResult
    {
        public string Status { get; set; } = "Unknown";
        public double[] X { get; set; } = Array.Empty<double>();
        public double F { get; set; }
        public int Iterations { get; set; }
    }

    public sealed class NonlinearSteepest
    {
        private readonly IIterationLogger _log;
        
        public NonlinearSteepest(IIterationLogger log) 
        { 
            _log = log; 
        }

        public NonlinearResult Solve(NonlinearProblem p)
        {
            // Header + problem summary
            _log.LogHeader("=== NON-LINEAR OPTIMIZATION ===");
            _log.Log($"Objective: {(p.Sense == NlSense.Min ? "MINIMIZE" : "MAXIMIZE")}");
            _log.Log($"Method: {(p.Sense == NlSense.Min ? "Steepest Descent" : "Steepest Ascent")}");
            _log.Log($"Bounds for h: [a, b] = [{p.HBounds.a}, {p.HBounds.b}]");
            _log.Log("");

            // Analytical derivatives block
            if (p.hess != null)
            {
                var H = p.hess(p.x0);
                var det = Determinant(H);
                
                _log.LogHeader("=== ANALYTICAL DERIVATIVES ===");
                _log.Log($"∇f(x0) = [{string.Join(", ", p.grad(p.x0).Select(val => val.ToString("F6")))}]");
                
                _log.Log("Hessian H(x0) =");
                int n = H.GetLength(0);
                for (int i = 0; i < n; i++)
                {
                    var row = new double[n];
                    for (int j = 0; j < n; j++) row[j] = H[i, j];
                    _log.Log($"[ {string.Join("  ", row.Select(val => val.ToString("F3")))} ]");
                }
                
                _log.Log($"det(H) = {det:F6}");
                
                if (n == 2)
                {
                    if (det > 0 && H[0, 0] > 0)
                        _log.Log("Second-order test: local minimum candidate");
                    else if (det > 0 && H[0, 0] < 0)
                        _log.Log("Second-order test: local maximum candidate");
                    else if (det < 0)
                        _log.Log("Second-order test: saddle candidate");
                }
                _log.Log("");
            }

            // Iteration loop
            var x = (double[])p.x0.Clone();
            int k = 0;
            
            for (k = 1; k <= p.MaxIter; k++)
            {
                var gk = p.grad(x);
                double norm = Math.Sqrt(gk.Sum(gi => gi * gi));
                
                _log.LogHeader($"=== ITERATION {k} ===");
                _log.Log($"Current point: x^{k} = [{string.Join(", ", x.Select(xi => xi.ToString("F6")))}]");
                _log.Log($"f(x^{k}) = {p.f(x):F6}");
                _log.Log($"∇f(x^{k}) = [{string.Join(", ", gk.Select(gi => gi.ToString("F6")))}]");
                _log.Log($"||∇f|| = {norm:F6}");
                
                // Convergence check
                if (norm < p.GradTol)
                {
                    _log.Log("∇f(x)=0 → STATIONARY/OPTIMAL (within tol)");
                    break;
                }
                
                // Direction
                var d = new double[x.Length];
                for (int i = 0; i < x.Length; i++)
                    d[i] = (p.Sense == NlSense.Max) ? gk[i] : -gk[i];
                
                _log.Log($"Build g(h) = f(x^{k} + h * d)");
                _log.Log($"Line-search on h ∈ [a, b] = [{p.HBounds.a}, {p.HBounds.b}]");
                
                // Golden Section Search
                Func<double, double> gFunc = h =>
                {
                    var xTemp = new double[x.Length];
                    for (int i = 0; i < x.Length; i++)
                        xTemp[i] = x[i] + h * d[i];
                    return p.f(xTemp);
                };
                
                var (hStar, fStar, iters) = GoldenSection(gFunc, p.HBounds.a, p.HBounds.b, p.HTol);
                _log.Log($"Golden Section completed in {iters} iterations");
                _log.Log($"h* = {hStar:F6}");
                _log.Log($"g(h*) = {fStar:F6}");
                
                // Update
                var xNext = new double[x.Length];
                for (int i = 0; i < x.Length; i++)
                    xNext[i] = x[i] + hStar * d[i];
                
                _log.Log($"New point: x^{k+1} = [{string.Join(", ", xNext.Select(xi => xi.ToString("F6")))}]");
                
                if (Math.Abs(hStar) < p.HTol)
                {
                    _log.Log("Step too small → stopping");
                    break;
                }
                
                x = xNext;
                _log.Log("");
            }
            
            // Final block
            _log.Log($"Optimal x = [{string.Join(", ", x.Select(xi => xi.ToString("F6")))}]");
            _log.Log($"Optimal f(x) = {p.f(x):F6}");
            _log.Log($"Iterations: {k}");
            
            return new NonlinearResult
            {
                Status = (k <= p.MaxIter) ? "Optimal" : "MaxIter",
                X = x,
                F = p.f(x),
                Iterations = k
            };
        }

        private (double hStar, double fStar, int iters) GoldenSection(
            Func<double, double> g, double a, double b, double tol = 1e-6, int maxIt = 100)
        {
            double phi = (Math.Sqrt(5) - 1) / 2.0; // ~0.618
            double c = b - phi * (b - a);
            double d = a + phi * (b - a);
            double gc = g(c), gd = g(d);
            int k = 0;

            while ((b - a) > tol && k < maxIt)
            {
                k++;
                if (gc <= gd)
                {
                    b = d; d = c; gd = gc;
                    c = b - phi * (b - a);
                    gc = g(c);
                }
                else
                {
                    a = c; c = d; gc = gd;
                    d = a + phi * (b - a);
                    gd = g(d);
                }
            }
            double hStar = 0.5 * (a + b);
            return (hStar, g(hStar), k);
        }

        private static double Determinant(double[,] M)
        {
            int n = M.GetLength(0);
            if (n == 1) return M[0, 0];
            if (n == 2) return M[0, 0] * M[1, 1] - M[0, 1] * M[1, 0];

            // Bare-bones LU (Doolittle) without pivoting
            var A = (double[,])M.Clone();
            double det = 1.0;
            for (int k = 0; k < n; k++)
            {
                if (Math.Abs(A[k, k]) < 1e-12) return 0.0; // singular
                for (int i = k + 1; i < n; i++)
                {
                    double factor = A[i, k] / A[k, k];
                    for (int j = k + 1; j < n; j++) A[i, j] -= factor * A[k, j];
                }
                det *= A[k, k];
            }
            return det;
        }
    }
}