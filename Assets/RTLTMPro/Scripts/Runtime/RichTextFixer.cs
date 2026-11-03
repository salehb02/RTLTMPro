namespace RTLTMPro
{
    public static class RichTextFixer
    {
        public enum TagType
        {
            None,
            Opening,
            Closing,
            SelfContained,
        }

        public struct Tag
        {
            public int Start;
            public int End;
            public int HashCode;
            public TagType Type;

            public Tag(int start, int end, TagType type, int hashCode)
            {
                Type = type;
                Start = start;
                End = end;
                HashCode = hashCode;
            }
        }

        /// <summary>
        ///     Fixes rich text tags in input string and returns the result.
        /// </summary>
        public static void Fix(FastStringBuilder text)
        {
            int[] letters = text.RawArray;
            int length = text.Length;
            for (int i = 0; i < length; i++)
            {
                // If we couldn't find a tag, end the process
                if (!FindTagBounds(letters, length, i, out int start, out int end))
                {
                    break;
                }

                text.Reverse(start, end - start + 1);

                i = end;
            }
        }

        /// <summary>
        ///     Same matching rules as <see cref="FindTag"/>, without the hash code and tag type that
        ///     <see cref="Fix"/> does not use.
        /// </summary>
        private static bool FindTagBounds(int[] letters, int length, int from, out int start, out int end)
        {
            int i = from;
            while (i < length)
            {
                if (letters[i] != '<')
                {
                    i++;
                    continue;
                }

                int j = i + 1;
                // Rich text tag cannot start with space
                if (j < length && letters[j] != ' ')
                {
                    for (; j < length; j++)
                    {
                        int jChar = letters[j];
                        if (jChar == '>')
                        {
                            start = i;
                            end = j;
                            return true;
                        }

                        // Rich text tag cannot contain <. That '<' is the next candidate.
                        if (jChar == '<') break;
                    }

                    // Reached the end without '>' or '<': no tag can start after this point either.
                    if (j == length) break;

                    i = j;
                    continue;
                }

                i++;
            }

            start = 0;
            end = 0;
            return false;
        }

        public static void FindTag(
            FastStringBuilder str,
            int start,
            out Tag tag)
        {
            for (int i = start; i < str.Length;)
            {
                if (str.Get(i) != '<')
                {
                    i++;
                    continue;
                }

                bool calculateHashCode = true;
                tag.HashCode = 0;
                for (int j = i + 1; j < str.Length; j++)
                {
                    int jChar = str.Get(j);

                    if (calculateHashCode)
                    {
                        if (Char32Utils.IsLetter(jChar))
                        {
                            unchecked
                            {
                                if (tag.HashCode == 0)
                                {
                                    tag.HashCode = jChar.GetHashCode();
                                }
                                else
                                {
                                    tag.HashCode = (tag.HashCode * 397) ^ jChar.GetHashCode();
                                }
                            }
                        }
                        else if (tag.HashCode != 0)
                        {
                            // We have computed the hash code. Now we reached a non letter character. We need to stop
                            calculateHashCode = false;
                        }
                    }

                    // Rich text tag cannot contain < or start with space
                    if ((j == i + 1 && jChar == ' ') || jChar == '<')
                    {
                        break;
                    }

                    if (jChar == '>')
                    {
                        // Check if the tag is closing, opening or self contained

                        tag.Start = i;
                        tag.End = j;

                        if (str.Get(j - 1) == '/')
                        {
                            // This is self contained.
                            tag.Type = TagType.SelfContained;
                        }
                        else if (str.Get(i + 1) == '/')
                        {
                            // This is closing
                            tag.Type = TagType.Closing;
                        }
                        else
                        {
                            tag.Type = TagType.Opening;
                        }

                        return;
                    }
                }

                i++;
            }

            tag.Start = 0;
            tag.End = 0;
            tag.Type = TagType.None;
            tag.HashCode = 0;
        }
    }
}