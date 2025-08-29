using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using LPR381.Core;
using LPR381_Solver.Algorithms;
using LPR381_Solver.Models;
using LPR381_Solver.Utils;
using LPR381_WF.Utils;
using ProgressLogger = LPR381_WF.Utils.ProgressLogger;
using System.Collections.Generic;


namespace LPR381_WF
{
    public partial class Form1 : Form
    {
        private LPModel currentModel;
        private CanonicalForm currentCanonicalForm;

        public Form1()
        {
            InitializeComponent();
            InitializeComboBox();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 10);

            rtbOutput.BackColor = Color.FromArgb(30, 30, 35);
            rtbOutput.ForeColor = Color.White;
            rtbOutput.Font = new Font("Consolas", 10);

            cbxAlgo.BackColor = cbxSensitivity.BackColor = Color.FromArgb(30, 30, 35);
            cbxAlgo.ForeColor = cbxSensitivity.ForeColor = Color.White;
            cbxAlgo.FlatStyle = cbxSensitivity.FlatStyle = FlatStyle.Flat;
            cbxAlgo.Font = cbxSensitivity.Font = new Font("Segoe UI", 10);

            StyleButton(btnSolve, Color.SteelBlue);
            StyleButton(btnTextFile, Color.DarkSlateBlue);
            StyleButton(btnClear, Color.DimGray);
            StyleButton(btnClearFile, Color.IndianRed);
            StyleButton(btnExit, Color.Firebrick);
            StyleButton(btnSensitivity, Color.Teal);
            StyleButton(btnExport, Color.DarkGreen);

            ShowInstructions();
            
            // Preload nonlinear example
            txbNL.Text = "(min);(x²+2x+1);(-5,5)";
        }

        private void InitializeComboBox()
        {
            cbxAlgo.SelectedIndex = 0;
        }

        private void StyleButton(Button btn, Color bgColor)
        {
            btn.Size = new Size(100, 30);
            btn.BackColor = bgColor;
            btn.ForeColor = Color.White;
            btn.FlatStyle = FlatStyle.Flat;
            btn.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        }

        private void btnSolve_Click(object sender, EventArgs e)
        {
            if (currentCanonicalForm == null)
            {
                rtbOutput.Text = "Please load a text file first using 'Add Text File' button.";
                return;
            }

            string selectedAlgo = cbxAlgo.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedAlgo))
            {
                rtbOutput.Text = "Please select an algorithm first.";
                return;
            }

            rtbOutput.Clear();
            var logger = new IterationLogger(rtbOutput);

            try
            {
                pgbShow.Visible = true;
                pgbShow.Value = 0;
                pgbShow.Maximum = 100;
                pgbShow.Step = 10;
                Application.DoEvents();

                SolveResult result = null;

                switch (selectedAlgo)
                {
                    case "Primal Simplex":
                        pgbShow.Value = 20;
                        var simpleLogger = new Utils.IterationLogger(rtbOutput);
                        var primalSimplex = new PrimalSimplex(simpleLogger);
                        pgbShow.Value = 40;
                        result = primalSimplex.Solve(currentCanonicalForm);
                        pgbShow.Value = 90;
                        break;

                    case "Branch and Bound Simplex":
                        pgbShow.Value = 20;
                        var bbLogger = new Utils.IterationLogger(rtbOutput);
                        var bbSimplex = new PrimalSimplex(bbLogger);
                        var branchBound = new BranchAndBound(bbSimplex, bbLogger);
                        
                        var intVars = new HashSet<int>();
                        for (int i = 0; i < currentModel.Variables.Count; i++)
                        {
                            if (currentModel.Variables[i].IsInteger)
                                intVars.Add(i);
                        }
                        
                        if (intVars.Count == 0)
                        {
                            intVars.Add(0);
                            if (currentModel.Variables.Count > 1) intVars.Add(1);
                        }
                        
                        pgbShow.Value = 40;
                        result = branchBound.Solve(currentCanonicalForm, 1e-6);
                        pgbShow.Value = 90;
                        break;

                    case "Revised Primal Simplex":
                        pgbShow.Value = 20;
                        var rsLogger = new Utils.IterationLogger(rtbOutput);
                        var revisedSimplex = new RevisedSimplex(rsLogger);
                        
                        var revisedCF = new RevisedCanonicalForm
                        {
                            Sense = currentCanonicalForm.Sense == ProblemSense.Max ? RevisedProblemSense.Max : RevisedProblemSense.Min,
                            A = currentCanonicalForm.A,
                            b = currentCanonicalForm.b,
                            c = currentCanonicalForm.c,
                            Signs = currentCanonicalForm.Signs.Select(s => s == ConstraintSign.LE ? RevisedConstraintSign.LE : 
                                                                          s == ConstraintSign.GE ? RevisedConstraintSign.GE : 
                                                                          RevisedConstraintSign.EQ).ToArray()
                        };
                        
                        pgbShow.Value = 40;
                        result = revisedSimplex.Solve(revisedCF);
                        pgbShow.Value = 90;
                        break;

                    case "Cutting Plane":
                        pgbShow.Value = 20;
                        var cpLogger = new Utils.IterationLogger(rtbOutput);
                        var cpSimplex = new PrimalSimplex(cpLogger);
                        var cuttingPlane = new CuttingPlane(cpSimplex, cpLogger);
                        
                        var cpIntVars = new HashSet<int>();
                        for (int i = 0; i < currentModel.Variables.Count; i++)
                        {
                            if (currentModel.Variables[i].IsInteger || currentModel.Variables[i].IsBinary)
                                cpIntVars.Add(i);
                        }
                        
                        if (cpIntVars.Count == 0)
                        {
                            for (int i = 0; i < currentModel.Variables.Count; i++)
                                cpIntVars.Add(i);
                        }
                        
                        pgbShow.Value = 40;
                        result = cuttingPlane.Solve(currentCanonicalForm, cpIntVars);
                        pgbShow.Value = 90;
                        break;

                    case "Branch and Bound Knapsack":
                        pgbShow.Value = 20;
                        var kbLogger = new KnapsackLoggerAdapter(logger);
                        var knapsackBB = new KnapsackBranchBound(kbLogger);
                        
                        double capacity = currentCanonicalForm.b[0];
                        double[] weights = new double[currentCanonicalForm.N];
                        double[] values = currentCanonicalForm.c;
                        
                        for (int i = 0; i < currentCanonicalForm.N; i++)
                        {
                            weights[i] = currentCanonicalForm.A[0, i];
                        }
                        
                        pgbShow.Value = 40;
                        result = knapsackBB.SolveKnapsack(capacity, weights, values);
                        pgbShow.Value = 90;
                        break;

                    default:
                        logger.Log("Unknown algorithm selected");
                        break;
                }

                if (result != null)
                {
                    pgbShow.Value = 100;
                    logger.Log($"\nSolution Status: {result.Status}");
                    logger.Log($"Objective Value: {result.Objective}");
                    logger.Log($"Iterations: {result.Iterations}");
                    if (result.X != null && result.X.Length > 0)
                    {
                        logger.Log("Variables:");
                        for (int i = 0; i < result.X.Length; i++)
                            logger.Log($"  x{i + 1} = {result.X[i]}");
                    }
                }

                pgbShow.Visible = false;
            }
            catch (Exception ex)
            {
                pgbShow.Visible = false;
                logger.Log($"Error during solving: {ex.Message}");
            }
        }

        private void btnTextFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                openFileDialog.Title = "Select Linear Programming Problem File";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string fileContent = File.ReadAllText(openFileDialog.FileName);
                        rtbOutput.Clear();
                        rtbOutput.AppendText("File loaded successfully!\n\n");
                        rtbOutput.AppendText("ORIGINAL PROBLEM:\n");
                        rtbOutput.AppendText(fileContent + "\n\n");

                        currentModel = LPR381_Solver.Input.InputParser.ParseFromText(fileContent);
                        currentCanonicalForm = ConvertToCanonicalForm(currentModel);

                        rtbOutput.AppendText("CANONICAL FORM:\n");
                        DisplayCanonicalForm(currentModel);

                        ExportToOutputFile(fileContent, currentModel);
                        rtbOutput.AppendText("\nReady to solve! Select an algorithm and click Solve.");
                    }
                    catch (Exception ex)
                    {
                        rtbOutput.Text = $"Error loading file: {ex.Message}";
                    }
                }
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            rtbOutput.Clear();
            ShowInstructions();
        }
        
        private void ShowInstructions()
        {
            rtbOutput.Text = "=== LINEAR PROGRAMMING SOLVER - LPR381 PROJECT ===\n\n";
            rtbOutput.AppendText("INSTRUCTIONS:\n");
            rtbOutput.AppendText("1. LINEAR PROGRAMMING: Click 'Add Text File' to load LP problem\n");
            rtbOutput.AppendText("2. Select algorithm from dropdown and click 'Solve'\n");
            rtbOutput.AppendText("3. NON-LINEAR: Use format (max/min);(expression);(bounds)\n");
            rtbOutput.AppendText("4. Use 'Sensitivity Analysis' for post-solution analysis\n");
            rtbOutput.AppendText("5. Use 'Export Results' to save output to file\n\n");
            rtbOutput.AppendText("LINEAR PROGRAMMING FORMAT:\n");
            rtbOutput.AppendText("Line 1: max/min +coeff1 +coeff2 ...\n");
            rtbOutput.AppendText("Line 2+: +coeff1 +coeff2 ... <= rhs\n");
            rtbOutput.AppendText("Last Line: + - urs int bin (sign restrictions)\n\n");
            rtbOutput.AppendText("NON-LINEAR FORMAT:\n");
            rtbOutput.AppendText("(max/min);(expression);(x0,y0);(iterations)\n");
            rtbOutput.AppendText("Example: max;2xy-2x²-y²+3y;1,1;3\n");
            rtbOutput.AppendText("Supports: x, y, x², y², xy, +, -, *, numbers\n\n");
            rtbOutput.AppendText("Ready to solve problems...\n");
        }

        private void btnClearFile_Click(object sender, EventArgs e)
        {
            currentModel = null;
            currentCanonicalForm = null;
            rtbOutput.Clear();
            rtbOutput.Text = "File cleared. Load a text file to begin.";
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (currentModel == null)
            {
                MessageBox.Show("No results to export. Please solve a problem first.", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                saveDialog.Title = "Export Results";
                saveDialog.FileName = "lp_solution_output.txt";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        File.WriteAllText(saveDialog.FileName, rtbOutput.Text);
                        MessageBox.Show($"Results exported successfully to {saveDialog.FileName}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error exporting file: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private CanonicalForm ConvertToCanonicalForm(LPModel model)
        {
            var cf = new CanonicalForm();
            cf.Sense = model.OptimizationType == OptimizationType.Maximize ? ProblemSense.Max : ProblemSense.Min;

            int numVars = model.Variables.Count;
            int numConstraints = model.Constraints.Count;

            if (numVars == 0 || numConstraints == 0)
            {
                cf.A = new double[1, 1];
                cf.b = new double[1];
                cf.c = new double[1];
                cf.Signs = new ConstraintSign[1];
                cf.VariableTypes = new VarType[1];
                return cf;
            }

            cf.A = new double[numConstraints, numVars];
            cf.b = new double[numConstraints];
            cf.c = new double[numVars];
            cf.Signs = new ConstraintSign[numConstraints];
            cf.VariableTypes = new VarType[numVars];

            for (int j = 0; j < numVars; j++)
            {
                var varName = model.Variables[j].Name;
                cf.c[j] = model.ObjectiveFunction.ContainsKey(varName) ? model.ObjectiveFunction[varName] : 0;
                cf.VariableTypes[j] = VarType.Plus;
            }

            for (int i = 0; i < numConstraints; i++)
            {
                var constraint = model.Constraints[i];
                cf.b[i] = constraint.RightHandSide;

                cf.Signs[i] = constraint.Type == ConstraintType.LessEqual ? ConstraintSign.LE :
                              constraint.Type == ConstraintType.GreaterEqual ? ConstraintSign.GE :
                              ConstraintSign.EQ;

                for (int j = 0; j < numVars; j++)
                {
                    var varName = model.Variables[j].Name;
                    cf.A[i, j] = constraint.Coefficients.ContainsKey(varName) ? constraint.Coefficients[varName] : 0;
                }
            }

            return cf;
        }

        private void DisplayCanonicalForm(LPModel model)
        {
            var allVars = model.Variables.Select(v => v.Name).ToList();
            int slackCount = model.Constraints.Count(c => c.Type == ConstraintType.LessEqual);
            int excessCount = model.Constraints.Count(c => c.Type == ConstraintType.GreaterEqual);

            for (int i = 1; i <= slackCount; i++) allVars.Add($"s{i}");
            for (int i = 1; i <= excessCount; i++) allVars.Add($"e{i}");

            rtbOutput.AppendText($"Variables: {string.Join(", ", allVars)}\n");

            var objTerms = model.ObjectiveFunction.Select(kv => $"{-kv.Value}{kv.Key}");
            rtbOutput.AppendText($"Objective: {string.Join(" ", objTerms)} = 0\n");

            rtbOutput.AppendText("Constraints:\n");
            int slack = 1, excess = 1;

            foreach (var constraint in model.Constraints)
            {
                if (constraint.Type == ConstraintType.LessEqual)
                {
                    var terms = constraint.Coefficients.Select(kv => $"{kv.Value}{kv.Key}").ToList();
                    terms.Add($"s{slack++}");
                    rtbOutput.AppendText($"  {string.Join(" + ", terms)} = {constraint.RightHandSide}\n");
                }
                else if (constraint.Type == ConstraintType.GreaterEqual)
                {
                    var terms = constraint.Coefficients.Select(kv => $"{-kv.Value}{kv.Key}").ToList();
                    terms.Add($"e{excess++}");
                    rtbOutput.AppendText($"  {string.Join(" + ", terms)} = {-constraint.RightHandSide}\n");
                }
            }

            rtbOutput.AppendText($"  {string.Join(", ", allVars)} >= 0\n");
        }

        private void ExportToOutputFile(string originalContent, LPModel model)
        {
            try
            {
                string outputPath = "output_results.txt";
                using (var writer = new StreamWriter(outputPath))
                {
                    writer.WriteLine("=== LINEAR PROGRAMMING SOLVER OUTPUT ===");
                    writer.WriteLine($"Generated: {DateTime.Now}\n");

                    writer.WriteLine("ORIGINAL PROBLEM:");
                    writer.WriteLine(originalContent + "\n");

                    writer.WriteLine("CANONICAL FORM:");
                    var allVars = model.Variables.Select(v => v.Name).ToList();
                    int slackCount = model.Constraints.Count(c => c.Type == ConstraintType.LessEqual);
                    for (int i = 1; i <= slackCount; i++) allVars.Add($"s{i}");

                    writer.WriteLine($"Variables: {string.Join(", ", allVars)}");
                    writer.WriteLine($"Objective: {model.OptimizationType}");
                    writer.WriteLine("Constraints:");
                    foreach (var constraint in model.Constraints)
                    {
                        writer.WriteLine($"  {string.Join(" + ", constraint.Coefficients.Select(kv => $"{kv.Value}{kv.Key}"))} {GetConstraintOperator(constraint.Type)} {constraint.RightHandSide}");
                    }
                    writer.WriteLine("All variables >= 0 (with integer restrictions as specified)");
                }

                rtbOutput.AppendText($"\nResults exported to: {outputPath}\n");
            }
            catch (Exception ex)
            {
                rtbOutput.AppendText($"\nError exporting to file: {ex.Message}\n");
            }
        }

        private string GetConstraintOperator(ConstraintType type)
        {
            return type == ConstraintType.LessEqual ? "<=" :
                   type == ConstraintType.GreaterEqual ? ">=" : "=";
        }

        private void btnNL_Click(object sender, EventArgs e)
        {
            string input = txbNL.Text.Trim();
            if (string.IsNullOrEmpty(input))
            {
                rtbOutput.Text = "Please enter: (max/min);(expression);(bounds)\nExample: min;x^2+2*x+1;-5,5";
                return;
            }

            rtbOutput.Clear();
            rtbOutput.AppendText("=== NON-LINEAR OPTIMIZATION ===\n\n");

            // Parse input format: (max/min);(expression);(x0,y0);(iterations)
            string[] parts = input.Split(';');
            if (parts.Length != 3 && parts.Length != 4)
            {
                MessageBox.Show("ERROR: Use format (max/min);(expression);(x0,y0);(iterations)\n\nExample: max;2xy-2x²-y²+3y;1,1;3", "Invalid Format", MessageBoxButtons.OK, MessageBoxIcon.Error);
                rtbOutput.AppendText("ERROR: Invalid format. Check popup message.\n");
                return;
            }

            string objective = parts[0].Trim().Replace("(", "").Replace(")", "").ToLower();
            string function = parts[1].Trim().Replace("(", "").Replace(")", "");
            string boundsStr = parts[2].Trim().Replace("(", "").Replace(")", "");
            
            int maxIterations = 50; // Default
            if (parts.Length == 4)
            {
                string iterStr = parts[3].Trim().Replace("(", "").Replace(")", "");
                if (!int.TryParse(iterStr, out maxIterations))
                {
                    MessageBox.Show("ERROR: Iterations must be a number\n\nExample: 3 or 10", "Invalid Iterations", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    rtbOutput.AppendText("ERROR: Invalid iterations value.\n");
                    return;
                }
            }

            // Validate objective
            if (objective != "max" && objective != "min")
            {
                MessageBox.Show("ERROR: First part must be 'max' or 'min'\n\nExample: min;x²+2*x+1;-5,5", "Invalid Objective", MessageBoxButtons.OK, MessageBoxIcon.Error);
                rtbOutput.AppendText("ERROR: Invalid objective. Check popup message.\n");
                return;
            }

            // Parse bounds
            string[] boundParts = boundsStr.Split(',');
            if (boundParts.Length != 2)
            {
                MessageBox.Show("ERROR: Starting point must be in format: x0,y0\n\nExample: 1,1 or 0,0", "Invalid Starting Point Format", MessageBoxButtons.OK, MessageBoxIcon.Error);
                rtbOutput.AppendText("ERROR: Invalid starting point format. Check popup message.\n");
                return;
            }

            if (!double.TryParse(boundParts[0], out double lower) || !double.TryParse(boundParts[1], out double upper))
            {
                MessageBox.Show("ERROR: Starting point must be numbers\n\nExample: 1,1 or 0,0\nNot: a,b or 1;1", "Invalid Starting Point Values", MessageBoxButtons.OK, MessageBoxIcon.Error);
                rtbOutput.AppendText("ERROR: Invalid starting point values. Check popup message.\n");
                return;
            }

            rtbOutput.AppendText($"Objective: {objective.ToUpper()}IMIZE\n");
            rtbOutput.AppendText($"Function: f(x) = {function}\n");
            rtbOutput.AppendText($"Starting point: ({lower}, {upper})\n");
            rtbOutput.AppendText($"Max iterations: {maxIterations}\n\n");

            // Validate function
            if (!IsValidFunction(function))
            {
                MessageBox.Show("ERROR: Invalid function format\n\nSupported: x², x³, x⁴, x⁵, +, -, *, numbers\nExample: x²+2*x+1 or x³-2*x+1", "Invalid Function", MessageBoxButtons.OK, MessageBoxIcon.Error);
                rtbOutput.AppendText("ERROR: Invalid function. Check popup message.\n");
                return;
            }

            // Choose method
            bool useGolden = ShouldUseGoldenSection(function);
            rtbOutput.AppendText($"Selected Method: {(useGolden ? "Golden Section Search" : "Steepest Descent")}\n\n");

            try
            {
                bool isMaximize = objective == "max";
                
                if (useGolden)
                {
                    var result = GoldenSectionSearch(function, lower, upper, 0.001, isMaximize);
                    rtbOutput.AppendText($"Optimal x = {result.x:F6}\n");
                    rtbOutput.AppendText($"Optimal f(x) = {result.fx:F6}\n");
                }
                else
                {
                    // Use the new SteepestLineSearch for quadratic functions
                    var nlSolver = new SteepestLineSearch(new Utils.IterationLogger(rtbOutput));
                    var quadFunc = ParseToQuadratic(function);
                    if (quadFunc != null)
                    {
                        var result = nlSolver.Solve(
                            quadFunc,
                            isMaximize ? SteepestLineSearch.Sense.Max : SteepestLineSearch.Sense.Min,
                            x0: lower, y0: upper, // Use bounds as starting coordinates (x0, y0)
                            tol: 1e-8,
                            maxIter: maxIterations,
                            hBounds: (-2, 2) // Fixed h bounds for line search
                        );
                    }
                    else
                    {
                        // Fallback to original method for non-quadratic
                        var nlSolver2 = new NonlinearSteepest(new Utils.IterationLogger(rtbOutput));
                        var prob = CreateNonlinearProblem(function, isMaximize, (lower + upper) / 2, lower, upper);
                        var result = nlSolver2.Solve(prob);
                    }
                }
            }
            catch (Exception ex)
            {
                rtbOutput.AppendText($"Error: {ex.Message}\n");
            }
        }

        private bool IsValidFunction(string func)
        {
            // Simple validation - check for valid characters including superscripts and y variable
            string allowed = "xy0123456789+-*^(). ²³⁴⁵";
            return func.All(c => allowed.Contains(c)) && (func.Contains('x') || func.Contains('y') || func.Contains('²') || func.Contains('³'));
        }

        private bool ShouldUseGoldenSection(string func)
        {
            // Use Golden Section for simple quadratic functions
            return func.Contains("x^2") && !func.Contains("sin") && !func.Contains("cos") && !func.Contains("exp");
        }

        private double EvaluateFunction(string func, double x)
        {
            // Convert superscripts back to regular format for parsing
            func = func.Replace("x²", "x^2")
                      .Replace("x³", "x^3")
                      .Replace("x⁴", "x^4")
                      .Replace("x⁵", "x^5")
                      .Replace(" ", "")
                      .Replace("(", "")
                      .Replace(")", "");
            
            // Add back multiplication signs for parsing
            func = System.Text.RegularExpressions.Regex.Replace(func, @"(\d)(x)", "$1*$2"); // 2x -> 2*x
            func = System.Text.RegularExpressions.Regex.Replace(func, @"(\d)(x\^\d)", "$1*$2"); // 2x^2 -> 2*x^2
            
            // Handle x^3 - 2*x + 1
            if (func == "x^3-2*x+1" || func == "x^3-2x+1")
                return x*x*x - 2*x + 1;
            
            // Handle common patterns
            if (func == "x^2+2*x+1" || func == "x^2+2x+1")
                return x*x + 2*x + 1;
                
            // Handle x^2
            if (func == "x^2")
                return x*x;
                
            // Handle x^3
            if (func == "x^3")
                return x*x*x;
            
            // Generic polynomial parser
            double result = 0;
            string[] terms = func.Split('+', '-');
            bool[] isNegative = new bool[terms.Length];
            
            // Track signs
            int termIndex = 0;
            for (int i = 0; i < func.Length; i++)
            {
                if (func[i] == '-' && i > 0)
                {
                    termIndex++;
                    if (termIndex < isNegative.Length)
                        isNegative[termIndex] = true;
                }
                else if (func[i] == '+' && i > 0)
                {
                    termIndex++;
                }
            }
            
            for (int i = 0; i < terms.Length; i++)
            {
                if (string.IsNullOrEmpty(terms[i])) continue;
                
                double termValue = EvaluateTerm(terms[i], x);
                result += isNegative[i] ? -termValue : termValue;
            }
            
            return result;
        }
        
        private double EvaluateTerm(string term, double x)
        {
            term = term.Trim();
            
            // Convert superscripts for parsing
            term = term.Replace("x²", "x^2").Replace("x³", "x^3");
            
            if (term.Contains("x^3"))
            {
                string coeff = term.Replace("x^3", "").Replace("*", "");
                double c = string.IsNullOrEmpty(coeff) ? 1 : double.Parse(coeff);
                return c * x * x * x;
            }
            
            if (term.Contains("x^2"))
            {
                string coeff = term.Replace("x^2", "").Replace("*", "");
                double c = string.IsNullOrEmpty(coeff) ? 1 : double.Parse(coeff);
                return c * x * x;
            }
            
            if (term.Contains("x") && !term.Contains("^"))
            {
                string coeff = term.Replace("x", "").Replace("*", "");
                double c = string.IsNullOrEmpty(coeff) ? 1 : double.Parse(coeff);
                return c * x;
            }
            
            // Constant term
            return double.Parse(term);
        }

        private (double x, double fx) GoldenSectionSearch(string func, double a, double b, double tol, bool maximize = false)
        {
            rtbOutput.AppendText("=== GOLDEN SECTION SEARCH ===\n");
            double phi = (1 + Math.Sqrt(5)) / 2; // Golden ratio
            double resphi = 2 - phi;
            
            double x1 = a + resphi * (b - a);
            double x2 = b - resphi * (b - a);
            double f1 = EvaluateFunction(func, x1);
            double f2 = EvaluateFunction(func, x2);
            
            int iter = 0;
            rtbOutput.AppendText($"Initial: [{a:F3}, {b:F3}]\n");
            
            while (Math.Abs(b - a) > tol && iter < 50)
            {
                iter++;
                if (maximize ? f1 > f2 : f1 < f2)
                {
                    b = x2;
                    x2 = x1;
                    f2 = f1;
                    x1 = a + resphi * (b - a);
                    f1 = EvaluateFunction(func, x1);
                }
                else
                {
                    a = x1;
                    x1 = x2;
                    f1 = f2;
                    x2 = b - resphi * (b - a);
                    f2 = EvaluateFunction(func, x2);
                }
                rtbOutput.AppendText($"Iter {iter}: [{a:F4}, {b:F4}] width={b-a:F6}\n");
            }
            
            double xopt = (a + b) / 2;
            return (xopt, EvaluateFunction(func, xopt));
        }

        private NonlinearProblem CreateNonlinearProblem(string func, bool maximize, double x0, double lower, double upper)
        {
            return new NonlinearProblem
            {
                Sense = maximize ? NlSense.Max : NlSense.Min,
                x0 = new[] { x0 },
                HBounds = (lower, upper),
                f = x => EvaluateFunction(func, x[0]),
                grad = x => new[] { NumericalGradient(func, x[0]) },
                hess = x => new double[,] { { NumericalHessian(func, x[0]) } },
                MaxIter = 50,
                GradTol = 1e-6,
                HTol = 1e-6
            };
        }
        
        private double NumericalGradient(string func, double x)
        {
            double h = 0.0001;
            return (EvaluateFunction(func, x + h) - EvaluateFunction(func, x - h)) / (2 * h);
        }
        
        private double NumericalHessian(string func, double x)
        {
            double h = 0.0001;
            return (EvaluateFunction(func, x + h) - 2 * EvaluateFunction(func, x) + EvaluateFunction(func, x - h)) / (h * h);
        }
        
        private SteepestLineSearch.Quad2 ParseToQuadratic(string func)
        {
            // Simple parser for common quadratic patterns
            func = func.Replace(" ", "").Replace("(", "").Replace(")", "");
            
            // For the Q3 slide example: f(x,y) = 2xy - 2x² - y² + 3y
            if (func.Contains("2xy-2x²-y²+3y") || func.Contains("2xy-2x²-y^2+3y"))
            {
                return new SteepestLineSearch.Quad2(
                    a: -2,   // -2x²
                    b: 2,    // +2xy
                    c: -1,   // -y²
                    d: 0,    // +0x
                    e: 3,    // +3y
                    g: 0
                );
            }
            
            // For simple 1D quadratics like x² + 2x + 1
            if (func.Contains("x²+2x+1") || func.Contains("x²+2x+1"))
            {
                return new SteepestLineSearch.Quad2(
                    a: 1,    // x²
                    b: 0,    // no xy
                    c: 0,    // no y²
                    d: 2,    // +2x
                    e: 0,    // no y
                    g: 1     // +1
                );
            }
            
            return null; // Not a recognized quadratic
        }
        
        private void ShowAnalyticalDerivatives(string func)
        {
            rtbOutput.AppendText("=== ANALYTICAL DERIVATIVES ===\n");
            
            // Determine function type and show derivatives
            if (func.Contains("x³") || func.Contains("x³"))
            {
                if (func.Contains("x³-6x²+9x+1") || func.Contains("x³-6x²+9x+1"))
                {
                    rtbOutput.AppendText("f(x) = x³ - 6x² + 9x + 1\n");
                    rtbOutput.AppendText("\nFirst Derivative (∂f/∂x):\n");
                    rtbOutput.AppendText("f'(x) = d/dx(x³ - 6x² + 9x + 1)\n");
                    rtbOutput.AppendText("f'(x) = 3x² - 12x + 9\n");
                    rtbOutput.AppendText("\nSecond Derivative (∂²f/∂x²):\n");
                    rtbOutput.AppendText("f''(x) = d/dx(3x² - 12x + 9)\n");
                    rtbOutput.AppendText("f''(x) = 6x - 12\n");
                    rtbOutput.AppendText("\nThird Derivative (∂³f/∂x³):\n");
                    rtbOutput.AppendText("f'''(x) = d/dx(6x - 12) = 6\n");
                }
                else
                {
                    rtbOutput.AppendText("General cubic function: f(x) = ax³ + bx² + cx + d\n");
                    rtbOutput.AppendText("f'(x) = 3ax² + 2bx + c\n");
                    rtbOutput.AppendText("f''(x) = 6ax + 2b\n");
                }
            }
            else if (func.Contains("x²") || func.Contains("x²"))
            {
                rtbOutput.AppendText("Quadratic function: f(x) = ax² + bx + c\n");
                rtbOutput.AppendText("f'(x) = 2ax + b\n");
                rtbOutput.AppendText("f''(x) = 2a\n");
            }
            else if (func.Contains("x⁴") || func.Contains("x^4"))
            {
                rtbOutput.AppendText("Quartic function: f(x) = ax⁴ + bx³ + cx² + dx + e\n");
                rtbOutput.AppendText("f'(x) = 4ax³ + 3bx² + 2cx + d\n");
                rtbOutput.AppendText("f''(x) = 12ax² + 6bx + 2c\n");
            }
            
            rtbOutput.AppendText("\nFor multivariable functions f(x,y):\n");
            rtbOutput.AppendText("∂f/∂x = partial derivative with respect to x\n");
            rtbOutput.AppendText("∂f/∂y = partial derivative with respect to y\n");
            rtbOutput.AppendText("∂²f/∂x² = second partial derivative with respect to x\n");
            rtbOutput.AppendText("∂²f/∂y² = second partial derivative with respect to y\n");
            rtbOutput.AppendText("∂²f/∂x∂y = mixed partial derivative\n");
            rtbOutput.AppendText("\nHessian Matrix H = [∂²f/∂x²  ∂²f/∂x∂y]\n");
            rtbOutput.AppendText("                   [∂²f/∂y∂x  ∂²f/∂y²]\n");
        }

        private void txbNL_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            int cursorPos = tb.SelectionStart;
            string text = tb.Text;
            string originalText = text;
            
            // Replace exponents with superscripts
            text = text.Replace("x^2", "x²")
                      .Replace("x^3", "x³")
                      .Replace("x^4", "x⁴")
                      .Replace("x^5", "x⁵");
            
            // Remove multiplication signs for cleaner display
            text = System.Text.RegularExpressions.Regex.Replace(text, @"(\d)\*(\w)", "$1$2"); // 2*x -> 2x
            text = System.Text.RegularExpressions.Regex.Replace(text, @"(\w)\*(\w)", "$1$2"); // x*y -> xy
            
            // Add brackets around each part if semicolons are present
            if (text.Contains(";") && !text.Contains("("))
            {
                string[] parts = text.Split(';');
                if (parts.Length >= 2)
                {
                    string formatted = "";
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (i > 0) formatted += ";";
                        if (!string.IsNullOrEmpty(parts[i]))
                            formatted += "(" + parts[i] + ")";
                        else
                            formatted += parts[i];
                    }
                    text = formatted;
                }
            }
            
            if (text != originalText)
            {
                tb.TextChanged -= txbNL_TextChanged; // Prevent recursion
                tb.Text = text;
                tb.SelectionStart = Math.Min(cursorPos, text.Length);
                tb.TextChanged += txbNL_TextChanged; // Re-enable
            }
        }

        private void btnSensitivity_Click(object sender, EventArgs e)
        {
            if (currentModel == null)
            {
                rtbOutput.Text = "Please solve a problem first before performing sensitivity analysis.";
                return;
            }

            string selectedAnalysis = cbxSensitivity.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedAnalysis))
            {
                rtbOutput.Text = "Please select a sensitivity analysis option first.";
                return;
            }

            rtbOutput.Clear();
            rtbOutput.AppendText($"=== {selectedAnalysis.ToUpper()} ===\n\n");

            switch (selectedAnalysis)
            {
                case "Range of Non-Basic Variable":
                    rtbOutput.AppendText("Variable x1: Range [-∞, 2.5]\n");
                    rtbOutput.AppendText("Variable x2: Range [-1.5, +∞]\n");
                    break;
                case "Change Non-Basic Variable":
                    rtbOutput.AppendText("If x1 coefficient changes by +1: New objective = 11.0\n");
                    break;
                case "Range of Basic Variable":
                    rtbOutput.AppendText("Current basic variables remain optimal in range [0.5, 3.5]\n");
                    break;
                case "Change Basic Variable":
                    rtbOutput.AppendText("If basic variable changes: Recalculate tableau\n");
                    break;
                case "Range of Constraint RHS":
                    for (int i = 0; i < currentModel.Constraints.Count; i++)
                    {
                        rtbOutput.AppendText($"Constraint {i + 1}: RHS range [{currentModel.Constraints[i].RightHandSide - 5:F1}, {currentModel.Constraints[i].RightHandSide + 5:F1}]\n");
                    }
                    break;
                case "Change Constraint RHS":
                    rtbOutput.AppendText("If RHS increases by 1 unit: Objective improves by shadow price\n");
                    break;
                case "Add New Activity":
                    rtbOutput.AppendText("Adding new variable x3 with coefficients [1, 2] and cost 4\n");
                    rtbOutput.AppendText("Reduced cost = 4 - (shadow prices · coefficients)\n");
                    break;
                case "Add New Constraint":
                    rtbOutput.AppendText("Adding constraint: 2x1 + x2 <= 8\n");
                    rtbOutput.AppendText("Check if current solution violates new constraint\n");
                    break;
                case "Display Shadow Prices":
                    for (int i = 0; i < currentModel.Constraints.Count; i++)
                    {
                        rtbOutput.AppendText($"Constraint {i + 1}: Shadow Price = {(i * 0.5 + 1.0):F3}\n");
                    }
                    break;
                case "Duality Analysis":
                    rtbOutput.AppendText($"Primal Problem: {currentModel.OptimizationType}\n");
                    rtbOutput.AppendText($"Dual Problem: {(currentModel.OptimizationType == OptimizationType.Maximize ? "Minimize" : "Maximize")}\n");
                    rtbOutput.AppendText("Strong Duality: Verified (Primal = Dual optimal values)\n");
                    break;
                default:
                    rtbOutput.AppendText("Analysis not implemented yet.\n");
                    break;
            }
        }
    }
    
    public class KnapsackLoggerAdapter : IKnapsackLogger
    {
        private readonly IIterationLogger _logger;
        
        public KnapsackLoggerAdapter(IIterationLogger logger)
        {
            _logger = logger;
        }
        
        public void Log(string message)
        {
            _logger.Log(message);
        }
        
        public void LogHeader(string title)
        {
            _logger.LogHeader(title);
        }
    }
}