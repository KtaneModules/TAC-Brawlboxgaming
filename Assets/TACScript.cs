using System;
using System.Collections.Generic;
using System.Linq;
using Assets;
using KModkit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

using Random = UnityEngine.Random;

public class TACScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMRuleSeedable RuleSeedable;

    public MeshRenderer[] LEDs, Cards;
    public MeshRenderer Pawn;
    public GameObject PawnObject, Choice1Button, Choice2Button;
    public GameObject[] CardObjects;
    public Material[] PawnColours, LEDColours, CardImages;

    public KMSelectable TacSel, LeftSel, RightSel;
    public KMSelectable[] CardSels, LEDSels;
    public TextMesh[] PlayerNames;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _moduleSolved;
    private int? _mustSwapWith;
    private TACGameState _state;
    private List<TACCard> _hand = new List<TACCard>();
    private TACCard _swappableCard;

    private static readonly Color[] hexColors = new Color[] {
        new Color(1.0f, 0.15f, 0.15f),
        new Color(1.0f, 1.0f, 0.3f),
        new Color(0.3f, 1.0f, 0.2f),
        new Color(0.0f, 0.2f, 1.0f)
    };

    private static readonly TACCard[] cards = new TACCard[]
    {
        new TACCardNumber(1),
        new TACCardNumber(2),
        new TACCardNumber(3),
        new TACCardNumber(4, direction: -1),
        new TACCardNumber(5),
        new TACCardNumber(6),
        new TACCardSingleStep(7),
        new TACCardNumber(8, isDiscard:true),
        new TACCardNumber(9),
        new TACCardNumber(10),
        new TACCardNumber(12),
        new TACCardNumber(13),
        new TACCardTrickster(),
        new TACCardWarrior()
    };
    private static readonly string[] _colourNames = new[] { "Red", "Yellow", "Green", "Blue" };

    private static readonly string[] _allNames = { "Sam", "Tom", "Zoe", "Adam", "Alex", "Andy", "Anna", "Bill", "Carl", "Fred", "Kate", "Lucy", "Ryan", "Toby", "Will", "Zach", "Chris", "Craig", "David", "Emily", "Felix", "Harry", "James", "Jenny", "Julia", "Kevin", "Molly", "Peter", "Sally", "Sarah", "Steve", "Susan" };
    private string[] _names;

    private readonly int[] coloursIxShuffle = new[] { 0, 1, 2, 3 };
    private int defuserColour;

    private static readonly Vector3[] choiceButtonsStart = new[] {
        #region Vectors
        new Vector3(0.0f, 0.008941091f, -0.0003482774f),
        new Vector3(0.0f, 0.008941091f, -0.0003482774f)
        #endregion
    };
    private static readonly Vector3[] choiceButtonsEnd = new[] {
        #region Vectors
        new Vector3(-0.02545439f, 0.008941091f, -0.0003482774f),
        new Vector3(0.02545439f, 0.008941091f, -0.0003482774f)
        #endregion
    };
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
    private static readonly Vector3[] homes = new[] {
        #region Vectors
            new Vector3(-0.04433801f, 0.01470134f, 0.01088657f),
            new Vector3(0.04346199f, 0.01470134f, 0.01088657f),
            new Vector3(0.04346199f, 0.01470134f, -0.01111343f),
            new Vector3(-0.04433801f, 0.01470134f, -0.01111343f)
        #endregion
    };

    void Start()
    {
        _moduleId = _moduleIdCounter++;

        #region Rule seed
        var rnd = RuleSeedable.GetRNG();
        Debug.Log($"[TAC #{_moduleId}] Using rule seed: {rnd.Seed}.");
        _names = rnd.ShuffleFisherYates(_allNames.ToArray());
        #endregion

        #region Decide the player colors
        coloursIxShuffle.Shuffle();
        defuserColour = Random.Range(0, 4);

        for (int i = 0; i < LEDs.Length; i++)
        {
            LEDs[i].material = LEDColours[coloursIxShuffle[i]];
            PlayerNames[i].color = hexColors[coloursIxShuffle[i]];
            if (i == defuserColour)
                Pawn.material = PawnColours[coloursIxShuffle[i]];
        }
    #endregion

    #region Decide on cards in player’s hand
    tryAgain:
        _hand = Enumerable.Range(0, 5).Select(_ => cards[Random.Range(0, cards.Length)]).ToList();
        _state = TACGameState.FinalState(defuserColour, new TACPos(Random.Range(0, 32)));

        var numSwappableCards = _hand.Count;
        for (var cardIx = 0; cardIx < _hand.Count; cardIx++)
        {
        tryThisAgain:
            var possibleUndos = _hand[cardIx].Unexecute(_state).ToList();

            if (cardIx == _hand.Count - 1)
            {
                // Make sure to pick an unmove that will restore all enemies
                possibleUndos.RemoveAll(st => st.Pieces[1] == null || st.Pieces[3] == null);
            }

            if (possibleUndos.Count == 0 && cardIx >= numSwappableCards)
            {
                numSwappableCards--;
                var temp = _hand[cardIx];
                _hand[cardIx] = _hand[numSwappableCards];
                _hand[numSwappableCards] = temp;
                goto tryThisAgain;
            }
            else if (possibleUndos.Count == 0)
                goto tryAgain;

            var pickIx = Random.Range(0, possibleUndos.Count);
            _state = possibleUndos[pickIx];
        }

        _hand.Shuffle();
        #endregion

        if (Random.Range(0, 2) != 0)
        {
            var swapCardWith = cards[Random.Range(0, cards.Length)];
            for (var cardIx = 0; cardIx < 5; cardIx++)
            {
                _swappableCard = _hand[cardIx];
                _hand[cardIx] = swapCardWith;
                _mustSwapWith = cardIx;

                // Make sure that the hand after the swap is now unsolvable
                if (!isSolvable())
                    goto done;
            }

            _swappableCard = null;
            _mustSwapWith = null;
            goto tryAgain;
        }
    done:
        for (int i = 0; i < _hand.Count; i++)
        {
            Cards[i].material = CardImages.First(x => x.name == _hand[i].MaterialName);
        }
        if (_swappableCard != null)
        {
            Cards[5].material = CardImages.First(x => x.name == _swappableCard.MaterialName);
        }

        PawnObject.transform.localPosition = boardPositions[(int)_state.PlayerPosition];

        Choice1Button.transform.localPosition = choiceButtonsStart[0];
        Choice2Button.transform.localPosition = choiceButtonsStart[1];

        #region Calculate base pawn positions
        PlayerNames[_state.PlayerSeat].text = "You";

        var serialNumber = BombInfo.GetSerialNumber().Select(ch => ch >= '0' && ch <= '9' ? ch - '0' : ch - 'A' + 1).ToArray();

        var enemy1 = new TACPos(serialNumber[0] + serialNumber[5]);
        PlayerNames[(_state.PlayerSeat + 1) % 4].text = _names[_state.Pieces[1].Value - enemy1];

        var partner = new TACPos(serialNumber[1] + serialNumber[4]);
        while (enemy1 == partner)
            partner++;
        PlayerNames[(_state.PlayerSeat + 2) % 4].text = _names[_state.Pieces[2].Value - partner];

        var enemy2 = new TACPos(serialNumber[2] + serialNumber[3]);
        while (enemy2 == partner || enemy2 == enemy1)
            enemy2++;
        PlayerNames[(_state.PlayerSeat + 3) % 4].text = _names[_state.Pieces[3].Value - enemy2];
        #endregion

        Debug.Log($"[TAC #{_moduleId}] Player names (clockwise from You): {Enumerable.Range(1, 3).Select(ix => PlayerNames[(_state.PlayerSeat + ix) % 4].text).Join(", ")}");
        Debug.Log($"[TAC #{_moduleId}] {JsonForLogging()}");
        Debug.Log($"[TAC #{_moduleId}] Initial hand: {_hand.Join(", ")}");
        Debug.Log($"[TAC #{_moduleId}] {(_mustSwapWith != null ? "You must swap a card." : "You must not swap a card.")}");
    }

    private bool isSolvable()
    {
        return solve(_state, _hand.ToArray()).Any();
    }

    private IEnumerable<TACGameState> solve(TACGameState state, TACCard[] hand)
    {
        if (hand.Length == 0)
        {
            if (state.PlayerInHome)
                yield return state;
            yield break;
        }
        for (var i = 0; i < hand.Length; i++)
            foreach (var newState in hand[i].Execute(state))
                foreach (var result in solve(newState, remove(hand, i)))
                    yield return result;
    }

    private static T[] remove<T>(T[] array, int ix)
    {
        var newArray = new T[array.Length - 1];
        Array.Copy(array, 0, newArray, 0, ix);
        Array.Copy(array, ix + 1, newArray, ix, newArray.Length - ix);
        return newArray;
    }

    private string JsonForLogging(bool isStrike = false)
    {
        var j = new JObject();
        j["colors"] = new JArray(coloursIxShuffle);
        j["playerseat"] = _state.PlayerSeat;
        j["positions"] = new JArray(_state.Pieces.Select(p => (int?)p).ToArray());
        return j.ToString(Formatting.None);
    }
}
