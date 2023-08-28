using System;

namespace Assets
{
    public struct TACPos : IEquatable<TACPos>
    {
        // There are 32 spaces on the board.
        // Spaces #0, 8, 16, 24 are the places where a piece can move into a home.
        // Space #-1 is the home itself.
        private int _pos;

        public TACPos(int pos) { _pos = (pos % 32 + 32) % 32; }
        public bool IsHome => _pos == -1;

        public static TACPos GetStart(int seat) => new TACPos(8 * seat);

        public static readonly TACPos Home = new TACPos { _pos = -1 };

        public static bool operator ==(TACPos pos1, TACPos pos2) => pos1._pos == pos2._pos;
        public static bool operator !=(TACPos pos1, TACPos pos2) => pos1._pos != pos2._pos;

        public static TACPos operator +(TACPos pos, int amount) => new TACPos(pos._pos + amount);
        public static TACPos operator -(TACPos pos, int amount) => new TACPos(pos._pos - amount);
        public static TACPos operator ++(TACPos pos) => pos + 1;
        public static TACPos operator --(TACPos pos) => pos - 1;

        public static int operator -(TACPos a, TACPos b) => (a._pos - b._pos + 32) % 32;
        public static explicit operator int(TACPos pos) => pos._pos;

        public bool Equals(TACPos other) => other._pos == _pos;
        public override bool Equals(object obj) => obj is TACPos && ((TACPos) obj)._pos == _pos;
        public override int GetHashCode() => _pos.GetHashCode();
        public override string ToString() => _pos == -1 ? "Home" : _pos.ToString();
    }
}