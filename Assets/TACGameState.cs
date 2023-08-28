using System.Linq;

namespace Assets
{
    class TACGameState
    {
        // Pieces[0] is player, Pieces[1] is partner, all other indexes are enemies
        public TACPos?[] Pieces;
        public TACPos PlayerPosition { get { return Pieces[0].Value; } }
        public TACPos PartnerPosition { get { return Pieces[2].Value; } }
        public bool PlayerInHome { get { return Pieces[0].Value.IsHome; } }

        public int PlayerColor;     // multiply by 8 to get position where you can enter home

        public bool HasPieceOn(TACPos pos)
        {
            return Pieces.Contains(pos);
        }

        public TACGameState Clone()
        {
            return new TACGameState { Pieces = Pieces.ToArray() };
        }

        public void RemoveEnemyPieceIfPresent(TACPos pos)
        {
            for (var i = 1; i < Pieces.Length; i += 2)
                if (Pieces[i] == pos)
                    Pieces[i] = null;
        }

        public bool IsPartnerOn(TACPos pos)
        {
            return Pieces[2] == pos;
        }

        public void SetPlayerPosition(TACPos pos)
        {
            Pieces[0] = pos;
        }

        public void SwapPieces(int pIx1, int pIx2)
        {
            var tmp = Pieces[pIx1];
            Pieces[pIx1] = Pieces[pIx2];
            Pieces[pIx2] = tmp;
        }
    }
}
