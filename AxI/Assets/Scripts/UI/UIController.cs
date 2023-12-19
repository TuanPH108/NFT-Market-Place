using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using AxieCore.AxieMixer;
using AxieMixer.Unity;
using Newtonsoft.Json.Linq;
using Spine.Unity;
using UnityEngine.Networking;
using Jint.Native.Number;
using Unity.VisualScripting;
using System;
using Random = UnityEngine.Random;
using UnityEngine.Windows;
using System.IO;
using Directory = System.IO.Directory;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using UnityEditor;

public class UIController : MonoBehaviour
{
    public Player player;
    public Text addressPlayer;
    public Text axieNum;
    public Button nextButton;
    public Button prevButton;
    public Button exitButton;
    public GameObject claim;
    public GameObject notice;
    public int currentAxiesIndex = 0;

    // Start is called before the first frame update
    void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
        addressPlayer.text = player.getPlayerAccout().Substring(0, 7)+ "..." + player.getPlayerAccout().Substring(player.getPlayerAccout().Length - 5, 5);
        UpdateUI();
        if (player.getTotalAxies() == 0) notice.SetActive(true);
        else loadList(player.getListAxies());
    }


    public void UpdateUI() {
        axieNum.text = player.getTotalAxies().ToString();
    }

    public void loadList(List<string> l) {
        // Load list of axies
        StartCoroutine(LoadAxiesSequentially(l));
        nextButton.onClick.AddListener(NextAxie);
        prevButton.onClick.AddListener(PrevAxie);
        exitButton.onClick.AddListener(ExitGame);
    }

    public void updateList(string axieId)
    {
        StartCoroutine(updateListAxie(axieId));
    }

    private IEnumerator LoadAxiesSequentially(List<string> axies) {
        for (int i = 0; i < axies.Count; i++) {
            yield return StartCoroutine(LevelManager.LInstance.GetAxiesGenes(axies[i].ToString(), true, false, i));
            currentAxiesIndex = i;
        }
        currentAxiesIndex = 0;
    }
    private IEnumerator updateListAxie(string axieId)
    {
        player.addAxie(axieId);
        yield return StartCoroutine(LevelManager.LInstance.GetAxiesGenes(axieId.ToString(), true, false, LevelManager.LInstance.axies.Count));
    }
    public void NextAxie() {
        if (currentAxiesIndex == LevelManager.LInstance.axies.Count - 1)
        {
            LevelManager.LInstance.axies[currentAxiesIndex].axie.SetActive(false);
            currentAxiesIndex = 0;
            LevelManager.LInstance.axies[currentAxiesIndex].axie.SetActive(true);
        }
        else
        {
            // tới vị trí kế
            LevelManager.LInstance.axies[currentAxiesIndex].axie.SetActive(false);
            currentAxiesIndex++;
            LevelManager.LInstance.axies[currentAxiesIndex].axie.SetActive(true);
        }
        LevelManager.LInstance.axieSelect = currentAxiesIndex;
    }

    public void PrevAxie() {
        if (currentAxiesIndex == 0)
        {
            LevelManager.LInstance.axies[currentAxiesIndex].axie.SetActive(false);
            currentAxiesIndex = LevelManager.LInstance.axies.Count - 1;
            LevelManager.LInstance.axies[currentAxiesIndex].axie.SetActive(true);
        }
        else
        {
            // lùi 1 vị trí
            LevelManager.LInstance.axies[currentAxiesIndex].axie.SetActive(false);
            currentAxiesIndex--;
            LevelManager.LInstance.axies[currentAxiesIndex].axie.SetActive(true);
        }
        LevelManager.LInstance.axieSelect = currentAxiesIndex;
    } 

    public async void BattleGame() {
        // load next scene
        var scene = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex + 1, LoadSceneMode.Single);
        scene.allowSceneActivation = false;

        await Task.Delay(1000);

    }

    public void ExitGame() {
        
        string[] files = Directory.GetFiles(Application.persistentDataPath);
        if (files.Length != 0)
        {
            foreach (var f in Directory.GetFiles(Application.persistentDataPath))
            {
                FileInfo i = new FileInfo(f);
                i.Delete();
            }
        }
        Debug.Log("Exit");
        #if !UNITY_EDITOR
            Application.Quit();
        #else
            EditorApplication.ExitPlaymode();
        #endif
    }
    
    public void Gift()
    {
        claim.SetActive(true);
        
        notice.SetActive(false);
    }
}