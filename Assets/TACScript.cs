using System;
using System.Collections;
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
    public Material[] PawnColors, LEDColors, CardImages;

    public KMSelectable TacSel, LeftSel, RightSel;
    public KMSelectable[] CardSels, LEDSels;
    public TextMesh[] PlayerNames;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _moduleSolved, _cardMoving, _hasSwapped;
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
    private static readonly string[] _colorNames = new[] { "Red", "Yellow", "Green", "Blue" };

    private static readonly string[] _allNames = { "Sam", "Tom", "Zoe", "Adam", "Alex", "Andy", "Anna", "Bill", "Carl", "Fred", "Kate", "Lucy", "Ryan", "Toby", "Will", "Zach", "Chris", "Craig", "David", "Emily", "Felix", "Harry", "James", "Jenny", "Julia", "Kevin", "Molly", "Peter", "Sally", "Sarah", "Steve", "Susan" };
    private string[] _names;

    private readonly int[] colorsIxShuffle = new[] { 0, 1, 2, 3 };
    private int defuserColor;

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
        colorsIxShuffle.Shuffle();
        defuserColor = Random.Range(0, 4);

        for (int i = 0; i < LEDs.Length; i++)
        {
            LEDs[i].material = LEDColors[colorsIxShuffle[i]];
            PlayerNames[i].color = hexColors[colorsIxShuffle[i]];
            if (i == defuserColor)
                Pawn.material = PawnColors[colorsIxShuffle[i]];
        }
    #endregion

    #region Decide on cards in player’s hand
    tryAgain:
        _hand = Enumerable.Range(0, 5).Select(_ => cards[Random.Range(0, cards.Length)]).ToList();
        _state = TACGameState.FinalState(defuserColor, new TACPos(Random.Range(0, 32)));

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
        #endregion

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

        for (int i = 0; i < CardSels.Length; i++)
        {
            int j = i;
            CardSels[i].OnInteract += delegate ()
            {
                CardHandler(j);
                return false;
            };
        }
        TacSel.OnInteract += delegate ()
        {
            TACButtonHandler();
            return false;
        };
    }

    private IEnumerator AnimationLoop(float fromValue, float toValue, float duration, Action<float> action)
    {
        var rate = (toValue - fromValue) / duration;
        for (float i = fromValue; i <= toValue; i += Time.deltaTime * rate)
        {
            action(i);
            yield return null;
        }
        action(toValue);
    }

    private IEnumerator MoveCard(Transform obj)
    {
        _cardMoving = true;
        var duration = 1f;

        var startPosition = obj.localPosition;
        var endPosition = new Vector3(-0.06409124f, 0.0081f, 0.052f);
        var startRotation = obj.localRotation;
        var endRotation = Quaternion.Euler(-90, 0, 0);

        yield return AnimationLoop(0, 1, duration, i =>
        {
            var curve = i * (i - 1) * (i - 1);
            obj.transform.localRotation = Quaternion.Euler(-curve * 180, 0, 0) * Quaternion.Slerp(startRotation, endRotation, i);
            obj.transform.localPosition = Vector3.Lerp(startPosition, endPosition, -i * (i - 2)) + new Vector3(0, curve * .47f, 0);
        });

        _cardMoving = false;
    }

    private void CardHandler(int ix)
    {
        if (!_moduleSolved)
        {
            if (_mustSwapWith != null && !_hasSwapped)
            {
                Strike();
                return;
            }
            if (!_cardMoving)
            {
                StartCoroutine(MoveCard(CardObjects[ix].transform));
            }
        }
    }

    private void TACButtonHandler()
    {
        if (!_moduleSolved)
        {
            if (!_hasSwapped)
            {
                if (_mustSwapWith == null)
                {
                    Strike();
                    return;
                }
                InitiateSwap();
                _hasSwapped = true;
            }
        }
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
        j["colors"] = new JArray(colorsIxShuffle);
        j["playerseat"] = _state.PlayerSeat;
        j["positions"] = new JArray(_state.Pieces.Select(p => (int?)p).ToArray());
        return j.ToString(Formatting.None);
    }

    private void Strike()
    {
        if (!_moduleSolved)
            Module.HandleStrike();
        PawnObject.transform.localPosition = boardPositions[(int)_state.PlayerPosition];

        Choice1Button.transform.localPosition = choiceButtonsStart[0];
        Choice2Button.transform.localPosition = choiceButtonsStart[1];

        _hasSwapped = false;

        ResetCardPositions();
        ResetLEDColors();
    }

    private void ResetCardPositions()
    {
        CardObjects[0].transform.localPosition = new Vector3(-0.04702418f, 0.01012446f, -0.05868292f);
        CardObjects[1].transform.localPosition = new Vector3(-0.02397433f, 0.01051207f, -0.05868292f);
        CardObjects[2].transform.localPosition = new Vector3(-0.0003125817f, 0.01092333f, -0.05868292f);
        CardObjects[3].transform.localPosition = new Vector3(0.02334929f, 0.0113346f, -0.05868292f);
        CardObjects[4].transform.localPosition = new Vector3(0.04712323f, 0.01174586f, -0.05868292f);
        CardObjects[5].transform.localPosition = new Vector3(-0.06409124f, 0.0081f, 0.05196124f);
    }

    private void ResetLEDColors()
    {
        LEDColors[0].color = hexColors[0];
        LEDColors[1].color = hexColors[1];
        LEDColors[2].color = hexColors[2];
        LEDColors[3].color = hexColors[3];
    }

    private void InitiateSwap()
    {
        throw new NotImplementedException();
    }
}
