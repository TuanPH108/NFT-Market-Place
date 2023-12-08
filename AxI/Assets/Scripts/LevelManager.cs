using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.SceneManagement;
using AxieCore.AxieMixer;
using AxieMixer.Unity;
using Newtonsoft.Json.Linq;
using Spine.Unity;
using UnityEngine.Networking;

public class LevelManager : MonoBehaviour
{
    public static LevelManager LInstance { get; private set; }
    public int axieSelect;
    public Vector2 spawnPos;
    [SerializeField] RectTransform rootTF;
    Axie2dBuilder builder => Mixer.Builder;
    bool isFetchingGenes = false;

    private void Awake()
    {  
        if (LInstance != null && LInstance != this) Destroy(this);
        else {
            LInstance = this;
            Mixer.Init();
            DontDestroyOnLoad(this);
        }
    }

    //private IEnumerator LoadScene()
    //{
    //    var scene = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex + 1, LoadSceneMode.Single);
    //    while (scene.progress < 0.9f)
    //    {
    //        if (SceneManager.GetActiveScene().buildIndex == 0)
    //        {

    //        }
    //    }
    //    yield return new WaitForSeconds(2.5f);
    //    yield return new WaitForEndOfFrame();
    //}

        void ProcessMixer(string axieId, string genesStr, bool isGraphic)   //Lụm
        {
            if (string.IsNullOrEmpty(genesStr))
            {
                Debug.LogError($"[{axieId}] genes not found!!!");
                return;
            }
            float scale = 0.007f;

            var meta = new Dictionary<string, string>();
            //foreach (var accessorySlot in ACCESSORY_SLOTS)
            //{
            //    meta.Add(accessorySlot, $"{accessorySlot}1{System.Char.ConvertFromUtf32((int)('a') + accessoryIdx - 1)}");
            //}
            var builderResult = builder.BuildSpineFromGene(axieId, genesStr, meta, scale, isGraphic);

            //Test
            if (isGraphic)
            {
                SpawnSkeletonGraphic(builderResult);
            }
            else
            {
                SpawnSkeletonAnimation(builderResult);
            }
        }

        void SpawnSkeletonAnimation(Axie2dBuilderResult builderResult)      //Lụm
        {
            GameObject go = new GameObject("DemoAxie");
            go.transform.localPosition = new Vector3(0f, -2.4f, 0f);
            SkeletonAnimation runtimeSkeletonAnimation = SkeletonAnimation.NewSkeletonAnimationGameObject(builderResult.skeletonDataAsset);
            runtimeSkeletonAnimation.gameObject.layer = LayerMask.NameToLayer("Player");
            runtimeSkeletonAnimation.transform.SetParent(go.transform, false);
            runtimeSkeletonAnimation.transform.localScale = Vector3.one;

            runtimeSkeletonAnimation.gameObject.AddComponent<AutoBlendAnimController>();
            runtimeSkeletonAnimation.state.SetAnimation(0, "action/idle/normal", true);

            if (builderResult.adultCombo.ContainsKey("body") &&
                builderResult.adultCombo["body"].Contains("mystic") &&
                builderResult.adultCombo.TryGetValue("body-class", out var bodyClass) &&
                builderResult.adultCombo.TryGetValue("body-id", out var bodyId))
            {
                runtimeSkeletonAnimation.gameObject.AddComponent<MysticIdController>().Init(bodyClass, bodyId);
            }
            runtimeSkeletonAnimation.skeleton.FindSlot("shadow").Attachment = null;
        }

        void SpawnSkeletonGraphic(Axie2dBuilderResult builderResult)        //Lụm
        {
            var skeletonGraphic = SkeletonGraphic.NewSkeletonGraphicGameObject(builderResult.skeletonDataAsset, rootTF, builderResult.sharedGraphicMaterial);
            skeletonGraphic.rectTransform.sizeDelta = new Vector2(1, 1);
            skeletonGraphic.rectTransform.localScale = new Vector3(0.33f,0.33f,0.1f);
            skeletonGraphic.rectTransform.anchoredPosition = new Vector2(1f, -1.5f);
            skeletonGraphic.Initialize(true);
            skeletonGraphic.Skeleton.SetSkin("default");
            skeletonGraphic.Skeleton.SetSlotsToSetupPose();

            skeletonGraphic.gameObject.AddComponent<AutoBlendAnimGraphicController>();
            skeletonGraphic.AnimationState.SetAnimation(0, "action/idle/normal", true);

            if (builderResult.adultCombo.ContainsKey("body") &&
             builderResult.adultCombo["body"].Contains("mystic") &&
             builderResult.adultCombo.TryGetValue("body-class", out var bodyClass) &&
             builderResult.adultCombo.TryGetValue("body-id", out var bodyId))
            {
                skeletonGraphic.gameObject.AddComponent<MysticIdGraphicController>().Init(bodyClass, bodyId);
            }
        }

        public IEnumerator GetAxiesGenes(string axieId, bool UIUse)     // Lụm
        {
            isFetchingGenes = true;
            string searchString = "{ axie (axieId: \"" + axieId + "\") { id, genes, newGenes}}";
            JObject jPayload = new JObject();
            jPayload.Add(new JProperty("query", searchString));

            var wr = new UnityWebRequest("https://graphql-gateway.axieinfinity.com/graphql", "POST");
            //var wr = new UnityWebRequest("https://testnet-graphql.skymavis.one/graphql", "POST");
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jPayload.ToString().ToCharArray());
            wr.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
            wr.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            wr.SetRequestHeader("Content-Type", "application/json");
            wr.timeout = 10;
            yield return wr.SendWebRequest();
            if (wr.error == null)
            {
                var result = wr.downloadHandler != null ? wr.downloadHandler.text : null;
                if (!string.IsNullOrEmpty(result))
                {
                    JObject jResult = JObject.Parse(result);
                    string genesStr = (string)jResult["data"]["axie"]["newGenes"];
                    Debug.Log(genesStr);
                    ProcessMixer(axieId, genesStr, UIUse);
                }
            }
            isFetchingGenes = false;
        }
}
