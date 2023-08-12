using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using KModkit;
using System.Text.RegularExpressions;

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
    private int _moduleId, finalPosition, enemyPawn1, enemyPawn2, partnerPawn, steps;
    private bool _moduleSolved, needSwap;
    private string[] numbers = new[] { "1", "2", "3", "-4", "5", "6", "7", "8", "9", "10", "12", "13" },
        powers = new[] { "Trickster", "Warrior" },
        colours = new[] { "Green", "Red", "Yellow", "Blue" },
        newColours = new string[4];
    private List<string> hand = new List<string>();
    private int[] coloursIxShuffle = new[] { 0, 1, 2, 3 }, serialNumber;
    private string defuserColour, swappableCard;
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
    },
        homes = new[] {
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
        #region Initiate random colours and final position
        coloursIxShuffle.Shuffle();
        int tmp = Random.Range(0, 4);

        for (int i = 0; i < LEDs.Length; i++)
        {
            LEDs[i].material = LEDColours[coloursIxShuffle[i]];
            newColours[i] = colours[coloursIxShuffle[i]];
            if (i == tmp)
            {
                Pawn.material = PawnColours[coloursIxShuffle[i]];
                defuserColour = colours[coloursIxShuffle[i]];
            }
        }
        for (int i = 0; i < newColours.Length; i++)
        {
            if (newColours[i] == defuserColour)
            {
                finalPosition = i;
            }
        }
        #endregion
        #region Calculate other pawn positions
        serialNumber = BombInfo.GetSerialNumber().Select(ch => ch >= '0' && ch <= '9' ? ch - '0' : ch - 'A' + 1).ToArray();
        enemyPawn1 = serialNumber[0] + serialNumber[5];
        enemyPawn2 = serialNumber[1] + serialNumber[4];
        while (enemyPawn1 == enemyPawn2)
            enemyPawn2++;
        partnerPawn = serialNumber[2] + serialNumber[3];
        while (enemyPawn1 == partnerPawn || enemyPawn2 == partnerPawn)
            partnerPawn++;
        enemyPawn1 %= 32;
        enemyPawn2 %= 32;
        partnerPawn %= 32;
        Debug.Log(enemyPawn1 + " " + enemyPawn2 + " " + partnerPawn);
        #endregion
        #region Generate random powers
        bool hasPower = Random.Range(0, 2) == 1 ? true : false;
        if (hasPower)
        {
            powers.Shuffle();
            hand.Add(powers[0]);
        }
        #endregion
        #region Generate starting location

        #endregion
        #region Requires a card swap
        //bool needCardSwap = Random.Range(0, 2) == 1 ? true : false;
        bool needCardSwap = false;
        #endregion
        #region Generate number cards
            
        #endregion


    }
}
