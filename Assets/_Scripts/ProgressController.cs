

using UnityEngine;
using UnityEngine.UI;

public class ProgressController : MonoBehaviour
{
    public float totalStage = 10;
    public float currentStage;
    public Image image;
    public float scale = 0;

    private void Start()
    {
        currentStage = 0;
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            NextProgress();
        }
    }

    public void NextProgress()
    {
        currentStage += 1;
        scale = totalStage > 0 ? currentStage / totalStage : 0f;
        if (image != null) image.fillAmount = scale;
    }
}
