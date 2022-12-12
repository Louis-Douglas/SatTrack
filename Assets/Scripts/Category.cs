using System;
using System.Collections.Generic;
using SGPdotNET.Exception;
using SGPdotNET.Observation;
using SGPdotNET.TLE;
using UnityEngine;
using System.Threading.Tasks;

public class Category
{
    public HashSet<uint> satellites = new HashSet<uint>();
    private String label;
    private TleMapper tleMapper = TleMapper.Instance;
    private bool isActive;

    public bool Initiated { get; protected set; }

    public Category(String url, String label)
    {
        Initialize = CreateInstanceAsync(url, label);
    }

    public Task Initialize { get; }

    private async Task CreateInstanceAsync(String url, String label)
    {
        this.label = label;
        isActive = false;

        Uri uri = new Uri(url);
        RemoteTleProvider provider = new RemoteTleProvider(true, uri);

        Dictionary<int, Tle> tles = await provider.GetTlesAsync();

        foreach (Tle tle in tles.Values)
        {

            if (tleMapper.loadedSatellites.ContainsKey(tle.NoradNumber))
            {
                satellites.Add(tle.NoradNumber);
                continue;
            }

            // Make the satellite object from a prefab
            GameObject satelliteObject = MonoBehaviour.Instantiate(
            tleMapper.satellitePrefab,
            tleMapper.originalPos,
            Quaternion.identity);

            // Make the satellite the child of satellites object
            satelliteObject.transform.parent = tleMapper.satellitesObject.transform;
            // Set the norad number as the objects name
            satelliteObject.transform.name = tle.NoradNumber.ToString();


            SGPdotNET.Observation.Satellite satelliteData = new SGPdotNET.Observation.Satellite(tle);


            //float altitude;

            // Catch invalid data before writing satellite to satellites list
            try
            {
                satelliteData.Predict(DateTime.Now);
            }
            catch (SatellitePropagationException e)
            {
                Debug.Log("Satellite " + satelliteData.Name + " has invalid data!");
                Debug.Log(e.Data);
                tleMapper.DestroySatelliteObject(satelliteObject);
                continue;
            }
            catch (DecayedException e)
            {
                Debug.Log("Satellite " + satelliteData.Name + " has decayed!");
                Debug.Log(e.Data);
                tleMapper.DestroySatelliteObject(satelliteObject);
                continue;
            }

            Satellite satellite = new Satellite(satelliteData, satelliteObject);

            // TODO: Make modifier of satellite object size based on distance

            //float distance = Vector3.Distance(tleMapper.gameObject.transform.position, satelliteObject.transform.position);
            // Make satellite object bigger the further away from earth it is
            //satelliteObject.transform.localScale = (satelliteObject.transform.localScale * (distance / 5));


            tleMapper.loadedSatellites.Add(satelliteData.Tle.NoradNumber, satellite);
            satellites.Add(tle.NoradNumber);
        }

        SetActive(isActive);

        Initiated = true;
    }

    public HashSet<uint> GetSatellites()
    {
        return satellites;
    }

    public String GetLabel()
    {
        return this.label;
    }

    public void SetActive(bool isActive)
    {
        this.isActive = isActive;


        foreach (uint noradNumber in satellites)
        {

            // Ensure you dont change satellites that other active categories are showing
            bool isLoadedByOtherCategory = false;
            foreach (Category category in tleMapper.categories)
            {
                if (category.isActive && !category.label.Equals(label) && category.satellites.Contains(noradNumber))
                {
                    isLoadedByOtherCategory = true;
                }
            }

            if (isLoadedByOtherCategory)
            {
                continue;
            }


            Satellite satellite = tleMapper.GetSatellite(noradNumber);
            satellite.getSatelliteObject().SetActive(isActive);

            if (tleMapper.selectedSatellite != null
                && tleMapper.selectedSatellite.getSatelliteData().Name.Equals(satellite.getSatelliteData().Name)
                && !isActive)
            {
                tleMapper.selectedSatellite.Deselect();
            }
        }
    }

    public bool IsActive()
    {
        return this.isActive;
    }

}

