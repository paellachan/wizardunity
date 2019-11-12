using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Advertisements;

public class UnityAdsScript : MonoBehaviour { 

	string gameId = "3243908";
	bool testMode = true;

	void Start () {
		Advertisement.Initialize (gameId, testMode);
	}
}


public class adsInit : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
