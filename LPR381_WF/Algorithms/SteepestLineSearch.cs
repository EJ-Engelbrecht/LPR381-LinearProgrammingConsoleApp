using System;
using LPR381.Core;

namespace LPR381_Solver.Algorithms
{
    /// <summary>
    /// Steepest Ascent/Descent with analytic line search using g(h).
    /// Exactly matches the slide flow & notation.
    /// f(x,y) = a x^2 + b x y + c y^2 + d x + e y + g
    /// </summary>
    public sealed class SteepestLineSearch
    {
        private readonly IIterationLogger log;
        public SteepestLineSearch(IIterationLogger logger) => log = logger;
        
        private static string ToFraction(double value, int maxDenom = 1000)
        {
            if (Math.Abs(value) < 1e-10) return "0";
            if (Math.Abs(value - Math.Round(value)) < 1e-10) return Math.Round(value).ToString();
            
            bool negative = value < 0;
            value = Math.Abs(value);
            
            for (int denom = 2; denom <= maxDenom; denom++)
            {
                int num = (int)Math.Round(value * denom);
                if (Math.Abs(value - (double)num / denom) < 1e-10)
                {
                    int gcd = GCD(num, denom);
                    num /= gcd;
                    denom /= gcd;
                    return (negative ? "-" : "") + (denom == 1 ? num.ToString() : $"{num}/{denom}");
                }
            }
            return value.ToString("0.######");
        }
        
        private static int GCD(int a, int b)
        {
            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }

        public enum Sense { Max, Min }

        public sealed class Quad2
        {
            // f(x,y) = a x^2 + b x y + c y^2 + d x + e y + g
            public double a, b, c, d, e, g;
            public Quad2(double a, double b, double c, double d, double e, double g = 0)
            { this.a = a; this.b = b; this.c = c; this.d = d; this.e = e; this.g = g; }

            public double F(double x, double y) => a*x*x + b*x*y + c*y*y + d*x + e*y + g;

            // ∇f = [fx, fy]
            public (double fx, double fy) Grad(double x, double y)
            {
                // fx = 2ax + by + d
                // fy = bx + 2cy + e
                return (2*a*x + b*y + d, b*x + 2*c*y + e);
            }

            // Hessian
            public (double hxx, double hxy, double hyx, double hyy) Hess()
            {
                // H = [[2a, b], [b, 2c]]
                return (2*a, b, b, 2*c);
            }
        }

        public sealed class Result
        {
            public string Status { get; set; } = "Unknown";
            public double X { get; set; }
            public double Y { get; set; }
            public double F { get; set; }
            public int Iterations { get; set; }
        }

        /// <summary>
        /// Perform steepest ascent/descent with g(h) derivation printed each step.
        /// </summary>
        public Result Solve(
            Quad2 f,
            Sense sense,
            double x0, double y0,
            double tol = 1e-8,
            int maxIter = 50,
            (double a, double b)? hBounds = null // printed only; analytic h* ignores bounds
        )
        {
            var res = new Result();

            // ===== Header like slides =====
            log.LogHeader("=== NON-LINEAR OPTIMIZATION ===");
            log.Log($"Objective: {(sense == Sense.Max ? "MAXIMIZE" : "MINIMIZE")}");
            log.Log($"Using: Steepest {(sense == Sense.Max ? "Ascent" : "Descent")} with g(h) line search");
            if (hBounds.HasValue) log.Log($"Bounds for h: [a,b] = [{hBounds.Value.a}, {hBounds.Value.b}]");

            // ===== Hessian + determinant & classification (like Q3 sheet) =====
            var (hxx, hxy, hyx, hyy) = f.Hess();
            double det = hxx * hyy - hxy * hyx;
            log.Log("\n=== ANALYTICAL DERIVATIVES (at a generic point) ===");
            log.Log($"∇f(x,y) = [ ∂f/∂x , ∂f/∂y ]");
            log.Log("Hessian H =");
            log.Log($"[ {ToFraction(hxx)}   {ToFraction(hxy)} ]");
            log.Log($"[ {ToFraction(hyx)}   {ToFraction(hyy)} ]");
            log.Log($"det(H) = {ToFraction(det)}");

            // Classify by definiteness of H (for quadratics this is global)
            string nature;
            if (det > 0 && hxx > 0) nature = "positive definite → convex → unique global MIN";
            else if (det > 0 && hxx < 0) nature = "negative definite → concave → unique global MAX";
            else if (det < 0) nature = "indefinite → saddle";
            else nature = "semi-definite / degenerate";
            log.Log($"Classification: {nature}");

            // ===== Iterations =====
            double x = x0, y = y0;
            for (int k = 1; k <= maxIter; k++)
            {
                var (fx, fy) = f.Grad(x, y);
                double norm = Math.Sqrt(fx*fx + fy*fy);
                double fk = f.F(x, y);

                log.Log($"\n=== ITERATION {k} ===");
                log.Log($"Current point: (x, y) = ({ToFraction(x)}, {ToFraction(y)})");
                log.Log($"f(x,y) = {ToFraction(fk)}");
                log.Log($"∇f = [{ToFraction(fx)}, {ToFraction(fy)}]");
                log.Log($"‖∇f‖ = {ToFraction(norm)}");

                if (norm <= tol)
                {
                    log.Log("∇f = 0 ⇒ STATIONARY/OPTIMAL (within tol)");
                    res.Status = "Optimal";
                    res.X = x; res.Y = y; res.F = fk; res.Iterations = k;
                    return res;
                }

                // Steepest direction: ascent uses +grad, descent uses -grad
                double dx = (sense == Sense.Max) ? fx : -fx;
                double dy = (sense == Sense.Max) ? fy : -fy;
                string dirName = sense == Sense.Max ? "Ascent" : "Descent";
                log.Log($"{dirName} direction d = [{ToFraction(dx)}, {ToFraction(dy)}]");

                // Build exact g(h) for a quadratic via Taylor (exact for quadratics):
                // g(h) = f(x + h d) = C + B h + A h^2, with
                // A = 0.5 * d^T H d, B = ∇f(x)·d, C = f(x)
                double A = 0.5 * (hxx*dx*dx + (hxy+hyx)*dx*dy + hyy*dy*dy);
                double B = fx*dx + fy*dy;
                double C = fk;

                // g'(h) = 2 A h + B  ⇒  h* = -B / (2A)  (if A ≠ 0)
                if (Math.Abs(A) < 1e-15)
                {
                    // Linear in h – move once with some capped step
                    double hLinear = (B > 0 ? (hBounds?.b ?? 1.0) : (hBounds?.a ?? -1.0));
                    log.Log("g(h) is linear in h (A≈0). Using boundary step.");
                    log.Log($"g(h) = {ToFraction(B)} h + {ToFraction(C)}");
                    log.Log($"Pick h* = {ToFraction(hLinear)}");

                    x += hLinear * dx;
                    y += hLinear * dy;
                    log.Log($"New point: (x, y) = ({ToFraction(x)}, {ToFraction(y)})");
                    continue;
                }

                double hstar = -B / (2*A);
                // (We keep it analytic like slides; you can clamp to [a,b] if you want:)
                if (hBounds.HasValue)
                {
                    if (hstar < hBounds.Value.a) hstar = hBounds.Value.a;
                    if (hstar > hBounds.Value.b) hstar = hBounds.Value.b;
                }

                log.Log("Build g(h) = f(x + h d)");
                log.Log($"g(h) = {ToFraction(A)} h² + {ToFraction(B)} h + {ToFraction(C)}");
                log.Log($"g'(h) = {ToFraction(2*A)} h + {ToFraction(B)} ⇒ h* = -B/(2A) = {ToFraction(hstar)}");
                double gstar = A*hstar*hstar + B*hstar + C;
                log.Log($"g(h*) = {ToFraction(gstar)}");

                // Step
                x += hstar * dx;
                y += hstar * dy;
                log.Log($"New point: (x, y) = ({ToFraction(x)}, {ToFraction(y)})");
            }

            res.Status = "MaxIter";
            res.X = x; res.Y = y; res.F = f.F(x, y);
            res.Iterations = maxIter;
            log.Log("\nStopped: reached maximum iterations.");
            return res;
        }
    }
}