using System.Collections;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using System;
using UnityEngine.Video;
using System.Collections.Generic;

public class AlcoholicRampageScript : MonoBehaviour {

    public KMBombInfo bomb;
    public KMSelectable[] buttons;

    public VideoPlayer video;
    public VideoClip[] clips;
    public AudioSource videoAudio;
    public AudioSource deathAudio;
    public AudioClip[] deathClips;
    public Renderer videoRend;
    public TextMesh bacText;
    public SpriteRenderer mercRend;
    public Sprite[] mercIcons;

    List<int> killedMercs = new List<int>();
    string[] mercs = { "Scout", "Soldier", "Pyro", "Heavy", "Engineer", "Medic", "Sniper", "Spy" };
    int chosenMerc;
    int bac;
    int targetBac;
    int stage;
    bool processingVideo = true;
    bool activated;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable obj in buttons)
        {
            KMSelectable pressed = obj;
            pressed.OnInteract += delegate () { PressButton(pressed); return false; };
        }
        GetComponent<KMBombModule>().OnActivate += Intro;
    }

    void Start()
    {
        ToggleStuffVisibility(false);
        if (Application.isEditor)
        {
            video.clip = clips[0];
            video.SetTargetAudioSource(0, videoAudio);
            video.Prepare();
            GenerateStage();
        }
        else
            StartCoroutine(WaitForVideoClips());
    }

    void Intro()
    {
        if (Application.isEditor || (!Application.isEditor && VideoLoader.clips != null))
        {
            video.Play();
            StartCoroutine(ProcessingVideo());
        }
        activated = true;
    }

    void GenerateStage()
    {
        bac = UnityEngine.Random.Range(3, 6);
        bacText.text = "BAC: 0." + bac.ToString("00");
        chosenMerc = UnityEngine.Random.Range(0, mercs.Length);
        while (killedMercs.Contains(chosenMerc))
            chosenMerc = UnityEngine.Random.Range(0, mercs.Length);
        killedMercs.Add(chosenMerc);
        mercRend.sprite = mercIcons[chosenMerc];
        if (chosenMerc != 6)
            deathAudio.clip = deathClips[chosenMerc];
        switch (chosenMerc)
        {
            case 0:
                targetBac = 0;
                break;
            case 1:
                targetBac = bomb.GetModuleIDs().Count(x => x.EqualsAny("obamaGroceryStore", "spangledStars", "USACycle", "USA")) + 10;
                break;
            case 2:
                targetBac = bomb.GetBatteryCount() + bomb.GetPortCount();
                break;
            case 3:
                targetBac = 5 * bomb.GetBatteryHolderCount() + 2;
                break;
            case 4:
                targetBac = (int)GetVoltage() + 15;
                break;
            case 5:
                if (Application.isEditor)
                    targetBac = 3;
                else
                    targetBac = transform.parent.parent.GetComponent(ReflectionHelper.FindType("Bomb", "Assembly-CSharp")).GetValue<int>("NumStrikesToLose");
                break;
            case 6:
                targetBac = 3 * bomb.GetIndicators().Count() + bomb.GetModuleIDs().Count(x => x.Equals("HitmanModule"));
                break;
            default:
                targetBac = bomb.GetSerialNumberNumbers().Sum() + 3;
                break;
        }
        if (targetBac > 25)
            targetBac = 25;
        Debug.LogFormat("[Alcoholic Rampage #{0}] You decide to kill {1}! You have a BAC of 0.{2} and need at least 0.{3}!", moduleId, mercs[chosenMerc], bac.ToString("00"), targetBac.ToString("00"));
    }

    void PressButton(KMSelectable pressed)
    {
        if (moduleSolved != true && processingVideo != true)
        {
            int index = Array.IndexOf(buttons, pressed);
            if (index == 0)
            {
                if (bac < targetBac)
                {
                    processingVideo = true;
                    video.clip = Application.isEditor ? clips[UnityEngine.Random.Range(1, 4)] : VideoLoader.clips[UnityEngine.Random.Range(1, 4)];
                    video.Play();
                    bac += UnityEngine.Random.Range(1, 6);
                    bacText.text = "BAC: 0." + bac.ToString("00");
                    Debug.LogFormat("[Alcoholic Rampage #{0}] That's good scrumpy! You now have a BAC of 0.{1}.", moduleId, bac.ToString("00"));
                    ToggleStuffVisibility(false);
                    StartCoroutine(ProcessingVideo());
                }
                else
                {
                    GetComponent<KMBombModule>().HandleStrike();
                    Debug.LogFormat("[Alcoholic Rampage #{0}] Do not excessively drink! Strike!", moduleId);
                }
            }
            else
            {
                if (bac >= targetBac)
                {
                    processingVideo = true;
                    Debug.LogFormat("[Alcoholic Rampage #{0}] You killed {1}! Good job!", moduleId, mercs[chosenMerc]);
                    video.clip = Application.isEditor ? clips[4] : VideoLoader.clips[4];
                    video.Stop();
                    video.Play();
                    StartCoroutine(PlayDeathScream(chosenMerc));
                    stage++;
                    if (stage != 3)
                        GenerateStage();
                    ToggleStuffVisibility(false);
                    StartCoroutine(ProcessingVideo());
                }
                else
                {
                    GetComponent<KMBombModule>().HandleStrike();
                    Debug.LogFormat("[Alcoholic Rampage #{0}] You are not drunk enough to kill! Strike!", moduleId);
                }
            }
        }
    }

    double GetVoltage()
    {
        if (bomb.QueryWidgets("volt", "").Count() != 0)
        {
            double TempVoltage = double.Parse(bomb.QueryWidgets("volt", "")[0].Substring(12).Replace("\"}", ""));
            return TempVoltage;
        }
        return 0;
    }

    IEnumerator WaitForVideoClips()
    {
        yield return new WaitUntil(() => VideoLoader.clips != null);
        video.clip = VideoLoader.clips[0];
        video.SetTargetAudioSource(0, videoAudio);
        video.Prepare();
        GenerateStage();
        if (activated)
        {
            video.Play();
            StartCoroutine(ProcessingVideo());
        }
    }

    IEnumerator ProcessingVideo()
    {
        while (!video.isPrepared || video.isPlaying)
            yield return null;
        if (stage == 3)
        {
            moduleSolved = true;
            GetComponent<KMBombModule>().HandlePass();
            Debug.LogFormat("[Alcoholic Rampage #{0}] Module solved!", moduleId);
        }
        else
            ToggleStuffVisibility(true);
        processingVideo = false;
    }

    IEnumerator PlayDeathScream(int merc)
    {
        while (video.time < 1.8f) yield return null;
        if (merc != 6)
        {
            deathAudio.clip = deathClips[merc];
            deathAudio.Play();
        }
    }

    void ToggleStuffVisibility(bool show)
    {
        for (int i = 0; i < buttons.Length; i++)
            buttons[i].gameObject.SetActive(show);
        bacText.gameObject.SetActive(show);
        mercRend.gameObject.SetActive(show);
        if (show)
            videoRend.material.color = new Color(0.4f, 0.4f, 0.4f, 1);
        else
            videoRend.material.color = new Color(1, 1, 1, 1);
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} drink [Drink some scrumpy] | !{0} kill [Kill the mercenary]";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*drink\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (processingVideo)
            {
                yield return "sendtochaterror You cannot drink right now!";
                yield break;
            }
            yield return null;
            buttons[0].OnInteract();
            yield break;
        }
        if (Regex.IsMatch(command, @"^\s*kill\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (processingVideo)
            {
                yield return "sendtochaterror You cannot kill right now!";
                yield break;
            }
            yield return null;
            yield return "solve";
            buttons[1].OnInteract();
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (stage != 3)
        {
            if (!processingVideo)
            {
                if (bac >= targetBac)
                    buttons[1].OnInteract();
                else
                    buttons[0].OnInteract();
            }
            else
                yield return true;
        }
        while (!moduleSolved) yield return true;
    }
}