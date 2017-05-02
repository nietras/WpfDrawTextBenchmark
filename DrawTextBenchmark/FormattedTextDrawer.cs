using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace DrawTextBenchmark
{
    public sealed class FormattedTextDrawer
    {
        const float DPI = 96;
        readonly CultureInfo m_cultureInfo;
        readonly Typeface m_typeface;
        readonly double m_fontSize;

        public FormattedTextDrawer(Typeface typeface, CultureInfo cultureInfo, double fontSize)
        {
            m_cultureInfo = cultureInfo ?? throw new ArgumentNullException(nameof(cultureInfo));
            m_typeface = typeface ?? throw new ArgumentNullException(nameof(typeface));
            m_fontSize = fontSize;
        }

        public void DrawText(string text, Point origin, Brush brush, DrawingContext dc)
        {
            if (text.Length <= 0) { return; }

            var formattedText = new FormattedText(text, m_cultureInfo,
                FlowDirection.LeftToRight, m_typeface, m_fontSize, brush, DPI);

            dc.DrawText(formattedText, origin);
        }
    }
}
