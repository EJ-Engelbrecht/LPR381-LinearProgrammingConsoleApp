using System;
using System.IO;
using System.Windows.Forms;
using LPR381.Core;

namespace LPR381_Solver.Output
{
    public class OutputWriter
    {
        public static void WriteToRichTextBox(RichTextBox rtb, SolveResult result)
        {
            rtb.Clear();
            rtb.AppendText($"Solution Status: {result.Status}\n");
            rtb.AppendText($"Objective Value: {result.Objective}\n");
            rtb.AppendText($"Iterations: {result.Iterations}\n\n");
            
            if (result.X != null && result.X.Length > 0)
            {
                rtb.AppendText("Variables:\n");
                for (int i = 0; i < result.X.Length; i++)
                {
                    rtb.AppendText($"  x{i + 1} = {result.X[i]:F3}\n");
                }
            }
            
            if (result.LogLines != null && result.LogLines.Count > 0)
            {
                rtb.AppendText("\nSolution Log:\n");
                foreach (var line in result.LogLines)
                {
                    rtb.AppendText(line + "\n");
                }
            }
        }
        
        public static void SaveToFile(string filePath, SolveResult result)
        {
            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine($"Solution Status: {result.Status}");
                writer.WriteLine($"Objective Value: {result.Objective}");
                writer.WriteLine($"Iterations: {result.Iterations}");
                writer.WriteLine();
                
                if (result.X != null && result.X.Length > 0)
                {
                    writer.WriteLine("Variables:");
                    for (int i = 0; i < result.X.Length; i++)
                    {
                        writer.WriteLine($"  x{i + 1} = {result.X[i]:F3}");
                    }
                }
                
                if (result.LogLines != null && result.LogLines.Count > 0)
                {
                    writer.WriteLine();
                    writer.WriteLine("Solution Log:");
                    foreach (var line in result.LogLines)
                    {
                        writer.WriteLine(line);
                    }
                }
            }
        }
    }
}
