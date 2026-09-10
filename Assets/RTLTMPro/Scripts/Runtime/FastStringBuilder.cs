using System;
using System.Runtime.CompilerServices;
using System.Text;
namespace RTLTMPro
{
    public class FastStringBuilder
    {
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
        private int capacity;

        public FastStringBuilder(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            this.capacity = capacity;
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
            int len = 0;
            length = text.Length;
            EnsureCapacity(length, false);

            for (int i = 0; i < text.Length; i++)
            {
                int unicode32CodePoint = char.ConvertToUtf32(text, i);
                if (unicode32CodePoint > 0xffff)
                {
                    i++;
                }
                array[len++] = unicode32CodePoint;
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
            length++;
            if (capacity < length)
                EnsureCapacity(length, true);

            array[length - 1] = ch;
        }

        public void Append(char ch)
        {
            length++;
            if (capacity < length)
                EnsureCapacity(length, true);

            array[length - 1] = ch;
        }

        public void Insert(int pos, FastStringBuilder str, int offset, int count)
        {
            if (str == this) throw new InvalidOperationException("You cannot pass the same string builder to insert");
            if (count == 0) return;

            length += count;
            EnsureCapacity(length, true);

            Array.Copy(array, pos, array, pos + count, length - count - pos);
            Array.Copy(str.array, offset, array, pos, count);
        }

        public void Insert(int pos, FastStringBuilder str)
        {
            Insert(pos, str, 0, str.length);
        }

        public void Insert(int pos, int ch)
        {
            length++;
            EnsureCapacity(length, true);

            Array.Copy(array, pos, array, pos + 1, length - pos - 1);

            array[pos] = ch;
        }

        public void RemoveAll(int character)
        {
            int j = 0; // write index
            int i = 0; // read index
            for (; i < length; i++)
            {
                if (array[i] == character) continue;

                array[j] = array[i];
                j++;
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
            for (int i = 0; i < length / 2; i++)
            {
                int firstIndex = startIndex + i;
                int secondIndex = startIndex + length - i - 1;

                int first = array[firstIndex];
                int second = array[secondIndex];

                array[firstIndex] = second;
                array[secondIndex] = first;
            }
        }

        public void Reverse()
        {
            Reverse(0, length);
        }

        public void Substring(FastStringBuilder output, int start, int length)
        {
            output.length = 0;
            for (int i = 0; i < length; i++)
                output.Append(array[start + i]);
        }

        public override string ToString()
        {
            if (length == 0) return string.Empty;

            StringBuilder sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                int codePoint = array[i];
                if (codePoint <= 0xFFFF)
                {
                    sb.Append((char)codePoint);
                }
                else
                {
                    codePoint -= 0x10000;
                    sb.Append((char)((codePoint >> 10) + 0xD800));
                    sb.Append((char)((codePoint & 0x3FF) + 0xDC00));
                }
            }
            return sb.ToString();
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
            for (int i = 0; i < length; i++)
            {
                if (array[i] == oldChar)
                    array[i] = newChar;
            }
        }

        public void Replace(FastStringBuilder oldStr, FastStringBuilder newStr)
        {
            if (oldStr.Length == 0) return;

            for (int i = 0; i <= length - oldStr.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < oldStr.Length; j++)
                {
                    if (array[i + j] != oldStr.Get(j))
                    {
                        match = false;
                        break;
                    }
                }

                if (!match) continue;

                if (oldStr.Length == newStr.Length)
                {
                    for (int k = 0; k < oldStr.Length; k++)
                    {
                        array[i + k] = newStr.Get(k);
                    }
                }
                else if (oldStr.Length < newStr.Length)
                {
                    // We need to expand capacity
                    int diff = newStr.Length - oldStr.Length;
                    length += diff;
                    EnsureCapacity(length, true);

                    // Move everything forward by difference of length
                    for (int k = length - diff - 1; k >= i + oldStr.Length; k--)
                    {
                        array[k + diff] = array[k];
                    }

                    // Start writing new string
                    for (int k = 0; k < newStr.Length; k++)
                    {
                        array[i + k] = newStr.Get(k);
                    }
                }
                else
                {
                    // We need to shrink
                    int diff = oldStr.Length - newStr.Length;

                    // Move everything backwards by diff
                    for (int k = i + diff; k < length - diff; k++)
                    {
                        array[k] = array[k + diff];
                    }

                    for (int k = 0; k < newStr.Length; k++)
                    {
                        array[i + k] = newStr.Get(k);
                    }

                    length -= diff;
                }

                i += newStr.Length;
            }
        }

        public void Clear()
        {
            length = 0;
        }

        private void EnsureCapacity(int cap, bool keepValues)
        {
            if (capacity >= cap)
                return;

            if (capacity == 0)
                capacity = 1;

            while (capacity < cap)
                capacity *= 2;

            if (keepValues)
            {
                int[] newArray = new int[capacity];
                Array.Copy(array, 0, newArray, 0, length);
                array = newArray;
            }
            else
            {
                array = new int[capacity];
            }
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
