using System;
using System.Collections.Generic;
using System.Windows.Forms;
using LPR381.Core;

namespace LPR381_WF.Utils
{
    public class ProgressLogger : IIterationLogger
    {
        private readonly List<string> _logLines;
        private readonly RichTextBox _output;
        private readonly ProgressBar _progressBar;
        private int _currentIteration;
        private int _maxIterations;

        public ProgressLogger(RichTextBox output, ProgressBar progressBar, int maxIterations = 10)
        {
            _logLines = new List<string>();
            _output = output;
            _progressBar = progressBar;
            _maxIterations = maxIterations;
            _currentIteration = 0;
        }

        public void Log(string line)
        {
            _logLines.Add(line);
            _output?.AppendText(line + "\n");
            _output?.ScrollToCaret();
            
            if (line.Contains("Iteration"))
            {
                _currentIteration++;
                int progress = Math.Min((_currentIteration * 100) / _maxIterations, 100);
                _progressBar.Value = progress;
                Application.DoEvents();
            }
        }

        public void LogHeader(string title)
        {
            var header = $"\n=== {title} ===";
            _logLines.Add(header);
            _output?.AppendText(header + "\n");
            _output?.ScrollToCaret();
            Application.DoEvents();
        }

        public void LogMatrix(string title, double[,] mat, int round = 3, string[] colNames = null, string[] rowNames = null)
        {
            Log($"\n{title}:");
            int rows = mat.GetLength(0);
            int cols = mat.GetLength(1);

            for (int i = 0; i < rows; i++)
            {
                var rowStr = "";
                for (int j = 0; j < cols; j++)
                {
                    rowStr += $"{Math.Round(mat[i, j], round),8:F3} ";
                }
                Log(rowStr);
            }
        }

        public void LogVector(string title, double[] vec, int round = 3, string[] names = null)
        {
            Log($"\n{title}:");
            for (int i = 0; i < vec.Length; i++)
            {
                var name = names?[i] ?? $"[{i}]";
                Log($"{name}: {Math.Round(vec[i], round):F3}");
            }
        }
    }
}