using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Diagnostics;
using TMPro;
using UnityThreading;
using System;

public class Node : MonoBehaviour
{
    public GameObject door;
    public string nodeName;
    public TMP_Text nodeTextLabel;
    public float updateDelay = 3f;
    public NodeEnterProximity nodeEnterProximity;
    private ActionThread nodeInfoThread;

    public string nodeCpu;
    public string nodeMem;
    public string ephStorage;
    public string address;
    public string arch;
    public string podsOnNode;
    public string text;

    public bool nodeInfoReady;

    // Start is called before the first frame update
    void Start()
    {
        nodeInfoThread = UnityThreadHelper.CreateThread((Action)GetNodeDetails);
        InvokeRepeating("UpdateNode", 3f, updateDelay);
    }

    private void UpdateNode()
    {
        nodeInfoThread = UnityThreadHelper.CreateThread((Action)GetNodeDetails);
    }

    private IEnumerator UpdateNode(float delay)
    {
        yield return new WaitForSeconds(delay);
        GetNodeDetails();
        StopAllCoroutines();
    }

    public void GetNodeDetails()
    {
        text = "";
        UnityEngine.Debug.Log($"Node: {nodeName}");

        nodeCpu = ExecKubeCtl($"nodes {nodeName} -o custom-columns=:status.capacity.cpu")[0];
        nodeMem = ExecKubeCtl($"nodes {nodeName} -o custom-columns=:status.capacity.memory")[0];
        ephStorage = ExecKubeCtl($"nodes {nodeName} -o custom-columns=:status.capacity.ephemeral-storage")[0];
        address = ExecKubeCtl($"nodes {nodeName} -o custom-columns=:status.addresses[].address")[0];
        arch = ExecKubeCtl($"nodes {nodeName} -o custom-columns=:status.nodeInfo.architecture")[0];
        podsOnNode = ExecKubeCtl($"pods --all-namespaces -o wide --field-selector spec.nodeName={nodeName}", true)[0];

        text += $"Node: {nodeName}\n";
        text += $"Architecture: {arch}\n";
        text += $"Running pods: {podsOnNode}\n";
        text += $"CPUs: {nodeCpu}\n";
        text += $"Memory: {nodeMem}\n";
        text += $"Ephemeral Storage: {ephStorage}\n";
        text += $"First interface IP: {address}\n";

        UnityEngine.Debug.Log(text);
        nodeInfoReady = true;
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

    // Update is called once per frame
    void Update()
    {
        if (nodeInfoReady)
        {
            nodeInfoReady = false;
            nodeTextLabel.SetText(string.Empty);
            nodeTextLabel.SetText(text);
            nodeEnterProximity.nodeName = nodeName;
        }
    }

    public void OpenDoor()
    {
        door.transform.DOLocalMoveX(-2.3f, 1f, false);
    }

    public void CloseDoor()
    {
        door.transform.DOLocalMoveX(0f, 1f, false);
    }
}
