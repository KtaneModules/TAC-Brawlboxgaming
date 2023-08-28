using System.Linq;

namespace Assets
{
    class TACGameState
    {
        // Pieces[0] is player, Pieces[2] is partner, all other indexes are enemies
        public TACPos?[] Pieces;
        public TACPos PlayerPosition => Pieces[0].Value;
        public TACPos PartnerPosition => Pieces[2].Value;
        public bool PlayerInHome => Pieces[0].Value.IsHome;

        public int PlayerSeat;

        private TACGameState() { }

        public static TACGameState FinalState(int playerSeat, TACPos partnerFinalPosition) => new TACGameState
        {
            PlayerSeat = playerSeat,
            Pieces = new TACPos?[] { TACPos.Home, null, partnerFinalPosition, null }
        };

        public bool HasPieceOn(TACPos pos) => Pieces.Contains(pos);
        public bool IsPartnerOn(TACPos pos) => Pieces[2] == pos;
        public void SetPlayerPosition(TACPos pos) => Pieces[0] = pos;

        public TACGameState Clone() => new TACGameState { Pieces = Pieces.ToArray(), PlayerSeat = PlayerSeat };
        public override string ToString() => string.Format("Player = {0}, Pieces = [{1}]", PlayerSeat, Pieces.Join(", "));

        public void RemoveEnemyPieceIfPresent(TACPos pos)
        {
            for (var i = 1; i < Pieces.Length; i += 2)
                if (Pieces[i] == pos)
                    Pieces[i] = null;
        }

        public void SwapPieces(int pIx1, int pIx2)
        {
            var tmp = Pieces[pIx1];
            Pieces[pIx1] = Pieces[pIx2];
            Pieces[pIx2] = tmp;
        }
    }
}
