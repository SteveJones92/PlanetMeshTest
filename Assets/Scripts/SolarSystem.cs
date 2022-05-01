using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SolarSystem : MonoBehaviour
{
    private List<Transform> objs;
    private List<int> rotations;
    
    // Start is called before the first frame update
    void Start()
    {
        objs = new List<Transform>();
        rotations = new List<int>();
        foreach (Transform child in transform)
        {
            if (child.GetComponent<Planet>() != null)
            {
                objs.Add(child);
                rotations.Add(Random.Range(3, 13));
            }
        }
    }

    private void OnPreload(InputValue inp)
    {
        //Debug.Log("preload");
        if (_asyncOperation == null)
        {
            //Debug.Log("Started Scene Preloading");

            // Start scene preloading.
            this.StartCoroutine(this.LoadSceneAsyncProcess(sceneName: this._sceneName));
        }
    }

    private void OnActivate(InputValue inp)
    {
        //Debug.Log("activate");
        // Press the space key to activate the Scene.
        if (_asyncOperation != null)
        {
            //Debug.Log("Allowed Scene Activation");

            this._asyncOperation.allowSceneActivation = true;
            //SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());
        }
    }

    // Update is called once per frame
    void Update()
    {
        int i = 0;
        foreach (var obj in objs)
        {
            obj.RotateAround(transform.position, transform.up, (40000f / Vector3.Distance(obj.position, transform.position)) * Time.deltaTime);
            obj.Rotate(transform.up, rotations[i] * Time.deltaTime);
            i++;
        }
    }
    
    [SerializeField] private string _sceneName = "DemoForClass";
    public string _SceneName => this._sceneName;

    private AsyncOperation _asyncOperation;

    private IEnumerator LoadSceneAsyncProcess(string sceneName)
    {
        // Begin to load the Scene you have specified.
        this._asyncOperation = SceneManager.LoadSceneAsync(sceneName);

        // Don't let the Scene activate until you allow it to.
        this._asyncOperation.allowSceneActivation = false;

        while (!this._asyncOperation.isDone)
        {
            Debug.Log($"[scene]:{sceneName} [load progress]: {this._asyncOperation.progress}");

            yield return null;
        }
    }
}
