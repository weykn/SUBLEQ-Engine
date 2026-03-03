using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;

namespace OneInsArch
{
    public class VirtualLiteral
    {
        public static List<VirtualLiteral> DefinedLiterals { get; set; } = [];

        public long Offset { get; set; }
        public long Value { get; set; }

        public static string AsJson()
            => JsonSerializer.Serialize(DefinedLiterals);

        public static long GetOrDefine(long value, int line)
        {
            foreach (var l in DefinedLiterals)
            {
                if (l.Value == value)
                    return l.Offset;
            }

            IO.Log($"PREDEFINE VIRTUAL LITERAL {value}", line);

            var literal = new VirtualLiteral(value);
            DefinedLiterals.Add(literal);
            return literal.Offset;
        }

        private VirtualLiteral(long value)
        {
            Value = value;
        }
    }
}
