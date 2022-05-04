using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class SolarSystem : MonoBehaviour
{
    private List<Transform> objs;
    private List<int> rotations;
    private List<float> distances;
    
    // Start is called before the first frame update
    void Start()
    {
        objs = new List<Transform>();
        rotations = new List<int>();
        distances = new List<float>();
        foreach (Transform child in transform)
        {
            if (child.GetComponent<Planet>() != null)
            {
                objs.Add(child);
                rotations.Add(Random.Range(3, 13));
                distances.Add( (30000f / Vector3.Distance(child.position, transform.position)));
            }
        }
    }

    private void OnPreload()
    {
        if (_asyncOperation == null)
        {
            // Start scene preloading.
            this._asyncOperation = SceneManager.LoadSceneAsync(_sceneName);
            //this.StartCoroutine(this.LoadSceneAsyncProcess(sceneName: this._sceneName));
            this._asyncOperation.allowSceneActivation = false;
        }
        // Don't let the Scene activate until you allow it to.
        //this._asyncOperation.allowSceneActivation = false;
    }

    private void OnActivate()
    {
        // Press the + key to activate the Scene.
        if (_asyncOperation != null)
        {
            this._asyncOperation.allowSceneActivation = true;
        }
    }

    private void OnPrint()
    {
        foreach (var obj in objs)
        {
            obj.GetComponent<Planet>().PrintOutMap();
        }
    }

    // Update is called once per frame
    void Update()
    {
        int i = 0;
        foreach (var obj in objs)
        {
            if (Random.Range(0f, 1f) > 0.5f)
            {
                obj.RotateAround(transform.position, transform.up, distances[i] * Time.deltaTime);
            }
            else
            {
                obj.Rotate(transform.up, rotations[i] * Time.deltaTime);
            }
            i++;
        }
    }
    
    [SerializeField] private string _sceneName = "DemoForClass1";

    private AsyncOperation _asyncOperation;
}
