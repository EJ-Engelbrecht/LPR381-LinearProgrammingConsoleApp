# Member 2 – Simplex Core & Cutting Plane

This package implements:
- `PrimalSimplex` (tableau)
- `RevisedSimplex` (basis inverse with price-out)
- `CuttingPlane` (Gomory fractional cuts on LP relaxation)
- `MatrixHelper` (linear algebra + pivot ops)
- `CanonicalForm` (lightweight structure for A, b, c, signs, var types)
- `IterationLogger` interface (plug into your `OutputWriter.cs`)

> All iterations are logged and rounded to **3 decimals** as required.

## How to integrate

1. Add this folder to your Visual Studio solution (e.g., under `Core/Member2`).
2. Implement or connect your existing `InputParser.cs` to produce a `CanonicalForm` instance.
   - The provided `CanonicalForm` is intentionally small and self-contained.
   - If your project already has `LPModel.cs` and `CanonicalFormConverter.cs`, you can map to this `CanonicalForm`.
3. Plug the logger: implement `IIterationLogger` in your `OutputWriter.cs` or create an adapter that writes to console and to file.
4. From your menu:
   ```csharp
   var primal = new PrimalSimplex(logger).Solve(cf);
   var revised = new RevisedSimplex(logger).Solve(cf);
   var ip = new CuttingPlane(new RevisedSimplex(logger), logger).Solve(cf); // for integer models
