
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OneInsArch
{
    public class Label(
            string name,
            long offset,
            bool isLiteral,
            string? value,
            long[]? usedAt = null)
    {
        public string Name { get; } = name;
        public long Offset { get; } = offset;
        public bool IsLiteral { get; } = isLiteral;
        public string? Value { get; } = value;
        public List<long> UsedAt { get; set; } = usedAt?.ToList() ?? [];

        public override string ToString()
            => JsonSerializer.Serialize(this);
    }
}
