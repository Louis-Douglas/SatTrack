using UnityEngine;
using System.Collections;
using System;
using SGPdotNET.Observation;
using SGPdotNET.TLE;
using SGPdotNET.CoordinateSystem;
using UnityEngine.UIElements;
using System.Collections.Generic;
using SGPdotNET.Exception;
using UnityEngine.ProBuilder.Shapes;

public class Satellite
{
    SGPdotNET.Observation.Satellite satelliteData;
    GameObject satelliteObject;
    private TleMapper tleMapper = TleMapper.Instance;
    private List<Vector3> points;
    private DateTime cooldown;
    private int timeBetweenLinePoints = 15;
    GeodeticCoordinate location;
    public Vector3 currentVector;
    public bool firstRender;
    int minutesPerOrbit;
    LineRenderer lr;
    private int simplifiedPointsCount;
    private bool canRenderOrbit;
    float amountSimplified;
    public DateTime lineTime;
    public DateTime reverseLineTime;
    DateTime timeSinceLastUpdate;
    Color defaultColor;

    public Satellite(SGPdotNET.Observation.Satellite satelliteData, GameObject satelliteObject)
    {
        this.satelliteData = satelliteData;
        this.satelliteObject = satelliteObject;
        firstRender = true;
        lr = tleMapper.satellitePathLine.GetComponent<LineRenderer>();
        lr.useWorldSpace = false;
        points = new List<Vector3>();
        canRenderOrbit = true;
        defaultColor = satelliteObject.GetComponent<Renderer>().material.color;

        // Todo: fade in/out
        //lr.endColor = Color.clear;
        //lr.endWidth = 0;
        //lr.startColor = Color.clear;
        //lr.startWidth = 0;
    }

    public SGPdotNET.Observation.Satellite getSatelliteData()
    {
        return satelliteData;
    }

    public GameObject getSatelliteObject()
    {
        return satelliteObject;
    }

    public Vector3 GetVectorOfSatellite(DateTime time)
    {
        location = satelliteData.Predict(time).ToGeodetic();

        float altitude = (float)location.Altitude;

        float radianLatitude = (float) location.Latitude.Radians;
        float radianLongitude = (float) location.Longitude.Radians;

        float ninetyDegreesInRadians = (Mathf.PI / 180) * 90;
        radianLatitude -= ninetyDegreesInRadians;

        float radius = (tleMapper.earthRadius + altitude) / tleMapper.scale;

        float x = Mathf.Sin(radianLatitude) * Mathf.Cos(radianLongitude) * radius;
        float z = Mathf.Sin(radianLatitude) * Mathf.Sin(radianLongitude) * radius;
        float y = Mathf.Cos(radianLatitude) * radius;

        // ------

        return new Vector3(x, y, z);
    }

    public void RenderOrbitPath()
    {

        if (!canRenderOrbit)
        {
            return;
        }

        // Get how many minutes it takes a satellite to complete an orbit
        minutesPerOrbit = Mathf.RoundToInt((float)satelliteData.Orbit.Period);

        if (firstRender)
        {
            reverseLineTime = tleMapper.simulatedTime;
            lineTime = tleMapper.simulatedTime;
            firstRender = false;
            points.Clear();

            for (int i = 0; i < minutesPerOrbit * 4; i++)
            {
                try
                {
                    Vector3 point = GetVectorOfSatellite(lineTime);
                    points.Add(point);
                    lineTime = lineTime.AddSeconds(timeBetweenLinePoints * tleMapper.orbitLineLength);
                }
                catch (SatellitePropagationException e)
                {
                    Debug.Log("Couldn't continue rendering orbit path due to propagation exception:");
                    Debug.Log(e.ToString());
                    canRenderOrbit = false;
                    break;
                }
            }
            
            lr.positionCount = points.Count;
            lr.SetPositions(points.ToArray());
            tleMapper.satellitePathLine.SetActive(true);
        } else
        {
            UpdateOrbitPath();
        }
    }

    public void UpdateOrbitPath()
    {
        float satToLineStartDistance = GetDistanceBetweenLinePoints(points[0]);
        float satToLine2Distance = GetDistanceBetweenLinePoints(points[1]);

        if (tleMapper.speed > 0)
        {
            // If the distance to the first line point is greater than the distance to the second line point, delete first point and add point to end
            if (satToLineStartDistance > satToLine2Distance)
            {
                lineTime = lineTime.AddSeconds(timeBetweenLinePoints * tleMapper.orbitLineLength);
                Vector3 lastPoint = GetVectorOfSatellite(lineTime);
                points.RemoveAt(0);
                points.Add(lastPoint);
                lr.SetPositions(points.ToArray());
            }
        } else if (tleMapper.speed < 0)
        {
            if (satToLineStartDistance < satToLine2Distance)
            {
                try
                {
                    reverseLineTime = reverseLineTime.AddSeconds(-(timeBetweenLinePoints * tleMapper.orbitLineLength));
                    Vector3 firstPoint = GetVectorOfSatellite(reverseLineTime);
                    points.RemoveAt(points.Count - 1);
                    points.Insert(0, firstPoint);
                    lr.SetPositions(points.ToArray());
                } catch (ArgumentException e)
                {
                    Debug.Log("Invalid time found for orbit path:");
                    Debug.Log(e.ToString());
                    tleMapper.selectedSatellite.Deselect();
                }
                
            }
        }


    }

    private float GetDistanceBetweenLinePoints(Vector3 point)
    {
        Vector3 adjustedPointPosition = tleMapper.gameObject.transform.TransformPoint(point);
        return Vector3.Distance(satelliteObject.transform.position, adjustedPointPosition);
       
    }

    public void Select()
    {
        Debug.Log("Selecting satellite: " + satelliteData.Name);
        tleMapper.selectedSatellite = this;
        satelliteObject.transform.localScale *= tleMapper.enlargedSatelliteSize;
        satelliteObject.GetComponent<Renderer>().material.color = new Color(255, 0, 0, 1.0f);
        tleMapper.ToggleSatelliteInfoSlider(true);
        UpDateSatelliteInfoText();
    }

    public void Deselect()
    {
        satelliteObject.transform.localScale /= tleMapper.enlargedSatelliteSize;
        satelliteObject.GetComponent<Renderer>().material.color = defaultColor;

        firstRender = true;
        points.Clear();

        tleMapper.ToggleSatelliteInfoSlider(false);
        tleMapper.selectedSatellite = null;
        tleMapper.satellitePathLine.SetActive(false);
        tleMapper.SetSatelliteInfoText("Select satellite to view information");
    }

    public void UpDateSatelliteInfoText()
    {
        int altitude = Mathf.RoundToInt((float)satelliteData.Predict(tleMapper.simulatedTime).ToGeodetic().Altitude);
    
        tleMapper.SetSatelliteInfoText(
            "Name: " + satelliteData.Name + "\n" +
            "Norad Number: " + satelliteData.Tle.NoradNumber + "\n" +
            "Altitude: " + altitude + " km\n" +
            "Orbits: " + satelliteData.Tle.OrbitNumber + "\n" +
            "Orbit Duration: " + Mathf.Round((float)satelliteData.Orbit.Period) + " minutes");
    }
}

