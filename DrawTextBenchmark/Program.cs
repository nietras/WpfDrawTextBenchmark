using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;

namespace DrawTextBenchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            //var summary = BenchmarkRunner.Run<DrawText>();

            var drawText = new DrawText();
            drawText.DrawFormattedText_Save();
            drawText.DrawGlyphRun_Save();
            //for (int i = 0; i < 1000000; i++)
            //{
            //    drawText.DrawGlyphRun();
            //}
        }
    }
}
