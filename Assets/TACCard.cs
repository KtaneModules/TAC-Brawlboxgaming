using System;
using System.Collections.Generic;
using System.Linq;

namespace TAC
{
    enum TACCardOption
    {
        None,
        Discard,
        EnterHome,
        EnterHomeBackwards,
        Swap
    }

    abstract class TACCard
    {
        public abstract TACCardExecuteResult Execute(TACGameState state, Dictionary<TACCardOption, bool> currentOptions, int swap1, int swap2);
        public abstract IEnumerable<TACGameState> ExecuteAll(TACGameState state);
        public abstract IEnumerable<TACGameState> UnexecuteAll(TACGameState state);
        public abstract TACCardOption GetOption(TACGameState state, Dictionary<TACCardOption, bool> currentOptions);
        public abstract TACPawnMove MoveType { get; }
        public abstract int ButtonMoveImageIndex { get; }
        public abstract string MaterialName { get; }
    }

    class TACCardNumber : TACCard
    {
        public int Number { get; private set; }
        public int Direction { get; private set; }  // Either +1 or -1
        public bool IsDiscard { get; private set; }
        public TACCardNumber(int number, int direction = 1, bool isDiscard = false)
        {
            Number = number;
            Direction = direction;
            IsDiscard = isDiscard;
        }

        public override int ButtonMoveImageIndex
        {
            get
            {
                if (Number < 8 || Number > 10)
                    throw new NotImplementedException($"Discard cards with number {Number} not implemented.");
                return Number - 8;
            }
        }

        public override string MaterialName => $"{Number}{(Direction == -1 ? "back" : "")}{(IsDiscard ? "discard" : "")}";

        public override TACCardExecuteResult Execute(TACGameState state, Dictionary<TACCardOption, bool> currentOptions, int swap1, int swap2)
        {
            bool opt;

            // Option to use the card as a discard
            if (currentOptions.TryGetValue(TACCardOption.Discard, out opt) && opt)
                return state;

            // Strike if this would move across another piece
            for (var i = 1; i < Number; i++)
                if (state.HasPieceOn(state.PlayerPosition + i * Direction))
                    return $"Moving {Number} would move across another piece.";

            var newState = state.Clone();

            // Option to move into home
            if (currentOptions.TryGetValue(Direction < 0 ? TACCardOption.EnterHomeBackwards : TACCardOption.EnterHome, out opt) && opt)
            {
                newState.SetPlayerPosition(TACPos.Home);
                return newState;
            }

            // Strike if this would capture your partner
            if (state.IsPartnerOn(state.PlayerPosition + Number * Direction))
                return $"Moving {Number} would capture your partner.";

            // Move normally (incl. move past home)
            var newPosition = state.PlayerPosition + Number * Direction;
            newState.RemoveEnemyPieceIfPresent(newPosition);
            newState.SetPlayerPosition(newPosition);
            return newState;
        }

        public override IEnumerable<TACGameState> ExecuteAll(TACGameState state)
        {
            // Option to use the card as a discard
            yield return state;

            // Stop if this would move across another piece
            for (var i = 1; i < Number; i++)
                if (state.HasPieceOn(state.PlayerPosition + i * Direction))
                    yield break;

            var newState = state.Clone();

            // Option to move into home
            newState.SetPlayerPosition(TACPos.Home);
            yield return newState;

            // Stop if this would capture your partner
            if (state.IsPartnerOn(state.PlayerPosition + Number * Direction))
                yield break;

            // Move normally (incl. move past home)
            newState = state.Clone();
            var newPosition = state.PlayerPosition + Number * Direction;
            newState.RemoveEnemyPieceIfPresent(newPosition);
            newState.SetPlayerPosition(newPosition);
            yield return newState;
        }

        public override IEnumerable<TACGameState> UnexecuteAll(TACGameState state)
        {
            if (IsDiscard)
                yield return state;

            // Can we move backwards this many steps?
            var checkPos = state.PlayerInHome ? TACPos.GetStart(state.PlayerSeat) : state.PlayerPosition - Direction;
            for (var i = 1; i <= Number; i++, checkPos -= Direction)
                if (state.HasPieceOn(checkPos))
                    yield break;
            checkPos += Direction;

            if (state.PlayerInHome)
            {
                var newState = state.Clone();
                newState.SetPlayerPosition(checkPos);
                yield return newState;
                yield break;
            }

            var stateNoCapture = state.Clone();
            stateNoCapture.SetPlayerPosition(checkPos);
            yield return stateNoCapture;

            for (var i = 1; i <= 3; i += 2)
                if (state.Pieces[i] == null)
                {
                    var stateCapture = stateNoCapture.Clone();
                    stateCapture.Pieces[i] = state.PlayerPosition;
                    yield return stateCapture;
                }
        }

        public override TACCardOption GetOption(TACGameState state, Dictionary<TACCardOption, bool> currentOptions)
        {
            if (IsDiscard && !currentOptions.ContainsKey(TACCardOption.Discard)) return TACCardOption.Discard;
            else if (IsDiscard && currentOptions[TACCardOption.Discard]) return TACCardOption.None;
            else
            {
                var enterHomeOption = Direction < 0 ? TACCardOption.EnterHomeBackwards : TACCardOption.EnterHome;
                if (!currentOptions.ContainsKey(enterHomeOption))
                {
                    var newPosition = state.PlayerPosition + Number * Direction;
                    var canEnterHome = newPosition == TACPos.GetStart(state.PlayerSeat) + Direction;
                    if (canEnterHome) return enterHomeOption;
                }
                return TACCardOption.None;
            }
        }

        public override TACPawnMove MoveType => Direction < 0 ? TACPawnMove.Backwards : TACPawnMove.Forwards;
        public override string ToString() => string.Format("{0}{1}{2}", Number, IsDiscard ? "◊" : "", Direction < 0 ? "⏪" : "");
    }

    class TACCardSingleStep : TACCard
    {
        public int Number { get; private set; }
        public TACCardSingleStep(int number) { Number = number; }
        public override string MaterialName => $"{Number}single";

        public override TACCardExecuteResult Execute(TACGameState state, Dictionary<TACCardOption, bool> currentOptions, int swap1, int swap2)
        {
            bool opt;
            var newState = state.Clone();

            for (var i = 1; i < Number; i++)
            {
                // Strike if this would capture your partner
                if (state.PartnerPosition == state.PlayerPosition + i)
                    return $"Moving {Number} single steps would capture your partner.";

                // Bulldoze enemy pieces
                newState.RemoveEnemyPieceIfPresent(state.PlayerPosition + i);
            }

            // Option to move into home
            if (currentOptions.TryGetValue(TACCardOption.EnterHome, out opt) && opt)
            {
                newState.SetPlayerPosition(TACPos.Home);
                return newState;
            }

            // Strike if this would capture the partner
            if (state.IsPartnerOn(state.PlayerPosition + Number))
                return $"Moving {Number} would capture your partner.";

            // Move normally (incl. move past home)
            var newPosition = state.PlayerPosition + Number;
            newState.RemoveEnemyPieceIfPresent(newPosition);
            newState.SetPlayerPosition(newPosition);
            return newState;
        }

        public override IEnumerable<TACGameState> ExecuteAll(TACGameState state)
        {
            var newState = state.Clone();

            for (var i = 1; i < Number; i++)
            {
                // Stop if this would capture the partner
                if (state.PartnerPosition == state.PlayerPosition + i)
                    yield break;

                // Bulldoze enemy pieces
                newState.RemoveEnemyPieceIfPresent(state.PlayerPosition + i);
            }

            // Option to move into home
            newState.SetPlayerPosition(TACPos.Home);
            yield return newState;

            // Stop if this would capture the partner
            if (state.IsPartnerOn(state.PlayerPosition + Number))
                yield break;

            // Move normally (incl. move past home)
            newState = newState.Clone();
            var newPosition = state.PlayerPosition + Number;
            newState.RemoveEnemyPieceIfPresent(newPosition);
            newState.SetPlayerPosition(newPosition);
            yield return newState;
        }

        public override IEnumerable<TACGameState> UnexecuteAll(TACGameState state)
        {
            var checkPos = state.PlayerInHome ? TACPos.GetStart(state.PlayerSeat) : state.PlayerPosition - 1;
            for (var i = 1; i <= Number; i++, checkPos--)
                if (state.HasPieceOn(checkPos))
                    yield break;
            checkPos++;

            var s1 = state.Clone();
            s1.SetPlayerPosition(checkPos);
            yield return s1;
            var upTo = state.PlayerInHome ? Number - 1 : Number;
            if (state.Pieces[1] == null)
            {
                for (var j = 1; j <= upTo; j++)
                {
                    var s2 = s1.Clone();
                    s2.Pieces[1] = checkPos + j;
                    yield return s2;
                }
            }
            if (state.Pieces[3] == null)
            {
                for (var j = 1; j <= upTo; j++)
                {
                    var s2 = s1.Clone();
                    s2.Pieces[3] = checkPos + j;
                    yield return s2;

                    for (var k = 1; k <= upTo; k++)
                        if (k != j)
                        {
                            var s3 = s2.Clone();
                            s3.Pieces[1] = checkPos + k;
                            yield return s3;
                        }
                }
            }
        }

        public override TACCardOption GetOption(TACGameState state, Dictionary<TACCardOption, bool> currentOptions)
        {
            if (!currentOptions.ContainsKey(TACCardOption.EnterHome))
            {
                var newPosition = state.PlayerPosition + Number;
                var canEnterHome = newPosition == TACPos.GetStart(state.PlayerSeat) + 1;
                if (canEnterHome) return TACCardOption.EnterHome;
            }
            return TACCardOption.None;
        }

        public override int ButtonMoveImageIndex { get { throw new NotImplementedException("Single-step cards cannot have the discard option."); } }
        public override TACPawnMove MoveType => TACPawnMove.Forwards;
        public override string ToString() => string.Format("{0}∴", Number);
    }

    class TACCardTrickster : TACCard
    {
        public override string MaterialName => "Trickster";

        public override TACCardExecuteResult Execute(TACGameState state, Dictionary<TACCardOption, bool> currentOptions, int seat1, int seat2)
        {
            var ix1 = (seat1 + 4 - state.PlayerSeat) % 4;
            var ix2 = (seat2 + 4 - state.PlayerSeat) % 4;

            // Strike if the user tries to swap pieces that have already been captured
            if (state.Pieces[ix1] == null || state.Pieces[ix2] == null)
                return "You tried to swap a piece that has already been captured.";

            // Swap pieces
            var newState = state.Clone();
            var t = newState.Pieces[ix1].Value;
            newState.Pieces[ix1] = newState.Pieces[ix2].Value;
            newState.Pieces[ix2] = t;
            return newState;
        }

        public override IEnumerable<TACGameState> ExecuteAll(TACGameState state) => UnexecuteAll(state);

        public override IEnumerable<TACGameState> UnexecuteAll(TACGameState state)
        {
            if (state.PlayerInHome)
                yield break;

            for (var p1 = 0; p1 < state.Pieces.Length; p1++)
                if (state.Pieces[p1] != null)
                    for (var p2 = p1 + 1; p2 < state.Pieces.Length; p2++)
                        if (state.Pieces[p2] != null)
                        {
                            var newState = state.Clone();
                            newState.SwapPieces(p1, p2);
                            yield return newState;
                        }
        }

        public override TACCardOption GetOption(TACGameState state, Dictionary<TACCardOption, bool> currentOptions) =>
            !currentOptions.ContainsKey(TACCardOption.Swap) ? TACCardOption.Swap : TACCardOption.None;

        public override int ButtonMoveImageIndex { get { throw new NotImplementedException("Trickster cards cannot have the discard option."); } }
        public override TACPawnMove MoveType => TACPawnMove.Teleport;
        public override string ToString() => "Trickster";
    }

    class TACCardWarrior : TACCard
    {
        public override string MaterialName => "Warrior";
        public override TACCardExecuteResult Execute(TACGameState state, Dictionary<TACCardOption, bool> currentOptions, int swap1, int swap2)
        {
            var destination = state.PlayerPosition + 1;
            while (!state.HasPieceOn(destination))
                destination++;

            // Strike if this would capture the partner
            if (state.PartnerPosition == destination)
                return "Using the Warrior would capture your partner.";

            var newState = state.Clone();
            newState.RemoveEnemyPieceIfPresent(destination);
            newState.SetPlayerPosition(destination);
            return newState;
        }

        public override IEnumerable<TACGameState> ExecuteAll(TACGameState state)
        {
            var result = Execute(state, new Dictionary<TACCardOption, bool>(), 0, 0);
            return result is TACCardExecuteStrike ? Enumerable.Empty<TACGameState>() : new[] { ((TACCardExecuteSuccess) result).State };
        }

        public override IEnumerable<TACGameState> UnexecuteAll(TACGameState state)
        {
            if (state.PlayerInHome)
                yield break;

            // We can’t un-capture an enemy if there are already two on the board
            if (state.Pieces[1] != null && state.Pieces[3] != null)
                yield break;

            var destination = state.PlayerPosition - 1;
            while (!state.HasPieceOn(destination))
            {
                for (var i = 1; i <= 3; i += 2)
                    if (state.Pieces[i] == null)
                    {
                        var newState = state.Clone();
                        newState.SetPlayerPosition(destination);
                        newState.Pieces[i] = state.PlayerPosition;
                        yield return newState;
                    }
                destination--;
            }
        }

        public override int ButtonMoveImageIndex { get { throw new NotImplementedException("Warrior cards cannot have the discard option."); } }
        public override TACCardOption GetOption(TACGameState state, Dictionary<TACCardOption, bool> currentOptions) => TACCardOption.None;
        public override TACPawnMove MoveType => TACPawnMove.Forwards;
        public override string ToString() => "Warrior";
    }
}