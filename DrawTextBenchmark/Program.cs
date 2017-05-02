using System;
using BenchmarkDotNet.Running;

namespace DrawTextBenchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<DrawTextBenchmark>();

            var drawText = new DrawTextBenchmark();
            drawText.FormattedText();
            drawText.NaiveGlyphRun();
            drawText.FastGlyphRun();
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
