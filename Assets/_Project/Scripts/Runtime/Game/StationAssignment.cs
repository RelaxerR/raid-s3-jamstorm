using System;
using System.Collections.Generic;

namespace SecondCatch.Solo
{
    /// <summary>Локальные роли станций; боевая лебёдка не входит в текущий срез.</summary>
    public enum StationRole { Helm, Nets, Harpoon, Sail, Repair }

    /// <summary>Одно назначение на исполнителя и одного исполнителя на конкретную станцию.</summary>
    public sealed class StationAssignment
    {
        private readonly Dictionary<string, string> _operators = new Dictionary<string, string>();
        public string OperatorAt(string stationId) => stationId != null && _operators.TryGetValue(stationId, out var id) ? id : null;
        public bool Assign(string operatorId, string stationId)
        {
            if (string.IsNullOrWhiteSpace(operatorId) || string.IsNullOrWhiteSpace(stationId)) return false;
            var current = OperatorAt(stationId);
            if (current == operatorId) return true;
            if (current != null) return false;
            Release(operatorId);
            _operators[stationId] = operatorId;
            return true;
        }
        public void Release(string operatorId)
        {
            string found = null;
            foreach (var pair in _operators)
                if (pair.Value == operatorId) { found = pair.Key; break; }
            if (found != null) _operators.Remove(found);
        }
    }
}
