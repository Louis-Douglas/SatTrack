using System;
using System.Collections;
using System.Collections.Generic;
using SGPdotNET.Observation;
using UnityEngine;
using UnityEngine.EventSystems;

public class OnClickSatellite : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    private SGPdotNET.Observation.Satellite satelliteData;
    private Satellite satellite;
    private TleMapper tleMapper = TleMapper.Instance;
    LineRenderer lr;

    public void OnPointerDown(PointerEventData eventData)
    {
        satellite = tleMapper.GetSatellite(uint.Parse(name));
        satelliteData = satellite.getSatelliteData();
        if (satelliteData == null)
        {
            Debug.Log("Something went wrong, satellite " + name + " could not be found!");
            return;
        }

        if (tleMapper.selectedSatellite == null)
        {
            satellite.Select();

        }
        else if (tleMapper.selectedSatellite.getSatelliteObject() != gameObject)
        {
            // Set last selected satellite back to normal
            tleMapper.selectedSatellite.Deselect();

            // Select this satellite
            satellite.Select();
        }

        satellite.RenderOrbitPath();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        gameObject.transform.localScale *= tleMapper.enlargedSatelliteSize;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        gameObject.transform.localScale /= tleMapper.enlargedSatelliteSize;
    }
}
