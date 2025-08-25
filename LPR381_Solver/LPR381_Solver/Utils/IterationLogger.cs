using System;
using System.IO;
using System.Linq;
using System.Text;

namespace LPR381.Core
{
    public class FileAndConsoleLogger : IIterationLogger, IDisposable
    {
        private readonly StreamWriter _sw;
        public FileAndConsoleLogger(string path)
        {
            _sw = new StreamWriter(path, false, Encoding.UTF8);
        }

        public void Log(string line)
        {
            Console.WriteLine(line);
            _sw.WriteLine(line);
            _sw.Flush();
        }

        public void LogHeader(string title)
        {
            var bar = new string('=', 10);
            Log($"{bar} {title} {bar}");
        }

        public void LogMatrix(string title, double[,] mat, int round = 3, string[]? colNames = null, string[]? rowNames = null)
        {
            Log(title + ":");
            int m = mat.GetLength(0), n = mat.GetLength(1);
            for (int i = 0; i < m; i++)
            {
                string row = "";
                for (int j = 0; j < n; j++)
                    row += Math.Round(mat[i, j], round).ToString("0.###").PadLeft(8);
                Log(row);
            }
        }

        public void LogVector(string title, double[] vec, int round = 3, string[]? names = null)
        {
            Log(title + ":");
            for (int i = 0; i < vec.Length; i++)
            {
                string name = names != null && i < names.Length ? names[i] : $"v{i+1}";
                Log($"  {name} = {Math.Round(vec[i], round):0.###}");
            }
        }

        public void Dispose() => _sw?.Dispose();
    }
}
