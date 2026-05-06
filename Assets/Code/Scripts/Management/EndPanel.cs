using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class EndPanel : MonoBehaviour
{
    [SerializeField] private TextMeshPro narratorBox;
    [SerializeField] private GameObject comicController;

    private ComicManager comicManager;
    
    [SerializeField] private string[] loopText;
    [SerializeField] UnityEvent finalChoice;

    private LoopCount loopNum;

    public void UpdateEndPanelOnLoop()
    {
        comicManager = comicController.GetComponent<ComicManager>();
        loopNum = comicManager.GetLoopCount();

        switch (loopNum)
        {
            case LoopCount.Loop0:
                narratorBox.text = loopText[0];
                break;

            case LoopCount.Loop1:
                narratorBox.text = loopText[1];
                break;

            case LoopCount.Loop2:
                narratorBox.text = loopText[2];
                break;

            case LoopCount.Loop3:
                narratorBox.text = loopText[3];
                break;

            case LoopCount.Loop4:
                finalChoice.Invoke();
                narratorBox.text = loopText[4];
                break;

        }

    }
}
