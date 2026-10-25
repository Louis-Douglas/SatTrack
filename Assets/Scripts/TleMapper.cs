// SatTrack

using System;

using System.Collections.Generic;

using SGPdotNET.Exception;

using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.UI;

//using System.Threading;

public class TleMapper : MonoBehaviour
{

    private static TleMapper instance;

    public Vector3 originalPos;

    public GameObject satellitePrefab;
    public GameObject satellitesObject;
    public GameObject categoryToggleBody;
    public GameObject categoryTogglePrefab;
    public GameObject satelliteInfoText;
    public GameObject satellitePathLine;
    public GameObject satellitePathSlider;

    public GameObject minScreenIcon;
    public GameObject maxScreenIcon;

    public float lineTolerance = 0.1f;

    public int speed = 1;

    public float scale = 315.4735024045f;
    public DateTime simulatedTime;
    private DateTime lastCheckedTime;
    private int previousSatellitesToProcess;

    public float earthRadius = 6371.0f; // In KM

    public List<Category> categories = new List<Category>();
    public Dictionary<uint, Satellite> loadedSatellites = new Dictionary<uint, Satellite>();

    public Satellite selectedSatellite;
    public float enlargedSatelliteSize;

    public int orbitLineLength = 1;

    public int cpuThreads;

    List<Satellite> destroyList;


    // Make TleMapper a singleton to allow easy access from other classes
    public static TleMapper Instance {
        get
        {
            return instance;
        }
    }


    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
        }
    }

    // Start is called before the first frame update
    async void Start()
    {
        // Logging
        DateTime startBootup = DateTime.Now;

        Application.runInBackground = true;
        originalPos = satellitesObject.transform.position;

        destroyList = new List<Satellite>();

        Task<List<Category>> loadingSatellites = LoadSatellites();

        categories = await loadingSatellites;

        simulatedTime = DateTime.Now;
        lastCheckedTime = simulatedTime;
        previousSatellitesToProcess = 0;

        cpuThreads = Mathf.Max(SystemInfo.processorCount/2, 1);

        // Logging
        Debug.Log("Initiallised in " + (DateTime.Now - startBootup) + " seconds");
        
    }

    int processCooldown = 0;
    void FixedUpdate()
    {
        simulatedTime = simulatedTime.AddSeconds(speed * Time.deltaTime);

        // Earth rotation
        // Earth rotates 360 degrees every 86,164.09 seconds
        //float amountToRotate = ((speed * Time.deltaTime) / 86164.09f) * 360;
        //gameObject.transform.Rotate(0, -amountToRotate, 0);

        List<Satellite> satellitesToProcess = new List<Satellite>();
        HashSet<uint> noradNumbersToProcess = new HashSet<uint>();

        // Merge all satellites in active category into noradNumbersToProcess hashset 
        foreach (Category category in categories)
        {
            if (!category.IsActive())
            {
                continue;
            }

            noradNumbersToProcess.UnionWith(category.GetSatellites());
        }


        // Add all active loaded satellites to the processing list
        foreach (uint satNum in noradNumbersToProcess)
        {
            satellitesToProcess.Add(loadedSatellites[satNum]);
        }

        // Move satellite positions
        foreach (Satellite satellite in satellitesToProcess)
        {
            satellite.getSatelliteObject().transform.localPosition = satellite.currentVector;
        }

        // If paused and no categories have been toggled, dont run any processing
        if (lastCheckedTime.Equals(simulatedTime) && previousSatellitesToProcess == satellitesToProcess.Count)
        {
            return;
        }
        lastCheckedTime = simulatedTime;
        previousSatellitesToProcess = satellitesToProcess.Count;


        if (selectedSatellite != null)
        {
            selectedSatellite.UpDateSatelliteInfoText();
            selectedSatellite.RenderOrbitPath();
        }


        // Reduce frequency of satellite processing dependent on number of satellites and speed
        int cooldownAmount = Mathf.Max(Mathf.Min(satellitesToProcess.Count / 50, 200), 5);
        
        if (Mathf.Abs(speed) == 200)
        {
            cooldownAmount = Mathf.Min(cooldownAmount / 4, 5);
        }
        
        if (processCooldown >= cooldownAmount)
        {
            processCooldown = 0;
        } else
        {
            processCooldown++;
            return;
        }

        ProcessSatellites(satellitesToProcess);

        // Any errors caught while processing will add a satellite to the destroy list
        foreach (Satellite satellite in destroyList)
        {
            if (selectedSatellite.getSatelliteData().Tle.NoradNumber == satellite.getSatelliteData().Tle.NoradNumber)
            {
                selectedSatellite.Deselect();
            }

            Destroy(satellite.getSatelliteObject());
            foreach (Category category in categories)
            {
                category.satellites.Remove(satellite.getSatelliteData().Tle.NoradNumber);
            }

        }
    }

    // Add new categories here with 2 lines of code
    private Task<List<Category>> LoadSatellites()
    {

        List<Category> loadingCategories = new List<Category>();
        
        Category weather = new Category("weather.txt", "Weather");
        Category geosynchronous = new Category("geo.txt", "Geosynchronous");
        Category starlink = new Category("starlink.txt", "Starlink");
        Category oneWeb = new Category("oneweb.txt", "OneWeb");
        Category spaceStations = new Category("stations.txt", "Space Stations");
        Category active = new Category("active.txt", "Active");
        
        loadingCategories.Add(weather);
        loadingCategories.Add(geosynchronous);
        loadingCategories.Add(starlink);
        loadingCategories.Add(oneWeb);
        loadingCategories.Add(spaceStations);
        loadingCategories.Add(active);

        foreach (Category category in loadingCategories)
        {
            GameObject uiElement = TextMesh.Instantiate(categoryTogglePrefab, new Vector3(), Quaternion.identity);
            uiElement.GetComponentInChildren<Text>().text = category.GetLabel();
            uiElement.transform.SetParent(categoryToggleBody.transform, false);
            uiElement.name = category.GetLabel();
        }

        return System.Threading.Tasks.Task.FromResult(loadingCategories);
    }

    private void ProcessSatellites(List<Satellite> satellites)
    {
        for (int i = 0; i < satellites.Count - 1; i++)
        {
            try
            {
                satellites[i].currentVector = satellites[i].GetVectorOfSatellite(simulatedTime);
            } catch (SatellitePropagationException e)
            {
                destroyList.Add(satellites[i]);
                Debug.Log("Couldn't process satellite due to propagation exception");
                Debug.Log(e.ToString());
            }
            catch (DecayedException e)
            {
                Debug.Log("Couldn't process satellite due to decay exception");
                Debug.Log(e.ToString());
            }
        }
    }

    private void Update()
    {
        // Clearing selected satellite info
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Screen.fullScreen)
            {
                minScreenIcon.SetActive(false);
                maxScreenIcon.SetActive(true);
            } else if (selectedSatellite != null)
            {
                selectedSatellite.Deselect();
            }
                
        }
    }

    public GameObject GetSatellitePrefab()
    {
        return satellitePrefab;
    }

    public GameObject GetSatellitesObject()
    {
        return satellitesObject;
    }

    public Satellite GetSatellite(uint noradNumber)
    {
        Satellite sat = loadedSatellites[noradNumber];
        if (sat == null)
        {
            Debug.Log("Key not found: " + noradNumber);
            return null;
        }

        return sat;
        
    }

    public Category GetCategory(String label)
    {
        foreach (Category category in categories)
        {
            if (category.GetLabel().Equals(label))
            {
                return category;
            }
        }

        return null;
    }

    public void SetCategory(String label, bool isActive)
    {
        for (int i = 0; i < categories.Count; i++)
        {
            if (categories[i].GetLabel().Equals(label))
            {
                categories[i].SetActive(isActive);
            }
        }

        // This enables the satellites to process
        lastCheckedTime = lastCheckedTime.AddMilliseconds(1);
    }

    public void DestroySatelliteObject(GameObject satellite)
    {
        Destroy(satellite);
    }


    public void Play()
    {
        speed = 1;
        if (selectedSatellite != null)
        {
            selectedSatellite.firstRender = true;
            selectedSatellite.RenderOrbitPath();
        }
    }

    public void Pause()
    {
        speed = 0;
    }

    public void FastForward()
    {
        speed = 200;
        if (selectedSatellite != null)
        {
            selectedSatellite.firstRender = true;
            selectedSatellite.RenderOrbitPath();
        }

    }

    public void Rewind()
    {
        speed = -200;
        if (selectedSatellite != null)
        {
            selectedSatellite.firstRender = true;
            selectedSatellite.reverseLineTime = simulatedTime;
            selectedSatellite.RenderOrbitPath();
        }
    }

    public void ResetTime()
    {
        speed = 1;
        simulatedTime = DateTime.Now;
        if (selectedSatellite != null)
        {
            selectedSatellite.firstRender = true;
            selectedSatellite.RenderOrbitPath();
        }
    }

    public void SetSatelliteInfoText(String text)
    {
        satelliteInfoText.GetComponent<Text>().text = text;
    }

    public void ToggleAllCategories(bool active)
    {
        for (int i = 0; i < categories.Count; i++)
        {
            categories[i].SetActive(active);
        }

        // This enables the satellites to process
        lastCheckedTime = lastCheckedTime.AddMilliseconds(1);
    }

    public void ChangeOrbitPathLineLength(System.Single value)
    {
        orbitLineLength = (int)value;
        if (selectedSatellite != null)
        {
            selectedSatellite.firstRender = true;
            selectedSatellite.RenderOrbitPath();
        }

    }

    public void ToggleSatelliteInfoSlider(bool active)
    {
        satellitePathSlider.SetActive(active);
    }

    public void SetFullScreen(bool fullScreen)
    {
        
        Screen.fullScreen = fullScreen;
        if (fullScreen)
        {
            //Screen.SetResolution(Screen.width, Screen.height, true);
            minScreenIcon.SetActive(true);
            maxScreenIcon.SetActive(false);
        } else
        {
            minScreenIcon.SetActive(false);
            minScreenIcon.SetActive(true);
        }

    }

}
