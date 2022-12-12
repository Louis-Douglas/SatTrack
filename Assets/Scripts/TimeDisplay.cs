using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TimeDisplay : MonoBehaviour
{
    private TleMapper tleMapper;

    private void Start()
    {
        tleMapper = TleMapper.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        string date = tleMapper.simulatedTime.ToShortDateString();
        string time = tleMapper.simulatedTime.ToLongTimeString();
        gameObject.GetComponent<TextMeshProUGUI>().text = date + " - " + time;
    }
}
