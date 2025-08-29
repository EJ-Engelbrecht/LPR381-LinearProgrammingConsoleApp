using System;
using System.Windows.Forms;
using LPR381.Core;

namespace LPR381_WF.Utils
{
    public class SimpleLogger : IIterationLogger
    {
        private readonly RichTextBox output;

        public SimpleLogger(RichTextBox outputBox)
        {
            output = outputBox;
        }

        public void Log(string message)
        {
            if (output.InvokeRequired)
            {
                output.Invoke(new Action(() => {
                    output.AppendText(message + "\n");
                    output.ScrollToCaret();
                }));
            }
            else
            {
                output.AppendText(message + "\n");
                output.ScrollToCaret();
            }
            Application.DoEvents();
        }

        public void LogHeader(string title)
        {
            Log($"\n=== {title} ===");
        }

        public void LogMatrix(string title, double[,] mat, int round = 3, string[] colNames = null, string[] rowNames = null)
        {
            Log($"\n{title}:");
            int rows = mat.GetLength(0);
            int cols = mat.GetLength(1);
            
            for (int i = 0; i < rows; i++)
            {
                string row = "";
                for (int j = 0; j < cols; j++)
                {
                    row += $"{mat[i, j].ToString($"F{round}")}\t";
                }
                Log(row);
            }
        }

        public void LogVector(string title, double[] vec, int round = 3, string[] names = null)
        {
            Log($"\n{title}:");
            string line = "";
            for (int i = 0; i < vec.Length; i++)
            {
                line += $"{vec[i].ToString($"F{round}")}\t";
            }
            Log(line);
        }
    }
}