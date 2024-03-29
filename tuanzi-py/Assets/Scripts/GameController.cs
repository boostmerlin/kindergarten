using DG.Tweening;
using DG.Tweening.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
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
        pyTypeName.Add(PyType.DAN_YUNMU, "����ĸ");
        pyTypeName.Add(PyType.FU_YUNMU, "����ĸ");
        pyTypeName.Add(PyType.BI_YUNMU, "����ĸ");
        pyTypeName.Add(PyType.SHENGMU, "��ĸ");
        pyTypeName.Add(PyType.ZHENGTI_YINJIE, "��������");
        pyTypeName.Add(PyType.SANPIN_YINJIE, "��ƴ����");

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
            if (!dataModel.PyDatas.ContainsKey(pyType))
            {
                continue;
            }
            foreach (var pyData in dataModel.PyDatas[pyType])
            {
                if (pyData.name == name)
                {
                    return pyData;
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
            if (!dataModel.PyDatas.ContainsKey(pyType))
            {
                continue;
            }
            foreach (var pyData in dataModel.PyDatas[pyType])
            {
                if (flag == 0)
                {
                    return pyData;
                }
                if (pyData.name == name)
                {
                    flag--;
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
            if (!dataModel.PyDatas.ContainsKey(pyType))
            {
                continue;
            }
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
            case KeyCode k when k >= KeyCode.Alpha1 && k <= KeyCode.Alpha4:
                if (toneRoot.activeSelf)
                {
                    PlayTone(k - KeyCode.Alpha1);
                }
                break;
        }
    }

    private void PlayTone(int tone)
    {
        List<PyElement> tones;
        if (dataModel.PyTone.TryGetValue(currentSelect, out tones))
        {
            Button button = FindButtonOnRoot(toneRoot.transform, tones[tone]);
            button.Select();
            PlayPyVideo(tones[tone]);
        }
    }

    Button FindButtonOnRoot(Transform root, PyElement element)
    {
        foreach (var button in root.GetComponentsInChildren<Button>())
        {
            var text = button.GetComponentInChildren<Text>();
            if (text != null && text.text == element.spell)
            {
                return button;
            }
        }
        return null;
    }

    void SetButtonColor(Button button, Color targetColor)
    {
        var cb = button.colors;
        DOSetter<Color> setter = delegate (Color color)
        {
            cb.selectedColor = color;
            cb.normalColor = color;
            button.colors = cb;
        };
        var tween = DOTween.To(() => cb.normalColor, setter, targetColor, 1.0f); ;
        tween.PlayForward();
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
        if (currentSelect == pyData)
        {
            return;
        }
        Debug.Log("py click: " + pyData);
        PlayPyVideo(pyData);
        //�������������ʾ����
        List<PyElement> tones;
        if (dataModel.PyTone.TryGetValue(pyData, out tones))
        {
            for (int i = 0; i < toneRoot.transform.childCount; i++)
            {
                var t = toneRoot.transform.GetChild(i);
                var d = tones[i];
                t.GetComponentInChildren<Text>().text = d.spell;
                t.GetComponent<Button>().onClick.RemoveAllListeners();
                int index = i;
                t.GetComponent<Button>().onClick.AddListener(() => PlayTone(index));
                if (i == 0)
                {
                    t.GetComponent<Button>().Select();
                }
            }
            toneRoot.SetActive(true);
        }
        else
        {
            toneRoot.SetActive(false);
        }

        if (currentSelect != null)
        {
            Button lastButton = FindButtonOnRoot(subTypeRoot.transform.parent, currentSelect);
            if (lastButton != null)
            {
                SetButtonColor(lastButton, Color.white);
            }
        }
        Button button = FindButtonOnRoot(subTypeRoot.transform.parent, pyData);
        if (button != null)
        {
            //button.Select();
            SetButtonColor(button, Color.red);
        }
        currentSelect = pyData;
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
        cover.SetActive(false);
        VideoClip vc = Resources.Load<VideoClip>(pyData.Uri);
        if (vc == null)
        {
            Debug.LogError("no video clip found." + pyData.Uri);
            return;
        }
        videoPlayer.clip = vc;
        ResumeVideo();
    }
}
