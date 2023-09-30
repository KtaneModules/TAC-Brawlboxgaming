using System;
using UnityEngine;

namespace Assets
{
    public struct TACPos : IEquatable<TACPos>
    {
        private static readonly Vector3[] boardPositions = new[] {
            #region Vectors
            new Vector3(-0.04433801f, 0.01470134f, 0.02188657f),
            new Vector3(-0.03333801f, 0.01470134f, 0.02188657f),
            new Vector3(-0.02243801f, 0.01470134f, 0.02188657f),
            new Vector3(-0.01153801f, 0.01470134f, 0.02188657f),
            new Vector3(-0.00063801f, 0.01470134f, 0.02188657f),
            new Vector3(0.01056199f, 0.01470134f, 0.02188657f),
            new Vector3(0.02146199f, 0.01470134f, 0.02188657f),
            new Vector3(0.03246199f, 0.01470134f, 0.02188657f),
            new Vector3(0.04346199f, 0.01470134f, 0.02188657f),
            new Vector3(0.05436199f, 0.01470134f, 0.02188657f),
            new Vector3(0.06536199f, 0.01470134f, 0.02188657f),
            new Vector3(0.06536199f, 0.01470134f, 0.01088657f),
            new Vector3(0.06536199f, 0.01470134f, -0.00011343f),
            new Vector3(0.06536199f, 0.01470134f, -0.01111343f),
            new Vector3(0.06536199f, 0.01470134f, -0.02211343f),
            new Vector3(0.05436199f, 0.01470134f, -0.02211343f),
            new Vector3(0.04346199f, 0.01470134f, -0.02211343f),
            new Vector3(0.03246199f, 0.01470134f, -0.02211343f),
            new Vector3(0.02146199f, 0.01470134f, -0.02211343f),
            new Vector3(0.01056199f, 0.01470134f, -0.02211343f),
            new Vector3(-0.00063801f, 0.01470134f, -0.02211343f),
            new Vector3(-0.01153801f, 0.01470134f, -0.02211343f),
            new Vector3(-0.02243801f, 0.01470134f, -0.02211343f),
            new Vector3(-0.03333801f, 0.01470134f, -0.02211343f),
            new Vector3(-0.04433801f, 0.01470134f, -0.02211343f),
            new Vector3(-0.05523801f, 0.01470134f, -0.02211343f),
            new Vector3(-0.06623801f, 0.01470134f, -0.02211343f),
            new Vector3(-0.06623801f, 0.01470134f, -0.01111343f),
            new Vector3(-0.06623801f, 0.01470134f, -0.00011343f),
            new Vector3(-0.06623801f, 0.01470134f, 0.01088657f),
            new Vector3(-0.06623801f, 0.01470134f, 0.02188657f),
            new Vector3(-0.05523801f, 0.01470134f, 0.02188657f),
            #endregion
        };
        private static readonly Vector3[] homePositions = new[] {
            #region Vectors
                new Vector3(0.04346199f, 0.01470134f, 0.01088657f),
                new Vector3(0.04346199f, 0.01470134f, -0.01111343f),
                new Vector3(-0.04433801f, 0.01470134f, -0.01111343f),
                new Vector3(-0.04433801f, 0.01470134f, 0.01088657f)
            #endregion
        };

        // There are 32 spaces on the board.
        // Spaces #0, 8, 16, 24 are the places where a piece can move into a home.
        // Spaces #10, 14, 26, 30 are the corners.
        // Space #-1 is the home itself.
        private int _pos;

        public TACPos(int pos) { _pos = (pos % 32 + 32) % 32; }
        public bool IsHome => _pos == -1;
        public bool IsCorner => _pos == 10 || _pos == 14 || _pos == 26 || _pos == 30;
        public Vector3 Vector(TACGameState state) => IsHome ? homePositions[state.PlayerSeat] : boardPositions[_pos];

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