using System;
using System.Runtime.CompilerServices;
using System.Text;
namespace RTLTMPro
{
    public class FastStringBuilder
    {
        // Shared scratch buffer for ToString(), so converting to string allocates only the result.
        [ThreadStatic] private static char[] charBuffer;

        // Using fields to be as efficient as possible
        private int length;
        public int Length
        {
            get { return length; }
            set
            {
                if (value <= length) length = value;
            }
        }
        private int[] array;

        public FastStringBuilder(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            array = new int[capacity];
        }

        public FastStringBuilder(string text) : this(text, text.Length)
        {
        }

        public FastStringBuilder(string text, int capacity) : this(capacity)
        {
            SetValue(text);
        }

        public static implicit operator string(FastStringBuilder x)
        {
            return x.ToString();
        }

        public static implicit operator FastStringBuilder(string x)
        {
            return new FastStringBuilder(x);
        }

        /// <summary>
        ///     Backing array for tight internal loops. Only the first <see cref="Length"/> items are valid,
        ///     and the reference changes whenever the builder grows.
        /// </summary>
        internal int[] RawArray
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return array; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Get(int index)
        {
            return array[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int index, int ch)
        {
            array[index] = ch;
        }

        public void SetValue(string text)
        {
            int textLength = text.Length;
            EnsureCapacity(textLength, false);

            int[] arr = array;
            int len = 0;
            for (int i = 0; i < textLength; i++)
            {
                char ch = text[i];
                if ((uint)(ch - 0xD800) > 0x7FF)
                {
                    // Not a surrogate: the code point is the char itself.
                    arr[len++] = ch;
                    continue;
                }

                // Surrogates go through ConvertToUtf32 so malformed pairs still throw like before.
                int unicode32CodePoint = char.ConvertToUtf32(text, i);
                if (unicode32CodePoint > 0xffff)
                {
                    i++;
                }
                arr[len++] = unicode32CodePoint;
            }

            length = len;
        }

        public void SetValue(FastStringBuilder other)
        {
            EnsureCapacity(other.length, false);
            Array.Copy(other.array, 0, array, 0, other.length);
            length = other.length;
        }

        public void Append(int ch)
        {
            if (length == array.Length)
                EnsureCapacity(length + 1, true);

            array[length++] = ch;
        }

        public void Append(char ch)
        {
            if (length == array.Length)
                EnsureCapacity(length + 1, true);

            array[length++] = ch;
        }

        public void Insert(int pos, FastStringBuilder str, int offset, int count)
        {
            if (str == this) throw new InvalidOperationException("You cannot pass the same string builder to insert");
            if (count == 0) return;

            EnsureCapacity(length + count, true);

            Array.Copy(array, pos, array, pos + count, length - pos);
            Array.Copy(str.array, offset, array, pos, count);
            length += count;
        }

        public void Insert(int pos, FastStringBuilder str)
        {
            Insert(pos, str, 0, str.length);
        }

        public void Insert(int pos, int ch)
        {
            EnsureCapacity(length + 1, true);

            Array.Copy(array, pos, array, pos + 1, length - pos);

            array[pos] = ch;
            length++;
        }

        public void RemoveAll(int character)
        {
            int[] arr = array;
            int len = length;
            int j = 0; // write index
            for (int i = 0; i < len; i++)
            {
                int ch = arr[i];
                if (ch == character) continue;

                arr[j++] = ch;
            }

            length = j;
        }

        public void Remove(int start, int length)
        {
            Array.Copy(array, start + length, array, start, this.length - start - length);

            this.length -= length;
        }

        public void Reverse(int startIndex, int length)
        {
            int[] arr = array;
            int i = startIndex;
            int j = startIndex + length - 1;
            while (i < j)
            {
                int first = arr[i];
                arr[i] = arr[j];
                arr[j] = first;
                i++;
                j--;
            }
        }

        public void Reverse()
        {
            Reverse(0, length);
        }

        public void Substring(FastStringBuilder output, int start, int length)
        {
            output.length = 0;
            output.EnsureCapacity(length, false);
            Array.Copy(array, start, output.array, 0, length);
            output.length = length;
        }

        public override string ToString()
        {
            int len = length;
            if (len == 0) return string.Empty;

            // Worst case every code point needs a surrogate pair.
            int maxChars = len * 2;
            char[] buffer = charBuffer;
            if (buffer == null || buffer.Length < maxChars)
                charBuffer = buffer = new char[Math.Max(maxChars, 256)];

            int[] arr = array;
            int charCount = 0;
            for (int i = 0; i < len; i++)
            {
                int codePoint = arr[i];
                if (codePoint <= 0xFFFF)
                {
                    buffer[charCount++] = (char)codePoint;
                }
                else
                {
                    codePoint -= 0x10000;
                    buffer[charCount++] = (char)((codePoint >> 10) + 0xD800);
                    buffer[charCount++] = (char)((codePoint & 0x3FF) + 0xDC00);
                }
            }
            return new string(buffer, 0, charCount);
        }

        public string ToDebugString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                sb.Append("\\");
                sb.Append(array[i].ToString("X"));
            }
            return sb.ToString();
        }

        public void Replace(int oldChar, int newChar)
        {
            int[] arr = array;
            int len = length;
            for (int i = 0; i < len; i++)
            {
                if (arr[i] == oldChar)
                    arr[i] = newChar;
            }
        }

        public void Replace(FastStringBuilder oldStr, FastStringBuilder newStr)
        {
            int oldLength = oldStr.length;
            int newLength = newStr.length;
            if (oldLength == 0) return;

            int[] oldArr = oldStr.array;
            for (int i = 0; i <= length - oldLength; i++)
            {
                bool match = true;
                for (int j = 0; j < oldLength; j++)
                {
                    if (array[i + j] != oldArr[j])
                    {
                        match = false;
                        break;
                    }
                }

                if (!match) continue;

                if (oldLength != newLength)
                {
                    // Shift the tail to make room for (or close the gap left by) the replacement
                    int tailStart = i + oldLength;
                    int newTotalLength = length - oldLength + newLength;
                    EnsureCapacity(newTotalLength, true);
                    Array.Copy(array, tailStart, array, i + newLength, length - tailStart);
                    length = newTotalLength;
                }

                Array.Copy(newStr.array, 0, array, i, newLength);

                i += newLength;
            }
        }

        public void Clear()
        {
            length = 0;
        }

        /// <summary>
        ///     Makes sure the backing array can hold <paramref name="cap"/> items.
        ///     Must be called before <see cref="length"/> is increased, since the first
        ///     <see cref="length"/> items are what gets copied when growing.
        /// </summary>
        private void EnsureCapacity(int cap, bool keepValues)
        {
            int capacity = array.Length;
            if (capacity >= cap)
                return;

            if (capacity == 0)
                capacity = 1;

            while (capacity < cap)
                capacity *= 2;

            int[] newArray = new int[capacity];
            if (keepValues)
                Array.Copy(array, 0, newArray, 0, length);

            array = newArray;
        }

        internal void EnsureAdditionalCapacity(int additionalLength)
        {
            EnsureCapacity(length + additionalLength, true);
        }

        internal void SetLengthUnchecked(int value)
        {
            length = value;
        }
    }
}
