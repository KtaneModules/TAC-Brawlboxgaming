using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KModkit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TAC;
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
    public GameObject[] CardObjects, ChoiceButtonObjects, LEDObjects;
    public Material[] PawnColors, LEDColors, CardImages, ButtonImagesMove;
    public Material ButtonImageDiscard, ButtonImageEnterHome, ButtonImagePassHome, ButtonImagePassHomeBackwards;
    public Color[] LedOnColors;
    public Color[] LedOffColors;

    public KMSelectable TacSel, LeftSel, RightSel;
    public KMSelectable[] CardSels, LEDSels;
    public TextMesh[] PlayerNames;

    private static int _moduleIdCounter = 1;
    private static int _cardsPlayedCounter = 0;
    private int _moduleId, _swapPieceWith2;
    private bool _moduleSolved, _cardMoving, _gameResetting, _buttonsMoving, _hasSwapped, _canSwapPieces;
    private int? _mustSwapWith, _swapPieceWith1;
    private TACGameState _state, _initialState;
    private List<TACCard> _initialHand = new List<TACCard>();
    private List<TACCard> _hand = new List<TACCard>();
    private TACCard _swappableCard;

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
        #region Quaternions
        Quaternion.Euler(-90, 0, 0),
        Quaternion.Euler(-90, 0, 0),
        Quaternion.Euler(-90, 0, 0),
        Quaternion.Euler(-90, 0, 0),
        Quaternion.Euler(-90, 0, 0),
        Quaternion.Euler(90, 0, 180)
        #endregion
    };
    private static Vector3[] ledOrigPositions;

    private bool _tacButtonHeld = true;

    private int? currentCardChoice = null;

    private Dictionary<TACCardOption, bool> currentOptions = null;

    private TACCardOption currentChoice = TACCardOption.None;

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        ledOrigPositions = LEDObjects.Select(obj => obj.transform.localPosition).ToArray();

        #region Rule seed
        var rnd = RuleSeedable.GetRNG();
        Debug.Log($"[TAC #{_moduleId}] Using rule seed: {rnd.Seed}.");
        _names = rnd.ShuffleFisherYates(_allNames.ToArray());
        Debug.Log($"<TAC #{_moduleId}> Names order: {_names.Join(", ")}");
        #endregion

        #region Decide the player colors
        colorsIxShuffle.Shuffle();
        var defuserColor = Random.Range(0, 4);

        for (int i = 0; i < LEDs.Length; i++)
        {
            LEDs[i].material = LEDColors[colorsIxShuffle[i]];
            PlayerNames[i].color = LedOnColors[colorsIxShuffle[i]];
            if (i == defuserColor)
                Pawn.sharedMaterial = PawnColors[colorsIxShuffle[i]];
        }
        #endregion

        #region Decide on cards in player’s hand
        var mustSwap = Random.Range(0, 2) != 0;
        tryAgain:
        var logging = new List<string>();
        _hand = Enumerable.Range(0, 5).Select(_ => cards[Random.Range(0, cards.Length)]).ToList();
        _state = TACGameState.FinalState(defuserColor, new TACPos(Random.Range(0, 32)));
        _canSwapPieces = false;

        var numSwappableCards = _hand.Count;
        var intendedStates = new List<TACGameState> { _state };
        for (var cardIx = 0; cardIx < _hand.Count; cardIx++)
        {
            tryThisAgain:
            var possibleUndos = _hand[cardIx].UnexecuteAll(_state).ToList();

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
            intendedStates.Add(_state);
        }
        logging.Add($"[TAC #{_moduleId}] Intended gameplay:");
        for (var i = _hand.Count - 1; i >= 0; i--)
            logging.Add($"[TAC #{_moduleId}] {JsonForLogging($"Play {_hand[i]}, yielding:", intendedStates[i])}");

        _hand.Shuffle();

        if (mustSwap)
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

        if (!_hand.Any(c => c is TACCardTrickster) && !(_swappableCard is TACCardTrickster))
            goto tryAgain;
        #endregion

        foreach (var log in logging)
            Debug.Log(log);

        _initialHand = _hand.ToList();
        _initialState = _state.Clone();

        for (int i = 0; i < _hand.Count; i++)
        {
            Cards[i].sharedMaterial = CardImages.First(x => x.name == _hand[i].MaterialName);
        }
        if (_swappableCard != null)
        {
            Cards[5].sharedMaterial = CardImages.First(x => x.name == _swappableCard.MaterialName);
        }

        PawnObject.transform.localPosition = _state.PlayerPosition.Vector(_state);

        ChoiceButtons[0].transform.localPosition = choiceButtonsStart;
        ChoiceButtons[1].transform.localPosition = choiceButtonsStart;

        #region Calculate base pawn positions
        PlayerNames[_state.PlayerSeat].text = "You";

        var serialNumber = BombInfo.GetSerialNumber().Select(ch => ch >= '0' && ch <= '9' ? ch - '0' : ch - 'A' + 1).ToArray();

        var enemy1Offset = _state.PlayerPosition + serialNumber[0] + serialNumber[5];
        PlayerNames[(_state.PlayerSeat + 1) % 4].text = _names[_state.Pieces[1].Value - enemy1Offset];

        var partnerOffset = _state.Pieces[1].Value + serialNumber[1] + serialNumber[4];
        PlayerNames[(_state.PlayerSeat + 2) % 4].text = _names[_state.Pieces[2].Value - partnerOffset];

        var enemy2Offset = _state.Pieces[2].Value + serialNumber[2] + serialNumber[3];
        PlayerNames[(_state.PlayerSeat + 3) % 4].text = _names[_state.Pieces[3].Value - enemy2Offset];
        #endregion

        Debug.Log($"[TAC #{_moduleId}] {JsonForLogging($"Player names (clockwise from You): {Enumerable.Range(1, 3).Select(ix => PlayerNames[(_state.PlayerSeat + ix) % 4].text).Join(", ")}", _state)}");
        Debug.Log($"[TAC #{_moduleId}] Initial hand: {_hand.Join(", ")}");
        if (_mustSwapWith == null)
            Debug.Log($"[TAC #{_moduleId}] You must not swap a card.");
        else
        {
            Debug.Log($"[TAC #{_moduleId}] You must swap a card.");
            Debug.Log($"[TAC #{_moduleId}] Hand after swap: {Enumerable.Range(0, _hand.Count).Select(ix => ix == _mustSwapWith.Value ? _swappableCard : _hand[ix]).Join(", ")}");
        }

        for (var i = 0; i < CardSels.Length; i++)
            CardSels[i].OnInteract += CardHandler(i);

        for (var i = 0; i < LEDSels.Length; i++)
            LEDSels[i].OnInteract += LEDHandler(i);

        Coroutine tacButtonCoroutine = null;

        TacSel.OnInteract += delegate ()
        {
            if (_gameResetting || _moduleSolved)
                return false;

            _tacButtonHeld = true;
            tacButtonCoroutine = StartCoroutine(CheckTACButtonReleased());
            return false;
        };

        TacSel.OnInteractEnded += delegate ()
        {
            if (_gameResetting || _moduleSolved)
                return;

            if (_tacButtonHeld)
            {
                StopCoroutine(tacButtonCoroutine);
                TACButtonHandler();
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

    private KMSelectable.OnInteractHandler LEDHandler(int ix)
    {
        return delegate
        {
            if (!_canSwapPieces || _buttonsMoving || _moduleSolved)
                return false;

            if (_swapPieceWith1 == null)
            {
                // User selected the first LED for swapping
                _swapPieceWith1 = ix;
                StartCoroutine(MoveLED(ix, down: true));
            }
            else if (_swapPieceWith1 == ix)
            {
                // User selected the same LED a second time ⇒ unselect it
                _swapPieceWith1 = null;
                StartCoroutine(MoveLED(ix, down: false));
            }
            else
            {
                // User selected the second LED: perform the swap
                StartCoroutine(MoveLED(_swapPieceWith1.Value, down: false));
                _swapPieceWith2 = ix;
                currentOptions[TACCardOption.Swap] = true;
                _canSwapPieces = false;
                StartCoroutine(HandleCard());
            }

            return false;
        };
    }

    private KMSelectable.OnInteractHandler CardHandler(int ix)
    {
        return delegate
        {
            if (_moduleSolved)
                return false;

            if (_mustSwapWith != null && !_hasSwapped)
            {
                Debug.Log($"[TAC #{_moduleId}] You tried to play a card at the start of the round, but you needed to swap first. Strike!");
                Strike();
                return false;
            }

            if (_canSwapPieces)
            {
                StartCoroutine(BlinkLEDs());
                return false;
            }

            if (!_cardMoving && !_gameResetting && !_buttonsMoving && currentCardChoice == null)
            {
                currentCardChoice = ix;
                currentOptions = new Dictionary<TACCardOption, bool>();
                StartCoroutine(HandleCard());
            }

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

    private IEnumerator PlayCard()
    {
        var obj = CardObjects[currentCardChoice.Value].transform;
        var card = _hand[currentCardChoice.Value];
        _hand[currentCardChoice.Value] = null;

        _cardMoving = true;
        yield return MoveObjectSmooth(obj, new Vector3(-0.06409124f, 0.0081f + (_cardsPlayedCounter * 0.0004f), 0.052f), Quaternion.Euler(-90, 0, 0), 1f);
        _cardMoving = false;
        var execResult = card.Execute(_state, currentOptions, _swapPieceWith1 ?? -1, _swapPieceWith2);

        if (execResult is TACCardExecuteStrike)
        {
            Debug.Log($"[TAC #{_moduleId}] {((TACCardExecuteStrike) execResult).LoggingMessage} Strike!");
            Strike();
        }
        else
        {
            var newState = ((TACCardExecuteSuccess) execResult).State;

            if (newState.PlayerPosition != _state.PlayerPosition)
                yield return MovePawn(newState.PlayerPosition, card.MoveType);

            for (var ix = 0; ix < 4; ix++)
                if (newState.Pieces[ix] == null && _state.Pieces[ix] != null)
                    yield return TurnLEDOff((ix + _state.PlayerSeat) % 4);

            _state = newState;
            _canSwapPieces = false;
            _cardsPlayedCounter++;
            currentOptions = null;
            currentCardChoice = null;

            Debug.Log($"[TAC #{_moduleId}] {JsonForLogging($"After playing {card}, board looks like this:", _state)}");

            if (_state.PlayerInHome && _hand.Any(c => c != null))
            {
                Debug.Log($"[TAC #{_moduleId}] You entered your home with cards still in your hand. Strike!");
                Strike();
            }
            else if (_state.PlayerInHome)
            {
                Debug.Log($"[TAC #{_moduleId}] You entered your home and your hand is empty. Module solved!");
                Module.HandlePass();
            }
            else if (_hand.All(c => c == null))
            {
                Debug.Log($"[TAC #{_moduleId}] Your hand is empty, but you did not enter your home. Strike!");
                Strike();
            }
        }
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

    private void TACButtonHandler()
    {
        StartCoroutine(ResetLEDPositions());
        StartCoroutine(ResetButtonPositions());

        if (currentCardChoice != null && _canSwapPieces)
            StartCoroutine(BlinkLEDs());

        if (currentCardChoice == null && !_hasSwapped && _cardsPlayedCounter == 0)
        {
            if (_mustSwapWith == null)
            {
                Debug.Log($"[TAC #{_moduleId}] You pressed the TAC button at the start of the round, but no swap was required. Strike!");
                Strike();
            }
            else
            {
                StartCoroutine(InitiateSwap());
                _hasSwapped = true;
            }
        }

        _canSwapPieces = false;
        currentOptions = null;
        currentCardChoice = null;
    }

    private IEnumerator MoveLED(int ledIx, bool down)
    {
        _buttonsMoving = true;
        yield return MoveObjectSmooth(LEDObjects[ledIx].transform,
            new Vector3(ledOrigPositions[ledIx].x, ledOrigPositions[ledIx].y + (down ? -.002f : 0), ledOrigPositions[ledIx].z),
            Quaternion.Euler(-90, 0, 180), .5f, 0f, false);
        _buttonsMoving = false;
    }

    private IEnumerator ResetLEDPositions()
    {
        _buttonsMoving = true;
        yield return MoveObjectsSmooth(
            LEDObjects.Select(x => x.transform).ToArray(),
            ledStartPositions,
            new[] { Quaternion.Euler(-90, 0, 180), Quaternion.Euler(-90, 0, 180), Quaternion.Euler(-90, 0, 180), Quaternion.Euler(-90, 0, 180) },
            .5f,
            new[] { 0f, 0f, 0f, 0f },
            false);
        _buttonsMoving = false;
    }

    private IEnumerator TurnLEDOff(int ledIx)
    {
        var onColor = LedOnColors[colorsIxShuffle[ledIx]];
        var offColor = LedOffColors[colorsIxShuffle[ledIx]];

        LEDs[ledIx].material.color = offColor;
        yield return new WaitForSeconds(0.05f);
        LEDs[ledIx].material.color = onColor;
        yield return new WaitForSeconds(0.1f);
        LEDs[ledIx].material.color = offColor;
        yield return new WaitForSeconds(0.05f);
        LEDs[ledIx].material.color = onColor;
        yield return new WaitForSeconds(0.2f);
        LEDs[ledIx].material.color = offColor;
    }

    private IEnumerator BlinkLEDs()
    {
        for (var i = 0; i < LEDs.Length; i++)
            LEDs[i].material.color = LedOffColors[colorsIxShuffle[i]];
        yield return new WaitForSeconds(0.1f);
        for (var i = 0; i < LEDs.Length; i++)
            LEDs[i].material.color = LedOnColors[colorsIxShuffle[i]];
        yield return new WaitForSeconds(0.1f);
        for (var i = 0; i < LEDs.Length; i++)
            LEDs[i].material.color = LedOffColors[colorsIxShuffle[i]];
        yield return new WaitForSeconds(0.1f);
        for (var i = 0; i < LEDs.Length; i++)
            LEDs[i].material.color = LedOnColors[colorsIxShuffle[i]];
        yield return new WaitForSeconds(0.1f);
        for (var i = 0; i < LEDs.Length; i++)
            LEDs[i].material.color = _state.Pieces[(i + 4 - _state.PlayerSeat) % 4] == null ? LedOffColors[colorsIxShuffle[i]] : LedOnColors[colorsIxShuffle[i]];
    }

    private void ResetLEDColors()
    {
        for (int i = 0; i < LEDs.Length; i++)
            LEDs[i].material.color = LedOnColors[colorsIxShuffle[i]];
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
            false);
        _buttonsMoving = false;
    }

    private IEnumerator HandleCard()
    {
        if (currentOptions != null && currentOptions.Count > 0) yield return ResetButtonPositions();
        var option = _hand[currentCardChoice.Value].GetOption(_state, currentOptions);
        if (option == TACCardOption.None)
        {
            yield return PlayCard();
            yield break;
        }

        currentChoice = option;
        switch (option)
        {
            case TACCardOption.Discard:
                ChoiceButtons[0].sharedMaterial = ButtonImagesMove[0]; // To-do: ruleseed
                ChoiceButtons[1].sharedMaterial = ButtonImageDiscard;
                yield return MoveButtons();
                break;

            case TACCardOption.EnterHome:
                ChoiceButtons[0].sharedMaterial = ButtonImagePassHome;
                ChoiceButtons[1].sharedMaterial = ButtonImageEnterHome;
                yield return MoveButtons();
                break;

            case TACCardOption.EnterHomeBackwards:
                ChoiceButtons[0].sharedMaterial = ButtonImagePassHomeBackwards;
                ChoiceButtons[1].sharedMaterial = ButtonImageEnterHome;
                yield return MoveButtons();
                break;

            case TACCardOption.Swap:
                _canSwapPieces = true;
                _swapPieceWith1 = null;
                yield return BlinkLEDs();
                break;
        }
    }

    private IEnumerator MovePawn(TACPos endPos, TACPawnMove moveType)
    {
        var obj = PawnObject.transform;
        var endPosition = endPos.Vector(_state);
        var endRotation = Quaternion.Euler(-90, 0, 180);
        var duration = 1f;
        if (moveType == TACPawnMove.Teleport)
            yield return MoveObjectSmooth(obj, endPosition, endRotation, duration);
        else
        {
            var corners = new List<TACPos>();
            var numStepsForCorner = new List<int>();
            if (endPos.IsHome && _state.PlayerPosition == TACPos.GetStart(_state.PlayerSeat))
            {
                corners.Add(endPos);
                numStepsForCorner.Add(1);
            }
            else if (moveType == TACPawnMove.Backwards && _state.PlayerInHome && endPos == TACPos.GetStart(_state.PlayerSeat))
            {
                corners.Add(endPos);
                numStepsForCorner.Add(1);
            }
            else
            {
                var direction = moveType == TACPawnMove.Forwards ? 1 : -1;
                var posCheck = moveType == TACPawnMove.Backwards && _state.PlayerInHome ? TACPos.GetStart(_state.PlayerSeat) : _state.PlayerPosition + direction;
                var steps = 1;
                if (direction < 0 && _state.PlayerInHome)
                {
                    posCheck = TACPos.GetStart(_state.PlayerSeat);
                    corners.Add(posCheck);
                    numStepsForCorner.Add(1);
                    steps = 0;
                }
                while (posCheck != endPos)
                {
                    if (posCheck.IsCorner)
                    {
                        corners.Add(posCheck);
                        numStepsForCorner.Add(steps);
                        steps = 0;
                        posCheck += direction;
                    }
                    else if (endPos.IsHome && posCheck == TACPos.GetStart(_state.PlayerSeat))
                    {
                        corners.Add(posCheck);
                        numStepsForCorner.Add(steps);
                        steps = 0;
                        posCheck = TACPos.Home;
                    }
                    else
                        posCheck += direction;
                    steps++;
                }
                corners.Add(endPos);
                numStepsForCorner.Add(steps);
            }
            var totalSteps = numStepsForCorner.Sum();
            for (var i = 0; i < corners.Count; i++)
                yield return MoveObjectSmooth(obj, corners[i].Vector(_state), endRotation, duration / totalSteps * numStepsForCorner[i], 0, false);
        }
    }

    private bool isSolvable() => solve(_state, _hand.ToArray()).Any();

    private IEnumerable<TACGameState> solve(TACGameState state, TACCard[] hand)
    {
        if (hand.Length == 0)
        {
            if (state.PlayerInHome)
                yield return state;
            yield break;
        }
        for (var i = 0; i < hand.Length; i++)
            foreach (var newState in hand[i].ExecuteAll(state))
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

    private string JsonForLogging(string message, TACGameState state)
    {
        var j = new JObject();
        j["message"] = message;
        j["colors"] = new JArray(colorsIxShuffle);
        j["playerseat"] = state.PlayerSeat;
        j["positions"] = new JArray(state.Pieces.Select(p => (int?) p).ToArray());
        return j.ToString(Formatting.None);
    }

    private void Strike()
    {
        if (!_moduleSolved)
            Module.HandleStrike();

        StartCoroutine(ResetState());
    }

    private IEnumerator ResetState()
    {
        _gameResetting = true;

        yield return ResetButtonPositions();
        yield return ResetCardPositions();
        yield return ResetLEDPositions();
        ResetLEDColors();

        if (_state.PlayerPosition != _initialState.PlayerPosition)
            yield return MovePawn(_initialState.PlayerPosition, TACPawnMove.Backwards);

        #region ResetSwap
        if (_hasSwapped)
        {
            yield return MoveObjectsSmooth(
                new[] { CardObjects[5].transform, CardObjects[(int) _mustSwapWith].transform },
                new[] { cardPositions[(int) _mustSwapWith], cardPositions[5] },
                new[] { cardRotations[(int) _mustSwapWith], cardRotations[5] },
                1f,
                new[] { .23f, .5f });

            Cards[5].sharedMaterial = CardImages.First(x => x.name == _swappableCard.MaterialName);
            Cards[(int) _mustSwapWith].sharedMaterial = CardImages.First(x => x.name == _initialHand[(int) _mustSwapWith].MaterialName);

            CardObjects[5].transform.localPosition = cardPositions[5];
            CardObjects[(int) _mustSwapWith].transform.localPosition = cardPositions[(int) _mustSwapWith];
            CardObjects[5].transform.localRotation = cardRotations[5];
            CardObjects[(int) _mustSwapWith].transform.localRotation = cardRotations[(int) _mustSwapWith];

            _hasSwapped = false;
        }
        #endregion

        _hand = _initialHand.ToList();
        _state = _initialState.Clone();

        _cardsPlayedCounter = 0;

        _canSwapPieces = false;
        _gameResetting = false;
        currentOptions = null;
        currentCardChoice = null;
    }

    private IEnumerator ResetButtonPositions() => MoveObjectsSmooth(
            new[] { ChoiceButtons[0].transform, ChoiceButtons[1].transform },
            new[] { choiceButtonsStart, choiceButtonsStart },
            new[] { Quaternion.Euler(-90, 0, 180), Quaternion.Euler(-90, 0, 180) },
            1f,
            new[] { 0f, 0f },
            false);

    private IEnumerator ResetCardPositions()
    {
        for (int i = 0; i < CardObjects.Length; i++)
            if (CardObjects[i].transform.localPosition != cardPositions[i])
                StartCoroutine(MoveObjectSmooth(CardObjects[i].transform, cardPositions[i], cardRotations[i], 1f));
        yield return CardObjects;
    }

    private IEnumerator InitiateSwap()
    {
        yield return MoveObjectsSmooth(
            new[] { CardObjects[5].transform, CardObjects[(int) _mustSwapWith].transform },
            new[] { cardPositions[(int) _mustSwapWith], cardPositions[5] },
            new[] { cardRotations[(int) _mustSwapWith], cardRotations[5] },
            1f,
            new[] { .23f, .5f });

        Cards[5].sharedMaterial = CardImages.First(x => x.name == _hand[(int) _mustSwapWith].MaterialName);
        Cards[(int) _mustSwapWith].sharedMaterial = CardImages.First(x => x.name == _swappableCard.MaterialName);

        CardObjects[5].transform.localPosition = cardPositions[5];
        CardObjects[(int) _mustSwapWith].transform.localPosition = cardPositions[(int) _mustSwapWith];
        CardObjects[5].transform.localRotation = cardRotations[5];
        CardObjects[(int) _mustSwapWith].transform.localRotation = cardRotations[(int) _mustSwapWith];

        _hand[(int) _mustSwapWith] = _swappableCard;
    }
}