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

    public MeshRenderer[] LEDs, Cards, ChoiceButtons;
    public MeshRenderer Pawn;
    public GameObject PawnObject;
    public GameObject[] CardObjects, ChoiceButtonObjects;
    public Material[] PawnColors, LEDColors, CardImages, ButtonImagesMove;
    public Material ButtonImageDiscard, ButtonImageEnterHome, ButtonImagePassHome, ButtonImagePassHomeBackwards;

    public KMSelectable TacSel, LeftSel, RightSel;
    public KMSelectable[] CardSels, LEDSels;
    public TextMesh[] PlayerNames;

    private static int _moduleIdCounter = 1;
    private static int _cardsPlayedCounter = 0;
    private int _moduleId;
    private bool _moduleSolved, _cardMoving, _pieceMoving, _gameResetting, _buttonsMoving, _hasSwapped;
    private int? _mustSwapWith;
    private TACGameState _state;
    private List<TACCard> _initialHand = new List<TACCard>();
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

    private static readonly Vector3 choiceButtonsStart = new Vector3(0.0f, 0.008941091f, -0.0003482774f);
    private static readonly Vector3[] choiceButtonsEnd = new[] {
        #region Vectors
        new Vector3(-0.02545439f, 0.008941091f, -0.0003482774f),
        new Vector3(0.02545439f, 0.008941091f, -0.0003482774f)
        #endregion
    };
    private static readonly Vector3[] ledStartPositions = new[] {
        #region Vectors
        new Vector3(0.04343889f, 0.009911899f, 0.03191717f),
        new Vector3(0.0433994f, 0.009912316f, -0.03234167f),
        new Vector3(-0.04423796f, 0.009912226f, -0.03234166f),
        new Vector3(-0.04435579f, 0.009911899f, 0.03191718f)
        #endregion
    };
    private static readonly Vector3[] cardPositions = new[] {
        #region Vectors
        new Vector3(-0.04702418f, 0.01012446f, -0.05868292f),
        new Vector3(-0.02397433f, 0.01051207f, -0.05868292f),
        new Vector3(-0.0003125817f, 0.01092333f, -0.05868292f),
        new Vector3(0.02334929f, 0.0113346f, -0.05868292f),
        new Vector3(0.04712323f, 0.01174586f, -0.05868292f),
        new Vector3(-0.06409124f, 0.0081f, 0.05196124f)
        #endregion
    };
    private static readonly Quaternion[] cardRotations = new[] {
        #region Quarternions
        Quaternion.Euler(-90, 0, 0),
        Quaternion.Euler(-90, 0, 0),
        Quaternion.Euler(-90, 0, 0),
        Quaternion.Euler(-90, 0, 0),
        Quaternion.Euler(-90, 0, 0),
        Quaternion.Euler(90, 0, 180)
        #endregion
    };

    private bool _tacButtonHeld = true;

    private int? currentCardChoice = null;

    private Dictionary<TACCardOption, bool> currentOptions = null;

    private TACCardOption currentChoice = TACCardOption.None;

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
            var cardOrder = Enumerable.Range(0, 5).ToArray().Shuffle();
            foreach (var cardIx in cardOrder)
            {
                _swappableCard = _hand[cardIx];
                _hand[cardIx] = swapCardWith;
                _mustSwapWith = cardIx;

                // Make sure that the hand after the swap is now unsolvable
                if (!isSolvable())
                    goto done;

                _hand[cardIx] = _swappableCard;
            }

            _swappableCard = null;
            _mustSwapWith = null;
            goto tryAgain;
        }
    done:
        #endregion

        _initialHand = _hand.ToList();

        for (int i = 0; i < _hand.Count; i++)
        {
            Cards[i].sharedMaterial = CardImages.First(x => x.name == _hand[i].MaterialName);
        }
        if (_swappableCard != null)
        {
            Cards[5].sharedMaterial = CardImages.First(x => x.name == _swappableCard.MaterialName);
        }

        PawnObject.transform.localPosition = boardPositions[(int)_state.PlayerPosition];

        ChoiceButtons[0].transform.localPosition = choiceButtonsStart;
        ChoiceButtons[1].transform.localPosition = choiceButtonsStart;

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

        Coroutine tacButtonCoroutine = null;

        TacSel.OnInteract += delegate ()
        {
            if (!_gameResetting)
            {
                _tacButtonHeld = true;
                tacButtonCoroutine = StartCoroutine(CheckTACButtonReleased());
            }
            return false;
        };

        TacSel.OnInteractEnded += delegate ()
        {
            if (!_gameResetting)
            {
                if (_tacButtonHeld)
                {
                    StopCoroutine(tacButtonCoroutine);
                    TACButtonHandler();
                }
            }
            _tacButtonHeld = false;
        };

        LeftSel.OnInteract += delegate ()
        {
            currentOptions[currentChoice] = false;
            StartCoroutine(HandleCard());
            return false;
        };

        RightSel.OnInteract += delegate ()
        {
            currentOptions[currentChoice] = true;
            StartCoroutine(HandleCard());
            return false;
        };
    }

    private IEnumerator CheckTACButtonReleased()
    {
        yield return new WaitForSeconds(1);
        if (_tacButtonHeld)
        {
            StartCoroutine(ResetState());
        }
        _tacButtonHeld = false;
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

    private IEnumerator PlayCard(Transform obj)
    {
        _cardMoving = true;
        yield return MoveObjectSmooth(obj, new Vector3(-0.06409124f, 0.0081f + (_cardsPlayedCounter * 0.0004f), 0.052f), Quaternion.Euler(-90, 0, 0), 1f);
        _cardsPlayedCounter++;
        _cardMoving = false;
        currentOptions = null;
        currentCardChoice = null;
    }

    private IEnumerator MoveObjectSmooth(Transform obj, Vector3 endPosition, Quaternion endRotation, float duration, float height = .47f, bool extraRotation = true)
    {
        return MoveObjectsSmooth(new[] { obj }, new[] { endPosition }, new[] { endRotation }, duration, new[] { height }, extraRotation);
    }

    private IEnumerator MoveObjectsSmooth(Transform[] objs, Vector3[] endPositions, Quaternion[] endRotations, float duration, float[] heights, bool extraRotation = true)
    {
        if (objs.Length != endPositions.Length ||
            objs.Length != endRotations.Length ||
            objs.Length != heights.Length)
        {
            throw new Exception("Array lengths of arguments do not match.");
        }

        var startPositions = objs.Select(x => x.localPosition).ToArray();
        var startRotations = objs.Select(x => x.localRotation).ToArray();

        yield return AnimationLoop(0, 1, duration, j =>
        {
            for (int i = 0; i < objs.Length; i++)
            {
                var curve = j * (j - 1) * (j - 1);
                Quaternion extraRotationOffset = extraRotation ? Quaternion.Euler(-curve * 180, 0, 0) : Quaternion.Euler(0, 0, 0);
                objs[i].transform.localRotation = Quaternion.Slerp(startRotations[i], endRotations[i], Easing.OutQuad(j, 0, 1, 1));
                objs[i].transform.localPosition = Vector3.Lerp(startPositions[i], endPositions[i], -j * (j - 2)) + new Vector3(0, curve * heights[i], 0);
            }
        });
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
            if (!_cardMoving && !_gameResetting && !_pieceMoving && !_buttonsMoving)
            {
                if (currentCardChoice != null) return;
                currentCardChoice = ix;
                currentOptions = new Dictionary<TACCardOption, bool>();
                StartCoroutine(HandleCard());
            }
        }
    }

    private void TACButtonHandler()
    {
        if (!_moduleSolved && !_hasSwapped)
        {
            if (_mustSwapWith == null)
            {
                if (_cardsPlayedCounter == 0)
                {
                    Strike();
                }
                return;
            }
            StartCoroutine(InitiateSwap());
            _hasSwapped = true;
        }
    }

    private IEnumerator MoveButtons()
    {
        _buttonsMoving = true;
        yield return MoveObjectsSmooth(
            new[] { ChoiceButtons[0].transform, ChoiceButtons[1].transform },
            new[] { choiceButtonsEnd[0], choiceButtonsEnd[1] },
            new[] { Quaternion.Euler(-90, 0, 180), Quaternion.Euler(-90, 0, 180) },
            1f,
            new[] { 0f, 0f },
            false
            );
        _buttonsMoving = false;
    }

    private IEnumerator HandleCard()
    {
        if (currentOptions != null && currentOptions.Count > 0) yield return ResetButtonPositions();
        var option = _hand[currentCardChoice.Value].GetOption(_state, currentOptions);
        if (option == TACCardOption.None) yield return PlayCard(CardObjects[currentCardChoice.Value].transform);
        else
        {
            currentChoice = option;
            if (option == TACCardOption.Discard)
            {
                ChoiceButtons[0].sharedMaterial = ButtonImagesMove[0]; // To-do: ruleseed
                ChoiceButtons[1].sharedMaterial = ButtonImageDiscard;
                yield return MoveButtons();
            }
            else if (option == TACCardOption.EnterHome)
            {
                ChoiceButtons[0].sharedMaterial = ButtonImagePassHome;
                ChoiceButtons[1].sharedMaterial = ButtonImageEnterHome;
                yield return MoveButtons();
            }
            else if (option == TACCardOption.EnterHomeBackwards)
            {
                ChoiceButtons[0].sharedMaterial = ButtonImagePassHomeBackwards;
                ChoiceButtons[1].sharedMaterial = ButtonImageEnterHome;
                yield return MoveButtons();
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

        _hasSwapped = false;

        StartCoroutine(ResetState());
    }

    private IEnumerator ResetState()
    {
        _gameResetting = true;

        yield return ResetButtonPositions();
        yield return ResetLEDColors();
        yield return ResetCardPositions();

        if (_hasSwapped)
        {
            yield return MoveObjectsSmooth(
            new[] { CardObjects[5].transform, CardObjects[(int)_mustSwapWith].transform },
            new[] { cardPositions[(int)_mustSwapWith], cardPositions[5] },
            new[] { cardRotations[(int)_mustSwapWith], cardRotations[5] },
            1f,
            new[] { .23f, .5f }
            );

            Cards[5].sharedMaterial = CardImages.First(x => x.name == _swappableCard.MaterialName);
            Cards[(int)_mustSwapWith].sharedMaterial = CardImages.First(x => x.name == _initialHand[(int)_mustSwapWith].MaterialName);

            CardObjects[5].transform.localPosition = cardPositions[5];
            CardObjects[(int)_mustSwapWith].transform.localPosition = cardPositions[(int)_mustSwapWith];
            CardObjects[5].transform.localRotation = cardRotations[5];
            CardObjects[(int)_mustSwapWith].transform.localRotation = cardRotations[(int)_mustSwapWith];

            _hasSwapped = false;
        }

        _hand = _initialHand.ToList();

        _cardsPlayedCounter = 0;

        _gameResetting = false;
        currentOptions = null;
        currentCardChoice = null;
    }

    private IEnumerator ResetButtonPositions()
    {
        yield return StartCoroutine(MoveObjectsSmooth(
            new[] { ChoiceButtons[0].transform, ChoiceButtons[1].transform },
            new[] { choiceButtonsStart, choiceButtonsStart },
            new[] { Quaternion.Euler(-90, 0, 180), Quaternion.Euler(-90, 0, 180) },
            1f,
            new[] { 0f, 0f },
            false
            ));
    }

    private IEnumerator ResetCardPositions()
    {
        for (int i = 0; i < CardObjects.Length; i++)
        {
            if (CardObjects[i].transform.localPosition != cardPositions[i])
            {
                StartCoroutine(MoveObjectSmooth(CardObjects[i].transform, cardPositions[i], cardRotations[i], 1f));
            }
        }
        yield return CardObjects;
    }

    private IEnumerator ResetLEDColors()
    {
        LEDColors[0].color = hexColors[0];
        LEDColors[1].color = hexColors[1];
        LEDColors[2].color = hexColors[2];
        LEDColors[3].color = hexColors[3];
        yield return LEDColors;
    }

    private IEnumerator InitiateSwap()
    {
        yield return MoveObjectsSmooth(
            new[] { CardObjects[5].transform, CardObjects[(int)_mustSwapWith].transform },
            new[] { cardPositions[(int)_mustSwapWith], cardPositions[5] },
            new[] { cardRotations[(int)_mustSwapWith], cardRotations[5] },
            1f,
            new[] { .23f, .5f }
            );

        Cards[5].sharedMaterial = CardImages.First(x => x.name == _hand[(int)_mustSwapWith].MaterialName);
        Cards[(int)_mustSwapWith].sharedMaterial = CardImages.First(x => x.name == _swappableCard.MaterialName);

        CardObjects[5].transform.localPosition = cardPositions[5];
        CardObjects[(int)_mustSwapWith].transform.localPosition = cardPositions[(int)_mustSwapWith];
        CardObjects[5].transform.localRotation = cardRotations[5];
        CardObjects[(int)_mustSwapWith].transform.localRotation = cardRotations[(int)_mustSwapWith];

        _hand[(int)_mustSwapWith] = _swappableCard;
    }
}