using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleAllCategories : MonoBehaviour
{
    private TleMapper tleMapper;
    private Toggle toggle;
    public Sprite selectedSprite;

    void Start()
    {
        toggle = GetComponent<Toggle>();
        tleMapper = TleMapper.Instance;

        toggle.onValueChanged.AddListener(OnTargetToggleValueChanged);
        toggle.toggleTransition = Toggle.ToggleTransition.None;
    }


    void OnTargetToggleValueChanged(bool newValue)
    {
        tleMapper.ToggleAllCategories(newValue);
        foreach (Transform child in transform.parent.transform)
        {
            Toggle toggle = child.GetComponent<Toggle>();
            toggle.isOn = newValue;
        }

        Image targetImage = toggle.targetGraphic as Image;
        if (targetImage != null)
        {
            if (newValue)
            {
                targetImage.overrideSprite = selectedSprite;
                targetImage.enabled = false;
            }
            else
            {
                targetImage.overrideSprite = null;
                targetImage.enabled = true;
            }
        }
    }

}
