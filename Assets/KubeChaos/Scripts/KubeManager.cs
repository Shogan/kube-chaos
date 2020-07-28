using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using UnityThreading;
using System;
using DG.Tweening;

public class KubeManager : MonoBehaviour
{
    // Quick and dirty singleton instance
    public static KubeManager Instance;

    public string kubeContextName = "kubernetes-admin@kubernetes";
    public string kubeNamespace = "demo";
    public string kubectlExecutableName = "kubectl.exe";
    public string currentNode;
    public string[] nodes;
    public GameObject nodePrefab;
    private ActionThread nodeListThread;
    private bool readyToSpawn = false;
    private List<string> nodesList = new List<string>();
    public Vector2 nodeSpacing = new Vector2(5f, 5f);
    public Vector2 startSpawnPos;
    public bool gameStarted;
    public bool joystickEnabled;
    public bool sendStats = true;

    private void GetNodesList()
    {
        nodesList = ExecKubeCtl("nodes -o custom-columns=:metadata.name");
        UnityEngine.Debug.Log("Node List completed: " + nodesList.Count);
        readyToSpawn = true;
    }

    private void Awake()
    {
        UnityEngine.Debug.Log("KubeManager awake");
        Instance = this;
    }

    public void FetchNodeList()
    {
        nodeListThread = UnityThreadHelper.CreateThread((Action)GetNodesList);
    }

    // Start is called before the first frame update
    void Start()
    {
        UnityEngine.Debug.Log("KubeManager startup");
        DontDestroyOnLoad(this.gameObject);

        var joystickNames = Input.GetJoystickNames();
        if (joystickNames.Length > 0)
        {
            joystickEnabled = true;
        }
    }

    private void Update()
    {
        if (readyToSpawn && gameStarted)
        {
            readyToSpawn = false;
            var count = 0;
            foreach (var n in nodesList)
            {
                count++;
                var xPos = startSpawnPos.x + (count * 6f) * nodeSpacing.x;
                var yPos = startSpawnPos.y + (6f) + nodeSpacing.y;
                var node = (GameObject)Instantiate(nodePrefab, new Vector2(xPos, yPos), Quaternion.identity);
                node.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                node.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, UnityEngine.Random.Range(-15f, 15f)));
                node.transform
                    .DOScale(0.65f, 1.5f)
                    .SetEase(Ease.InBounce);

                node.transform
                    .DOLocalRotate(new Vector3(0, 0, UnityEngine.Random.Range(-15f, 15f)), 7f, RotateMode.LocalAxisAdd)
                    .SetEase(Ease.InOutQuad)
                    .SetLoops(-1, LoopType.Yoyo);

                var nodeScript = node.GetComponent<Node>();
                nodeScript.nodeName = n;
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
    }

    public List<string> ExecKubeCtl(string args, bool getCount = false)
    {
        var tempList = new List<string>();
        Process process = new Process();
        
        process.StartInfo.FileName = kubectlExecutableName;
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
}
