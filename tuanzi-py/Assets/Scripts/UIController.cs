using System;
using UnityEngine;
using DG.Tweening;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class UIController : MonoBehaviour
{
    public GameObject MainMenu;

    public GameObject PlayGround;

    public PyType currentUIType { get; private set; } = PyType.YUNMU;

    // Start is called before the first frame update
    void Awake()
    {
        ShowMenu();
    }

    void ShowMenu()
    {
       MainMenu.SetActive(true);
       var r = MainMenu.GetComponent<RectTransform>();
       var t = DOTween.To(() => -1200, x => r.anchoredPosition = new Vector2(x, r.anchoredPosition.y), 0, 0.8f);
        t.SetTarget(r);
        
        if (PlayGround.activeSelf)
        {
            var rr = PlayGround.GetComponent<RectTransform>();
            rr.DOAnchorPosX(2200, 0.8f).onComplete = () => PlayGround.SetActive(false);
        }
    }

    void ShowPlayGround(PyType py)
    {
        currentUIType = py;
        
        if (MainMenu.activeSelf)
        {
            MainMenu.GetComponent<RectTransform>().DOAnchorPosX(-1200, 0.8f).onComplete = () => MainMenu.SetActive(false);
        }
        PlayGround.SetActive(true);
        var r = PlayGround.GetComponent<RectTransform>();
        var t = DOTween.To(() => 2200, x => r.anchoredPosition = new Vector2(x, r.anchoredPosition.y), 0, 0.8f);
        t.SetTarget(r);
    }

    public void MainMenuClick(GameObject go)
    {
        PyType pyType = (PyType)Enum.Parse(typeof(PyType), go.name, true);

        Debug.Log("You have clicked the : " + pyType);
        ShowPlayGround(pyType);
    }

    public void OnSwipe(Vector2 delta)
    {
        if (delta.x > 500)
        {
            ShowMenu();
        }
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
#if UNITY_EDITOR
            EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }
    }
}
