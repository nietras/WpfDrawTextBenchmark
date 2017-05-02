using System;
using BenchmarkDotNet.Running;

namespace DrawTextBenchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<DrawText>();

            var drawText = new DrawText();
            drawText.FormattedText_Save();
            drawText.NaiveGlyphRun_Save();
            drawText.FastGlyphRun_Save();
            //for (int i = 0; i < 1000000; i++)
            //{
            //    drawText.DrawGlyphRun();
            //}
        }
    }
}
