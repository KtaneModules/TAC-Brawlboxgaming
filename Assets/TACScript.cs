using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;

using Random = UnityEngine.Random;
using Assets;

public class TACScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMRuleSeedable RuleSeedable;

    public MeshRenderer[] LEDs, Cards;
    public MeshRenderer Pawn;
    public GameObject PawnObject;
    public GameObject[] CardObjects;
    public Material[] PawnColours, LEDColours, CardImages;

    private static int _moduleIdCounter = 1;
    private int _moduleId, steps;
    private bool _moduleSolved, needSwap;
    private TACGameState state;

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
    private static readonly string[] colourNames = new[] { "Green", "Red", "Yellow", "Blue" };

    private List<TACCard> hand = new List<TACCard>();
    private TACCard swappableCard;

    private readonly int[] coloursIxShuffle = new[] { 0, 1, 2, 3 };
    private int defuserColour;

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
        PawnObject.transform.localPosition = boardPositions[0];
        _moduleId = _moduleIdCounter++;

        #region Decide the player colors
        coloursIxShuffle.Shuffle();
        defuserColour = Random.Range(0, 4);

        for (int i = 0; i < LEDs.Length; i++)
        {
            LEDs[i].material = LEDColours[coloursIxShuffle[i]];
            if (i == defuserColour)
                Pawn.material = PawnColours[coloursIxShuffle[i]];
        }
        #endregion

        #region Calculate base pawn positions
        var serialNumber = BombInfo.GetSerialNumber().Select(ch => ch >= '0' && ch <= '9' ? ch - '0' : ch - 'A' + 1).ToArray();
        var enemy1 = new TACPos(serialNumber[0] + serialNumber[5]);
        var partner = new TACPos(serialNumber[1] + serialNumber[4]);
        while (enemy1 == partner)
            partner++;
        var enemy2 = new TACPos(serialNumber[2] + serialNumber[3]);
        while (enemy2 == partner || enemy2 == enemy1)
            enemy2++;
        Debug.Log(enemy1 + " " + partner + " " + enemy2);
        #endregion
    }
}
