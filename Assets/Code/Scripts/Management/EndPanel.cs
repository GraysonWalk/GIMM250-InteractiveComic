using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class EndPanel : MonoBehaviour
{
    [SerializeField] private TextMeshPro narratorBox;
    
    [SerializeField] private string[] loopText;
    [SerializeField] UnityEvent finalChoice;

    private int index = 0;

    public void UpdateEndPanelOnLoop(LoopCount loopNum)
    {

        if (loopNum == LoopCount.Loop4)
        {
            finalChoice.Invoke();
            if (loopText[index] != null && narratorBox != null)
                {
                narratorBox.text = loopText[index];
                }
        } else
        {

            if (loopText[index] != null && narratorBox != null)
            {
                narratorBox.text = loopText[index];
            }
            index++;
        }

    }
}
