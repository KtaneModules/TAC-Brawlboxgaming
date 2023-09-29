using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets
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
        public abstract IEnumerable<TACGameState> Execute(TACGameState state);
        public abstract IEnumerable<TACGameState> Unexecute(TACGameState state);
        public abstract TACCardOption GetOption(TACGameState state, Dictionary<TACCardOption, bool> currentOptions);

        public abstract string MaterialName
        { get; }
        public bool IsPlayed { get; set; }
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

        public override string MaterialName => $"{Number}{(Direction == -1 ? "back" : "")}{(IsDiscard ? "discard" : "")}";

        public override IEnumerable<TACGameState> Execute(TACGameState state)
        {
            if (IsDiscard)
                yield return state;

            for (var i = 1; i < Number; i++)
                if (state.HasPieceOn(state.PlayerPosition + i * Direction))
                    yield break;
            var newState = state.Clone();
            var newPosition = state.PlayerPosition + Number * Direction;

            // Option to move into home
            if (newPosition == TACPos.GetStart(state.PlayerSeat) + Direction)
            {
                newState = newState.Clone();
                newState.SetPlayerPosition(TACPos.Home);
                yield return newState;
            }

            if (!state.IsPartnerOn(state.PlayerPosition + Number * Direction))
            {
                // Option to move past home
                newState.RemoveEnemyPieceIfPresent(newPosition);
                newState.SetPlayerPosition(newPosition);
                yield return newState;
            }
        }

        public override IEnumerable<TACGameState> Unexecute(TACGameState state)
        {
            if (IsDiscard)
                yield return state;

            if (state.PlayerInHome)
            {
                var newState = state.Clone();
                newState.SetPlayerPosition(TACPos.GetStart(state.PlayerSeat) - (Number - 1) * Direction);
                yield return newState;
                yield break;
            }

            // Can we move backwards this many steps?
            for (var i = 1; i <= Number; i++)
                if (state.HasPieceOn(state.PlayerPosition - i * Direction))
                    yield break;

            var stateNoCapture = state.Clone();
            stateNoCapture.SetPlayerPosition(state.PlayerPosition - Number * Direction);
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

        public override string ToString()
        {
            return string.Format("{0}{1}{2}", Number, IsDiscard ? "◊" : "", Direction < 0 ? "⏪" : "");
        }
    }

    class TACCardSingleStep : TACCard
    {
        public int Number { get; private set; }
        public TACCardSingleStep(int number) { Number = number; }
        public override string MaterialName => $"{Number}single";

        public override IEnumerable<TACGameState> Execute(TACGameState state)
        {
            var newState = state.Clone();
            for (var i = 1; i < Number; i++)
            {
                if (newState.PartnerPosition == state.PlayerPosition + i)
                    yield break;
                newState.RemoveEnemyPieceIfPresent(state.PlayerPosition + i);
            }
            var newPosition = state.PlayerPosition + Number;

            // Option to move into home
            if (newPosition == TACPos.GetStart(state.PlayerSeat) + 1)
            {
                newState = newState.Clone();
                newState.SetPlayerPosition(TACPos.Home);
                yield return newState;
            }

            newState.RemoveEnemyPieceIfPresent(newPosition);
            newState.SetPlayerPosition(newPosition);
            yield return newState;
        }

        public override IEnumerable<TACGameState> Unexecute(TACGameState state)
        {
            for (var i = 1; i <= Number; i++)
                if (state.HasPieceOn(state.PlayerPosition - i))
                    yield break;

            var s1 = state.Clone();
            s1.SetPlayerPosition(state.PlayerPosition - Number);
            yield return s1;
            if (state.Pieces[1] == null)
            {
                for (var j = 0; j < Number; j++)
                {
                    var s2 = s1.Clone();
                    s2.Pieces[1] = state.PlayerPosition - j;
                    yield return s2;
                }
            }
            if (state.Pieces[3] == null)
            {
                for (var j = 0; j < Number; j++)
                {
                    var s2 = s1.Clone();
                    s2.Pieces[3] = state.PlayerPosition - j;
                    yield return s2;

                    for (var k = 0; k < Number; k++)
                        if (k != j)
                        {
                            var s3 = s2.Clone();
                            s3.Pieces[1] = state.PlayerPosition - k;
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

        public override string ToString()
        {
            return string.Format("{0}∴", Number);
        }
    }

    class TACCardTrickster : TACCard
    {
        public override string MaterialName => "Trickster";
        public override IEnumerable<TACGameState> Execute(TACGameState state)
        {
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

        public override IEnumerable<TACGameState> Unexecute(TACGameState state)
        {
            return state.PlayerInHome ? Enumerable.Empty<TACGameState>() : Execute(state);
        }

        public override TACCardOption GetOption(TACGameState state, Dictionary<TACCardOption, bool> currentOptions)
        {
            return !currentOptions.ContainsKey(TACCardOption.Swap) ? TACCardOption.Swap : TACCardOption.None;
        }

        public override string ToString()
        {
            return "Trickster";
        }
    }

    class TACCardWarrior : TACCard
    {
        public override string MaterialName => "Warrior";
        public override IEnumerable<TACGameState> Execute(TACGameState state)
        {
            var destination = state.PlayerPosition + 1;
            while (!state.HasPieceOn(destination))
                destination++;

            if (state.PartnerPosition == destination)
                yield break;

            var newState = state.Clone();
            newState.RemoveEnemyPieceIfPresent(destination);
            newState.SetPlayerPosition(destination);
            yield return newState;
        }

        public override IEnumerable<TACGameState> Unexecute(TACGameState state)
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

        public override TACCardOption GetOption(TACGameState state, Dictionary<TACCardOption, bool> currentOptions)
        {
            return TACCardOption.None;
        }

        public override string ToString()
        {
            return "Warrior";
        }
    }
}