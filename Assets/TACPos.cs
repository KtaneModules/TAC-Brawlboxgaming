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
        public bool IsHome { get { return _pos == -1; } }

        public static TACPos GetStart(int color) { return new TACPos(8 * color); }

        public static readonly TACPos Home = new TACPos { _pos = -1 };

        public static bool operator ==(TACPos pos1, TACPos pos2) { return pos1._pos == pos2._pos; }
        public static bool operator !=(TACPos pos1, TACPos pos2) { return pos1._pos != pos2._pos; }

        public static TACPos operator +(TACPos pos, int amount) { return new TACPos(pos._pos + amount); }
        public static TACPos operator -(TACPos pos, int amount) { return new TACPos(pos._pos - amount); }
        public static TACPos operator ++(TACPos pos) { return pos + 1; }
        public static TACPos operator --(TACPos pos) { return pos - 1; }

        public static int operator -(TACPos a, TACPos b) { return (a._pos - b._pos + 32) % 32; }

        public bool Equals(TACPos other) { return other._pos == _pos; }
        public override bool Equals(object obj) { return obj is TACPos && ((TACPos) obj)._pos == _pos; }
        public override int GetHashCode() { return _pos.GetHashCode(); }
        public override string ToString() { return _pos == -1 ? "Home" : _pos.ToString(); }
    }
}