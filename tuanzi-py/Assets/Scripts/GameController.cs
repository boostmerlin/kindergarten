using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using UnityEngine.Video;

public class GameController : MonoBehaviour
{
    private UIController uIController;

    private DataModel dataModel;

    public GameObject subTypeRoot;

    private Dictionary<PyType, string> pyTypeName = new Dictionary<PyType, string>();

    private PyElement currentSelect;

    private VideoPlayer videoPlayer;

    public GameObject cover;

    public GameObject toneRoot;

    public GameObject playButton;

    void Awake()
    {
        uIController = Camera.main.GetComponent<UIController>();
        dataModel = Camera.main.GetComponent<DataModel>();
        pyTypeName.Add(PyType.DAN_YUNMU, "单韵母");
        pyTypeName.Add(PyType.FU_YUNMU, "复韵母");
        pyTypeName.Add(PyType.BI_YUNMU, "鼻韵母");
        pyTypeName.Add(PyType.SHENGMU, "声母");
        pyTypeName.Add(PyType.ZHENGTI_YINJIE, "整体音节");
        pyTypeName.Add(PyType.SANPIN_YINJIE, "三拼音节");

        videoPlayer = GetComponentInChildren<VideoPlayer>();
        Debug.Log(uIController.currentUIType);
        subTypeRoot.SetActive(false);
    }

    private List<PyType> MapTypes()
    {
        if (uIController.currentUIType == PyType.YUNMU)
        {
            return new List<PyType> { PyType.DAN_YUNMU, PyType.FU_YUNMU, PyType.BI_YUNMU };
        }
        else
        {
            return new List<PyType> { uIController.currentUIType };
        }
    }

    PyElement? FindData(string name)
    {
        foreach (var pyType in MapTypes())
        {
            if (dataModel.PyDatas.ContainsKey(pyType))
            {
                foreach (var pyData in dataModel.PyDatas[pyType])
                {
                    if (pyData.name == name)
                    {
                        return pyData;
                    }
                }
            }
        }
        return null;
    }

    Nullable<PyElement> FindNext(string name)
    {
        int flag = 1;
        foreach (var pyType in MapTypes())
        {
            if (dataModel.PyDatas.ContainsKey(pyType))
            {
                foreach (var pyData in dataModel.PyDatas[pyType])
                {
                    if (flag == 0)
                    {
                        Debug.Log(pyData);
                        return pyData;
                    }
                    if (pyData.name == name)
                    {
                        Debug.Log(name);
                        flag--;
                    }
                }
            }
        }
        return null;
    }

    PyElement? FindPrev(string name)
    {
        PyElement? pre = null;
        foreach (var pyType in MapTypes())
        {
            if (dataModel.PyDatas.ContainsKey(pyType))
            {
                foreach (var pyData in dataModel.PyDatas[pyType])
                {
                    if (pyData.name == name)
                    {
                        return pre;
                    }
                    else
                    {
                        pre = pyData;
                    }
                }
            }
        }
        return null;
    }

    void OnGUI()
    {
        if (!Input.anyKeyDown)
        {
            return;
        }
        Event e = Event.current;
        if (e.keyCode == KeyCode.None || e.rawType != EventType.KeyDown)
        {
            return;
        }
        switch (e.keyCode)
        {
            case KeyCode k when k >= KeyCode.A && k <= KeyCode.Z:
                string a = ((char)e.keyCode).ToString();
                var d = FindData(a);
                if (d != null)
                {
                    SetSelect(d.Value);
                }
                break;
            case KeyCode.RightArrow:
                d = FindNext(currentSelect.name);
                if (d.HasValue)
                {
                    SetSelect(d.Value);
                }
                break;
            case KeyCode.LeftArrow:
                d = FindPrev(currentSelect.name);
                if (d.HasValue)
                {
                    SetSelect(d.Value);
                }
                break;
            case KeyCode.Space:
                if (cover.activeSelf)
                {
                    break;
                }
                if (videoPlayer.isPlaying)
                {
                    PauseVideo();
                }
                else
                {
                    ResumeVideo();
                }
                break;
        }
    }

    private void OnEnable()
    {
        toneRoot.SetActive(false);

        PyElement first = new PyElement();
        foreach (var pyType in MapTypes())
        {
            Debug.Log(pyType);
            GameObject go = Instantiate(subTypeRoot);

            go.SetActive(true);
            go.GetComponentInChildren<Text>().text = pyTypeName[pyType];
            go.transform.SetParent(subTypeRoot.transform.parent);

            var letters = go.GetComponentInChildren<FlowLayoutGroup>().transform;

            GameObject letterTemplate = letters.GetChild(0).gameObject;
            if (!dataModel.PyDatas.ContainsKey(pyType))
            {
                continue;
            }

            foreach (var pyData in dataModel.PyDatas[pyType])
            {
                if (first.id == 0)
                {
                    first = pyData;
                }
                GameObject l = Instantiate(letterTemplate);
                l.transform.SetParent(letters, false);
                l.SetActive(true);
                l.GetComponentInChildren<Text>().text = pyData.spell;
                l.GetComponent<Button>().onClick.RemoveAllListeners();
                l.GetComponent<Button>().onClick.AddListener(() => SetSelect(pyData));
            }
        }

    }

    private void OnDisable()
    {
        currentSelect = new PyElement();
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
        for (int i = 1; i < subTypeRoot.transform.parent.childCount; i++)
        {
            var t = subTypeRoot.transform.parent.GetChild(i);
            if (t)
            {
                Destroy(t.gameObject);
            }
        }
        ClearOutRenderTexture(videoPlayer.targetTexture);
    }

    void SetSelect(PyElement pyData)
    {
        Debug.Log("py click: " + pyData);
        PlayPyVideo(pyData);

        //如果有声调，显示声调
        List<PyElement> tones;
        if (dataModel.PyTone.TryGetValue(pyData, out tones))
        {
            for (int i = 0; i < toneRoot.transform.childCount; i++)
            {
                var t = toneRoot.transform.GetChild(i);
                var d = tones[i];
                t.GetComponentInChildren<Text>().text = d.spell;
                t.GetComponent<Button>().onClick.RemoveAllListeners();
                t.GetComponent<Button>().onClick.AddListener(() => PlayPyVideo(d));
            }
            toneRoot.SetActive(true);
        }
        else
        {
            toneRoot.SetActive(false);
        }
    }

    private void ClearOutRenderTexture(RenderTexture renderTexture)
    {
        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = renderTexture;
        GL.Clear(true, true, Color.magenta);
        RenderTexture.active = rt;

        cover.SetActive(true);
        playButton.SetActive(false);
    }

    public void ResumeVideo()
    {
        videoPlayer.Play();
        playButton.SetActive(false);
    }

    private void PauseVideo()
    {
        videoPlayer.Pause();
        playButton.SetActive(true);
    }

    void PlayPyVideo(PyElement pyData)
    {
        if (currentSelect == pyData)
        {
            return;
        }
        cover.SetActive(false);
        VideoClip vc = Resources.Load<VideoClip>(pyData.Uri);
        if (vc == null)
        {
            Debug.LogError("no video clip found." + pyData.Uri);
            return;
        }
        videoPlayer.clip = vc;
        ResumeVideo();
        currentSelect = pyData;
    }
}
