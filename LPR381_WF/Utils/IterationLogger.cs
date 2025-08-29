using System;
using System.Collections.Generic;
using System.Windows.Forms;
using LPR381.Core;

namespace LPR381_WF.Utils
{
    public class IterationLogger : IIterationLogger
    {
        private readonly List<string> _logLines;
        private readonly RichTextBox _output;

        public IterationLogger(RichTextBox output = null)
        {
            _logLines = new List<string>();
            _output = output;
        }

        public void Log(string line)
        {
            _logLines.Add(line);
            if (_output != null && !_output.IsDisposed)
            {
                _output.AppendText(line + "\n");
                _output.ScrollToCaret();
            }
            Application.DoEvents();
        }

        public void LogHeader(string title)
        {
            var header = $"\n=== {title} ===";
            _logLines.Add(header);
            if (_output != null && !_output.IsDisposed)
            {
                _output.AppendText(header + "\n");
                _output.ScrollToCaret();
            }
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

        public List<string> GetLogLines()
        {
            return new List<string>(_logLines);
        }

        public void Clear()
        {
            _logLines.Clear();
            if (_output != null && !_output.IsDisposed)
                _output.Clear();
        }
    }
}