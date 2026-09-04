namespace Crucible.Diagnostics
{
    /// <summary>
    /// A fixed-capacity text buffer that never allocates after construction.
    ///
    /// The overlay reports GC allocation per frame, so it cannot allocate while doing so — string
    /// interpolation in the readout would show up as garbage in its own measurement. Everything is
    /// appended into one <see cref="char"/> array that TextMeshPro reads directly.
    /// </summary>
    public sealed class CharBuffer
    {
        private readonly char[] _chars;
        private int _length;

        public CharBuffer(int capacity)
        {
            _chars = new char[capacity];
            _length = 0;
        }

        public char[] Chars => _chars;
        public int Length => _length;

        public void Clear() => _length = 0;

        public void Append(char value)
        {
            if (_length < _chars.Length)
            {
                _chars[_length++] = value;
            }
        }

        /// <summary>
        /// Appends a string. Callers pass literals, which live in the assembly's string pool and
        /// are not allocated at runtime.
        /// </summary>
        public void Append(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                Append(value[i]);
            }
        }

        public void AppendLine() => Append('\n');

        public void AppendInt(long value)
        {
            if (value < 0)
            {
                Append('-');
                value = -value;
            }

            // Write digits into a small stack buffer, then reverse them out.
            const int MaxDigits = 20;
            int digitCount = 0;
            System.Span<char> digits = stackalloc char[MaxDigits];

            do
            {
                digits[digitCount++] = (char)('0' + (int)(value % 10));
                value /= 10;
            }
            while (value > 0 && digitCount < MaxDigits);

            for (int i = digitCount - 1; i >= 0; i--)
            {
                Append(digits[i]);
            }
        }

        /// <summary>Appends a value with a fixed number of decimals, rounded half away from zero.</summary>
        public void AppendFixed(double value, int decimals)
        {
            long scale = 1;
            for (int i = 0; i < decimals; i++)
            {
                scale *= 10;
            }

            bool negative = value < 0.0;
            if (negative)
            {
                value = -value;
            }

            long scaled = (long)(value * scale + 0.5);
            long whole = scaled / scale;
            long fraction = scaled % scale;

            if (negative && (whole != 0 || fraction != 0))
            {
                Append('-');
            }

            AppendInt(whole);

            if (decimals <= 0)
            {
                return;
            }

            Append('.');

            // Leading zeros of the fractional part have to be written explicitly.
            long divisor = scale / 10;
            while (divisor > 0)
            {
                Append((char)('0' + (int)(fraction / divisor % 10)));
                divisor /= 10;
            }
        }
    }
}
