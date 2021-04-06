using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using rnd = UnityEngine.Random;
using KModkit;
public class FaceRecognition: MonoBehaviour {
    string[][] names = new string[][] { new string[] { "Glen", "Bill", "Mary", "Ben" }, new string[] { "Clem", "Gary", "Jen", "Mel" }, new string[] { "Phil", "Larry", "Len", "Jill" }, new string[] { "Perry", "Del", "Harry", "Teri" } };
    public Sprite[] faces;
    public SpriteRenderer faceDisplay;
    public TextMesh nameDisplay;
    public KMSelectable button;
    string[] moduleNames = new string[5];
    Sprite[] moduleFaces = new Sprite[5];
    int currentFace;
    int correctFace;
    Coroutine faceAnimation;
    public KMBombModule module;
    public KMAudio sound;
    int moduleId;
    static int moduleIdCounter = 1;
    bool solved;
    void Awake() {
        moduleId = moduleIdCounter++;
        button.OnInteract += delegate { PressButton(); return false; };
        GeneratePuzzle();
    }

    void GeneratePuzzle() {
        List<int> seedNumbers = Enumerable.Range(0, 16).ToList().Shuffle().Take(5).ToList();
        int shiftAmountX = rnd.Range(0, 4);
        int shiftAmountY = rnd.Range(0, 4);
        for (int i = 0; i < 5; i++)
        {
            moduleFaces[i] = faces[seedNumbers[i]];
            moduleNames[i] = names[((seedNumbers[i] / 4) + shiftAmountY) % 4][((seedNumbers[i] % 4) + shiftAmountX) % 4];
        }
        correctFace = rnd.Range(0, 5);
        List<string> possibleSolutionNames = names.SelectMany(a => a).ToList();
        foreach (string i in moduleNames) possibleSolutionNames.Remove(i);
        moduleNames[correctFace] = possibleSolutionNames[rnd.Range(0, 11)];
        Debug.LogFormat("[Face Recognition #{0}] The names on the module are {1}.", moduleId, moduleNames.Join(", "));
        Debug.LogFormat("[Face Recognition #{0}] The faces on the module are Face {1}.", moduleId, seedNumbers.ToList().Select(x => x + 1).Join(", Face "));
        Debug.LogFormat("[Face Recognition #{0}] The correct person is {1}.", moduleId, moduleNames[correctFace]);
        faceAnimation = StartCoroutine(CycleFaces());
    }

    void PressButton()
    {
        if (!solved)
        {
            button.AddInteractionPunch(0.1f);
            Debug.LogFormat("[Face Recognition #{0}] You submitted {1}.", moduleId, moduleNames[currentFace]);
            StopCoroutine(faceAnimation);
            if (currentFace == correctFace)
            {
                Debug.LogFormat("[Face Recognition #{0}] That was correct. Module solved.", moduleId);
                module.HandlePass();
                sound.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                faceDisplay.sprite = null;
                nameDisplay.text = "Solved";
                solved = true;
            }
            else
            {
                Debug.LogFormat("[Face Recognition #{0}] That was incorrect. Strike!", moduleId);
                module.HandleStrike();
                GeneratePuzzle();
            }
        }
    }
    IEnumerator CycleFaces()
    {
        faceDisplay.sprite = null;
        nameDisplay.text = "";
        yield return new WaitForSeconds(1.5f);
        while (true)
        {
            currentFace++;
            if (currentFace == 5) currentFace = 0;
            faceDisplay.sprite = moduleFaces[currentFace];
            nameDisplay.text = moduleNames[currentFace];
            yield return new WaitForSeconds(1.5f);
        }
    }
    string TwitchHelpMessage = "Use '!{0} submit <name>' to submit the indicated name.";
    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();
        string[] commandArray = command.Trim().Split();
        string[] lowercaseNames = moduleNames.ToList().Select(x => x.ToLowerInvariant()).ToArray();
        if (!(commandArray.Length == 2 && commandArray[0] == "submit"))
        {
            yield return "sendtochaterror Invalid command format.";
            yield break;
        }
        if (!lowercaseNames.Contains(commandArray[1]))
        {
            yield return "sendtochaterror The name '" + commandArray[1] + "' is not on the module!";
            yield break;
        }
        else
        {
            yield return null;
            while (!(Array.IndexOf(lowercaseNames, commandArray[1]) == currentFace))
            {
                yield return null;
            }
            button.OnInteract();
        }
    }
    IEnumerator TwitchHandleForcedSolve()
    {
        while (currentFace != correctFace) yield return null;
        button.OnInteract();
    }
}