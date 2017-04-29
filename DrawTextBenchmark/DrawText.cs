using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
                    .WithIterationTime(TimeInterval.FromMilliseconds(100)) // 100ms per iteration
                    .WithWarmupCount(3)
                    .WithTargetCount(10)
                );
            }
        }

        const float DPI = 96;
        const int Width = 1024;
        const int Height = 32;
        const string Text = "This is a test text with numbers 0123456789 and symbols .,&%#";
        const double FontSize = 16;
        static readonly CultureInfo TextCultureInfo = CultureInfo.GetCultureInfo("en-us");
        static readonly Typeface TextTypeface = new Typeface("Courier New");
        //static readonly Pen TextShadowPen = new Pen(Brushes.Black, 3);
        //static readonly Brush TextBackgroundBrush = new SolidColorBrush(Color.FromArgb(64, 0, 0, 0));
        static readonly Brush TextForegroundBrush = Brushes.White;
        static readonly Point TextOrigin = new Point(10, 5);
        readonly Point TextOriginGlyph;

        readonly GlyphTypeface glyphTypeface;
        readonly IDictionary<int, ushort> characterToGlyphMap;
        readonly IDictionary<ushort, double> advanceWidthsDictionary;
        struct GlyphInfo
        {
            public readonly ushort Index;
            public readonly double Width; // Pre-computed with font size for now

            public GlyphInfo(ushort glyphIndex, double width) : this()
            {
                Index = glyphIndex;
                Width = width;
            }
        }
        readonly Dictionary<char, ushort> characterToGlyphIndex;
        readonly Dictionary<ushort, double> glyphIndexToAdvanceWidth;
        readonly Dictionary<char, GlyphInfo> characterToGlyphInfo;
        readonly ushort[] glyphIndexes;
        readonly double[] advanceWidths;
        readonly GlyphRun glyphRun;

        readonly DrawingVisual m_drawingVisual = new DrawingVisual();
        readonly RenderTargetBitmap m_renderBitmap = new RenderTargetBitmap(Width, Height, DPI, DPI, PixelFormats.Default);

        public DrawText()
        {
            if (!TextTypeface.TryGetGlyphTypeface(out glyphTypeface))
            {
                throw new ArgumentException("GlyphTypeface");
            }
            characterToGlyphMap = glyphTypeface.CharacterToGlyphMap;
            advanceWidthsDictionary = glyphTypeface.AdvanceWidths;

            double y = TextOrigin.Y + Math.Round(glyphTypeface.Baseline * FontSize);
            TextOriginGlyph = new Point(TextOrigin.X, y);

            var text = Text;
            glyphIndexes = new ushort[text.Length];
            advanceWidths = new double[text.Length];

            var characterCount = characterToGlyphMap.Count;
            characterToGlyphIndex = new Dictionary<char, ushort>(characterCount);
            // Don't need dictionary for glyph index probably since it should be from 0-count
            glyphIndexToAdvanceWidth = new Dictionary<ushort, double>(characterCount);
            characterToGlyphInfo = new Dictionary<char, GlyphInfo>(characterCount);

            foreach (var kvp in characterToGlyphMap)
            {
                var c = (char)kvp.Key;
                var glyphIndex = kvp.Value;
                characterToGlyphIndex.Add(c, glyphIndex);

                double width = advanceWidthsDictionary[glyphIndex] * FontSize;
                if (!glyphIndexToAdvanceWidth.ContainsKey(glyphIndex))
                {
                    glyphIndexToAdvanceWidth.Add(glyphIndex, width);
                }
                characterToGlyphInfo.Add(c, new GlyphInfo(glyphIndex, width));
            }

            double totalWidth = 0;
            for (int n = 0; n < text.Length; n++)
            {
                var c = text[n];
                ushort glyphIndex;
                if (!characterToGlyphIndex.TryGetValue(c, out glyphIndex))
                {
                    glyphIndex = characterToGlyphMap[c];
                    characterToGlyphIndex.Add(c, glyphIndex);
                }
                glyphIndexes[n] = glyphIndex;

                double width;
                if (!glyphIndexToAdvanceWidth.TryGetValue(glyphIndex, out width))
                {
                    width = advanceWidthsDictionary[glyphIndex] * FontSize;
                    glyphIndexToAdvanceWidth.Add(glyphIndex, width);
                }
                advanceWidths[n] = width;

                totalWidth += width;
            }
            glyphRun = new GlyphRun(glyphTypeface, 0, false, FontSize, DPI,
                glyphIndexes, TextOrigin, advanceWidths, null, null, null, null,
                null, null);
        }

        [Benchmark]
        public void RenderOpenDispose()
        {
            using (var dc = m_drawingVisual.RenderOpen())
            {
            }
        }

        [Benchmark(Baseline = true)]
        public void DrawFormattedText()
        {
            using (var dc = m_drawingVisual.RenderOpen())
            {
                var formattedText = new FormattedText(Text, TextCultureInfo,
                    FlowDirection.LeftToRight, TextTypeface, FontSize, TextForegroundBrush, DPI);
                dc.DrawText(formattedText, TextOrigin);
            }
        }

        public void DrawFormattedText_Save()
        {
            m_renderBitmap.Clear();
            DrawFormattedText();
            m_renderBitmap.Render(m_drawingVisual);
            SaveBitmap(m_renderBitmap);
        }

        [Benchmark]
        public void DrawGlyphRun()
        {
            using (var dc = m_drawingVisual.RenderOpen())
            {
                var text = Text;
                //double totalWidth = 0;
                for (int n = 0; n < text.Length; n++)
                {
                    var c = text[n];
                    var info = characterToGlyphInfo[c];
                    //ushort glyphIndex = characterToGlyphIndex[c];
                    //ushort glyphIndex;
                    //if (!characterToGlyphIndex.TryGetValue(c, out glyphIndex))
                    //{
                    //    glyphIndex = characterToGlyphMap[c];
                    //    characterToGlyphIndex.Add(c, glyphIndex);
                    //}
                    //glyphIndexes[n] = glyphIndex;

                    //double width = glyphIndexToAdvanceWidth[glyphIndex];
                    //double width;
                    //if (!glyphIndexToAdvanceWidth.TryGetValue(glyphIndex, out width))
                    //{
                    //    width = advanceWidthsDictionary[glyphIndex] * FontSize;
                    //    glyphIndexToAdvanceWidth.Add(glyphIndex, width);
                    //}
                    //advanceWidths[n] = width;

                    glyphIndexes[n] = info.Index;
                    advanceWidths[n] = info.Width;

                    //totalWidth += width;
                }

                var glyphRun = new GlyphRun(glyphTypeface, 0, false, FontSize, DPI,
                    glyphIndexes, TextOriginGlyph, advanceWidths, null, null, null, null,
                    null, null);
                dc.DrawGlyphRun(TextForegroundBrush, glyphRun);
            }
        }

        public void DrawGlyphRun_Save()
        {
            m_renderBitmap.Clear();
            DrawGlyphRun();
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