using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;

namespace DrawTextBenchmark
{
    [Config(typeof(FastAndDirtyConfig))]
    public class DrawText
    {
        public class FastAndDirtyConfig : ManualConfig
        {
            public FastAndDirtyConfig()
            {
                Add(new MemoryDiagnoser());
                Add(Job.Default
                    .WithLaunchCount(1)     // benchmark process will be launched only once
                    .WithIterationTime(TimeInterval.FromMilliseconds(150)) // 100ms per iteration
                    .WithWarmupCount(3)
                    .WithTargetCount(10)
                );
            }
        }

        const float DPI = 96;
        const int Width = 1024;
        const int Height = 32;
        const string Text0 = "";
        const string Text1 = "T";
        const string Text2 = "Th";
        const string Text4 = "This";
        const string Text8 = "This-is.";
        const string TextA = "This is a test text with numbers 0123456789 and symbols .,&%#";
        const string TextB = "This is a test text with numbers 0123456789 and symbols .,&%#This is a test text with numbers 0123456789 and symbols .,&%#";
        const string TextC = "This is a test text with numbers 0123456789 and symbols .,&%#This is a test text with numbers 0123456789 and symbols .,&%#This is a test text with numbers 0123456789 and symbols .,&%#This is a test text with numbers 0123456789 and symbols .,&%#";
        static readonly string[] Texts = new[] { Text0, Text1, Text2, Text4, Text8, TextA, TextB, TextC };
        //const string Text = "T";
        const double FontSize = 16;
        static readonly CultureInfo TextCultureInfo = CultureInfo.GetCultureInfo("en-us");
        static readonly Typeface TextTypeface = new Typeface("Courier New");
        //static readonly Pen TextShadowPen = new Pen(Brushes.Black, 3);
        //static readonly Brush TextBackgroundBrush = new SolidColorBrush(Color.FromArgb(64, 0, 0, 0));
        static readonly Brush TextForegroundBrush = Brushes.White;
        static readonly Point TextOrigin = new Point(10, 5);
        readonly Point TextOriginGlyph;

        readonly FormattedTextDrawer m_formattedText;
        readonly NaiveGlyphRunTextDrawer m_naiveGlyphRun;
        readonly FastGlyphRunTextDrawer m_fastGlyphRun;

        readonly DrawingVisual m_drawingVisual = new DrawingVisual();
        readonly RenderTargetBitmap m_renderBitmap = new RenderTargetBitmap(Width, Height, DPI, DPI, PixelFormats.Default);

        public DrawText()
        {
            GlyphTypeface glyphTypeface;
            if (!TextTypeface.TryGetGlyphTypeface(out glyphTypeface))
            {
                throw new ArgumentException("GlyphTypeface");
            }

            m_formattedText = new FormattedTextDrawer(TextTypeface, TextCultureInfo, FontSize);
            m_naiveGlyphRun = new NaiveGlyphRunTextDrawer(glyphTypeface, FontSize);
            m_fastGlyphRun = FastGlyphRunTextDrawer.Create(glyphTypeface, FontSize);

            double y = TextOrigin.Y + Math.Round(glyphTypeface.Baseline * FontSize);
            TextOriginGlyph = new Point(TextOrigin.X, y);
        }

        [Benchmark]
        public void RenderOpenDispose()
        {
            using (var dc = m_drawingVisual.RenderOpen())
            {
            }
        }

        [Benchmark(Baseline = true)]
        public void FormattedText()
        {
            using (var dc = m_drawingVisual.RenderOpen())
            {
                foreach (var t in Texts)
                {
                    m_formattedText.DrawText(t, TextOrigin, TextForegroundBrush, dc);
                }
            }
        }

        public void FormattedText_Save()
        {
            m_renderBitmap.Clear();
            FormattedText();
            m_renderBitmap.Render(m_drawingVisual);
            SaveBitmap(m_renderBitmap);
        }

        [Benchmark]
        public void NaiveGlyphRun()
        {
            using (var dc = m_drawingVisual.RenderOpen())
            {
                foreach (var t in Texts)
                {
                    m_naiveGlyphRun.DrawText(t, TextOrigin, TextForegroundBrush, dc);
                }

            }
        }

        public void NaiveGlyphRun_Save()
        {
            m_renderBitmap.Clear();
            NaiveGlyphRun();
            m_renderBitmap.Render(m_drawingVisual);
            SaveBitmap(m_renderBitmap);
        }

        [Benchmark]
        public void FastGlyphRun()
        {
            using (var dc = m_drawingVisual.RenderOpen())
            {
                foreach (var t in Texts)
                {
                    m_fastGlyphRun.DrawText(t, TextOrigin, TextForegroundBrush, dc);
                }
            }
        }

        public void FastGlyphRun_Save()
        {
            m_renderBitmap.Clear();
            FastGlyphRun();
            m_renderBitmap.Render(m_drawingVisual);
            SaveBitmap(m_renderBitmap);
        }

        private static void SaveBitmap(BitmapSource bitmap, [CallerMemberName] string fileName = null)
        {
            using (Stream stream = new FileStream(fileName + ".png", FileMode.Create))
            {
                var encoder = new PngBitmapEncoder();
                encoder.Interlace = PngInterlaceOption.Off;
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(stream);
            }
        }
    }
}