using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LPR381_Solver
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Linear & Integer Programming Solver ===");
            Console.WriteLine("1. Load Model from Input File");
            Console.WriteLine("2. Select Algorithm");
            Console.WriteLine("3. Run Solver");
            Console.WriteLine("4. Export Results");
            Console.WriteLine("5. Run Test Case");
            Console.WriteLine("0. Exit");

            while (true)
            {
                Console.Write("\nSelect an option: ");
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        InputParser.LoadModel("input.txt");
                        break;
                    case "2":
                        AlgorithmSelector.Select();
                        break;
                    case "3":
                        Solver.Run();
                        break;
                    case "4":
                        OutputWriter.Export("output.txt");
                        break;
                    case "5":
                        TestCaseManager.RunEdgeCases();
                        break;
                    case "0":
                        return;
                    default:
                        Console.WriteLine("Invalid option. Try again.");
                        break;
                }
            }
        }
    }
}
