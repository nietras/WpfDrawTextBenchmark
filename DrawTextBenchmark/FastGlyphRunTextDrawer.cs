using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Media;

namespace DrawTextBenchmark
{
    public sealed class FastGlyphRunTextDrawer
    {
        static readonly PropertyInfo isInitializingPropertyInfo =
            typeof(GlyphRun).GetProperty("IsInitializing", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly MethodInfo isInitializingSetMethod = isInitializingPropertyInfo.GetSetMethod(true);
        readonly Action<GlyphRun, bool> setIsInitializing =
            isInitializingSetMethod.CreateDelegate(typeof(Action<GlyphRun, bool>)) as Action<GlyphRun, bool>;

        static readonly PropertyInfo isInitializedPropertyInfo =
            typeof(GlyphRun).GetProperty("IsInitialized", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly MethodInfo isInitializedSetMethod = isInitializedPropertyInfo.GetSetMethod(true);
        readonly Action<GlyphRun, bool> setIsInitialized =
            isInitializedSetMethod.CreateDelegate(typeof(Action<GlyphRun, bool>)) as Action<GlyphRun, bool>;
        readonly GlyphRun m_glyphRun;

        readonly GlyphTypeface m_typeface;
        readonly GlyphInfo[] m_glyphInfoTable;
        readonly double m_fontSize;
        readonly double m_baseline;
        ushort[] m_glyphIndexes = new ushort[1024];
        double[] m_advanceWidths = new double[1024];
        readonly ListWrapper<ushort> m_glyphIndexesList;
        readonly ListWrapper<double> m_advanceWidthsList;


        private FastGlyphRunTextDrawer(GlyphTypeface typeface, GlyphInfo[] glyphInfoTable, double fontSize, float dpi)
        {
            m_typeface = typeface;
            m_glyphInfoTable = glyphInfoTable;
            m_fontSize = fontSize;
            // Round to ensure baseline is in whole "pixels" as otherwise drawing is offset
            m_baseline = Math.Round(typeface.Baseline * fontSize);
            m_glyphIndexesList = new ListWrapper<ushort>(m_glyphIndexes, 1); // Setting size to 1 for glyphrun reuse
            m_advanceWidthsList = new ListWrapper<double>(m_advanceWidths, 1); // Setting size to 1 for glyphrun reuse

            m_glyphRun = new GlyphRun(m_typeface, 0, false, m_fontSize, dpi,
                m_glyphIndexesList, new Point(0,0), m_advanceWidthsList,
                null, null, null, null,
                null, null);
        }

        public static FastGlyphRunTextDrawer Create(GlyphTypeface typeface, double fontSize, float dpi)
        {
            var characterToGlyphMap = typeface.CharacterToGlyphMap;
            var advanceWidthsDictionary = typeface.AdvanceWidths;

            var glyphInfoTable = new GlyphInfo[char.MaxValue];
            foreach (var kvp in characterToGlyphMap)
            {
                var c = (char)kvp.Key;
                var glyphIndex = kvp.Value;
                double width = advanceWidthsDictionary[glyphIndex] * fontSize;
                var info = new GlyphInfo(glyphIndex, width);
                glyphInfoTable[c] = info;
            }
            return new FastGlyphRunTextDrawer(typeface, glyphInfoTable, fontSize, dpi);
        }

        public void DrawText(string text, Point origin, Brush brush, DrawingContext dc)
        {
            if (text.Length <= 0) { return; }

            EnsureArraySize(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];
                var info = m_glyphInfoTable[c];

                m_glyphIndexes[i] = info.Index;
                m_advanceWidths[i] = info.Width;
            }
            m_glyphIndexesList.SetSize(text.Length);
            m_advanceWidthsList.SetSize(text.Length);
            
            var fixedOrigin = new Point(origin.X, origin.Y + m_baseline);

            var glyphRun = m_glyphRun;
            setIsInitialized(glyphRun, false);
            setIsInitializing(glyphRun, true);
            glyphRun.BaselineOrigin = fixedOrigin;
            setIsInitializing(glyphRun, false);
            setIsInitialized(glyphRun, true);

            dc.DrawGlyphRun(brush, glyphRun);
        }

        private void EnsureArraySize(int length)
        {
            if (length > m_glyphIndexes.Length)
            {
                var newLength = Math.Max(m_glyphIndexes.Length * 2, length);
                m_glyphIndexes = new ushort[newLength];
                m_advanceWidths = new double[newLength];
                m_glyphIndexesList.SetArray(m_glyphIndexes);
                m_advanceWidthsList.SetArray(m_advanceWidths);
            }
        }

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

        sealed class ListWrapper<T> : IList<T>
        {
            T[] m_array;
            int m_size;

            public ListWrapper(T[] array, int size)
            {
                m_array = array;
                m_size = size;
            }

            public T this[int index] { get => m_array[index]; set => throw new NotImplementedException(); }

            public int Count => m_size;

            public void CopyTo(T[] array, int arrayIndex)
            {
                Array.Copy(m_array, 0, array, arrayIndex, m_size);
            }

            public void SetArray(T[] array) { m_array = array; }
            public void SetSize(int size) { m_size = size; }

            public bool IsReadOnly => throw new NotImplementedException();

            public void Add(T item) => throw new NotImplementedException();

            public void Clear() => throw new NotImplementedException();

            public bool Contains(T item) => throw new NotImplementedException();

            public IEnumerator<T> GetEnumerator() => throw new NotImplementedException();

            public int IndexOf(T item) => throw new NotImplementedException();

            public void Insert(int index, T item) => throw new NotImplementedException();

            public bool Remove(T item) => throw new NotImplementedException();

            public void RemoveAt(int index) => throw new NotImplementedException();

            IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
        }
    }
}
