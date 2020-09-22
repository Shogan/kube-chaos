using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityThreading;
using DG.Tweening;
using System;
using System.Diagnostics;

public class NodeManager : MonoBehaviour
{
    // Quick and dirty singeton instance
    public static NodeManager Instance;
    
    public string nodeName;
    public List<string> pods = new List<string>();
    public List<string> podNamesInPlay = new List<string>();
    public GameObject[] podPrefabs;
    public GameObject podCanvasPrefab;
    public float podSizeScale = 0.5f;
    public float spawnRadius = 5f;
    public Color[] colorChoices;
    public Color gridColor1;
    public Color gridColor2;
    public List<string> currentPods = new List<string>();

    private System.Random rnd;
    private bool podListReady;
    private ActionThread podInfoThread;

    private int SeedFromName(string name)
    {
        var seed = 0;
        for (var i = 0; i < name.Length; i++)
        {
            int code = name[i];
            seed += code;
        }

        UnityEngine.Debug.Log($"seed: {seed}");
        return seed;
    }

    // Start is called before the first frame update
    void Start()
    {
        Instance = this;

        if (KubeManager.Instance != null)
            nodeName = KubeManager.Instance.currentNode;

        var seed = SeedFromName(nodeName);
        rnd = new System.Random(seed);
        colorChoices = ColorExtensions.GetRandomPastelColors(50, seed);

        var intensityFactor1 = rnd.Next(4, 8) * 1.5f;
        var intensityFactor2 = rnd.Next(4, 8) * 1.5f;

        // Colors to be assigned to background grid here. Removed for OSS version.

        // Invoke repeating pattern for checking for new pods to spawn in-game
        InvokeRepeating("PodSpawn", 0f, 10f);
    }

    void PodSpawn()
    {
        podInfoThread = UnityThreadHelper.CreateThread((Action)GetRunningPodInfo);
    }

    private void GetRunningPodInfo()
    {
        pods = ExecKubeCtl($"pods -n {KubeManager.Instance.kubeNamespace} --no-headers --field-selector spec.nodeName={nodeName},status.phase=Running -o custom-columns=name:.metadata.name,ns:.metadata.namespace,rs:.metadata.ownerReferences[0].name");
        podListReady = true;
    }

    public List<string> ExecKubeCtl(string args, bool getCount = false)
    {
        var tempList = new List<string>();
        Process process = new Process();

        process.StartInfo.FileName = KubeManager.Instance.kubectlExecutableName;
        process.StartInfo.Arguments = $"--context {KubeManager.Instance.kubeContextName} get {args}";
        process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.UseShellExecute = false;
        process.Start();
        while (!process.StandardOutput.EndOfStream)
        {
            string line = process.StandardOutput.ReadLine();
            if (!string.IsNullOrWhiteSpace(line))
            {
                tempList.Add(line);
            }
        }

        if (getCount)
        {
            return new List<string>() { tempList.Count.ToString() };
        }
        else
        {
            return tempList;
        }
    }

    

    private void Update()
    {
        if (podListReady)
        {
            podListReady = false;
            foreach (var pod in pods)
            {
                var split = System.Text.RegularExpressions.Regex.Split(pod, @"\s{2,}");
                var podName = split[0].Trim();
                if (!podNamesInPlay.Contains(podName))
                {
                    podNamesInPlay.Add(podName);
                }
                else
                {
                    UnityEngine.Debug.Log($"Pod: {podName} already spawned");
                    continue;
                }

                var ns = split[1];
                var rs = split[2];

                UnityEngine.Debug.Log($"spawning pod : {pod}");
                var spawnPosition = 
                    new Vector2(transform.position.x, transform.position.y) 
                        + UnityEngine.Random.insideUnitCircle * spawnRadius;

                var podGo = (GameObject)Instantiate(podPrefabs[UnityEngine.Random.Range(0, podPrefabs.Length)], spawnPosition, Quaternion.identity);

                podGo.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                podGo.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, UnityEngine.Random.Range(-15f, 15f)));
                podGo.transform
                    .DOScale(podSizeScale, 1.5f)
                    .SetEase(Ease.InBounce);


                var podCanvasGo = (GameObject)Instantiate(podCanvasPrefab, spawnPosition, Quaternion.identity);

                var movement = podGo.GetComponent<PodMovement>();
                movement.podName = podName;
                movement.canvasGo = podCanvasGo;

                var lerp = podCanvasGo.GetComponent<BasicFollowLerp>();
                lerp.target = podGo.transform;

                var tmpText = podCanvasGo.transform.GetChild(0).GetComponent<TMP_Text>();
                var podScript = podGo.transform.GetChild(0).GetComponent<Pod>();
                podScript.lr = lerp.lr;
                

                var podLrTarget = podGo.transform.GetChild(1).transform;
                var podLrTextTarget = podCanvasGo.transform.GetChild(1).transform.GetChild(0).transform;

                podScript.podText = tmpText;
                podScript.lrPodTarget = podLrTarget;
                podScript.lrTextTarget = podLrTextTarget;
                podScript.podName = podName;
                tmpText.SetText($"Pod: {podName}\nNamespace:{ns}\nStatus: Running\nReplicaSet: {rs}\nNode: {nodeName}");
            }
        }
    }
}
