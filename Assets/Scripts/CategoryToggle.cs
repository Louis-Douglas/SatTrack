using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.UI;

public class CategoryToggle : MonoBehaviour
{
    string label;
    Category category;
    private Toggle toggle;
    public Sprite selectedSprite;
    private TleMapper tleMapper = TleMapper.Instance;

    void Start()
    {
        label = gameObject.GetComponentInChildren<Text>().text;
        category = tleMapper.GetCategory(label);
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(OnTargetToggleValueChanged);
        toggle.toggleTransition = Toggle.ToggleTransition.None;
    }

    void OnTargetToggleValueChanged(bool newValue)
    {
        tleMapper.SetCategory(label, newValue);

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
