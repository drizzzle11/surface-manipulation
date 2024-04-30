using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class MainSurface : MonoBehaviour
{
    //user placed objects
    //prototype object to be instantiated when user adds a new object
    public GameObject objectPrototype;
    //currenlty selected object when in select mode
    public GameObject selectedObject;
    //list of all objects created by the user
    public List<GameObject> createdObjects = new List<GameObject>();

    //actions/tools
    //action type enumeration (these are simply labels for the different actions)
    public enum ActionType{None=0, Select=1, Add=2, Delete=3}
    //current action type (this determines how the system interprets user input)
    public ActionType actionType = ActionType.None;

    //callback for when an action button is pressed
    public void OnActionTypeChanged(int value) {
        actionType = (ActionType)value;
        Debug.Log("Action type changed to " + actionType);
    }

    //surface parameters
    public int resolutionX = 128;
    public int resolutionZ = 128;

    //variable and corresponding callaback function for the surface type dropdown
    public int surfType = 0;
    public void OnSurfTypeChanged(int value)
    {
        surfType = value;
        UpdateSurface();
    }

    //variable and corresponding callaback function for the surface visibility toggle
    public bool isSurfaceVisible = true;
    public void OnSurfaceVisibleChanged(bool value)
    {
        isSurfaceVisible = value;
        GetComponent<MeshRenderer>().enabled = value;
    }

    //callaback function for when the clear button is pressed
    public void OnClearButtonPressed()
    {
        Debug.Log("Button pressed");
        foreach(var obj in createdObjects) {
            Destroy(obj);
        }
        createdObjects.Clear();
        UpdateSurface();
    }

    //reference to the text label element in the ui that displays the current value of the slider
    public TMP_Text surfScaleYText;
    //variable and corresponding callaback function for the surface scale slider
    public float surfScaleY = 1.0f;
    public void OnScaleYChanged(float value)
    {
        surfScaleY = value;
        surfScaleYText.text = "Scale Y: " + value.ToString("F2");
        UpdateSurface();
    }

    //variable and corresponding callaback function for the surface parameter 1 slider
    public float surfParam1 = 1.0f;
    public void OnParam1Changed(float value)
    {
        surfParam1 = value;
        UpdateSurface();
    }


    //function called whenever the surface needs to be updated
    void UpdateSurface() {
        //get the mesh component of the game object
        var mesh = GetComponent<MeshFilter>().mesh;
        //get the array of vertices from the mesh
        Vector3[] vertices = mesh.vertices;

        //update the vertices


        if (surfType==0) { //sinusoidal waves
            float x0 = -5.0f;
            float x1 = 5.0f;

            float z0 = -5.0f;
            float z1 = 5.0f;

            float dx = (x1 - x0) / (resolutionX - 1);
            float dz = (z1 - z0) / (resolutionZ - 1);

            for(int j=0; j<resolutionZ; j++) {
                for(int i=0; i<resolutionX; i++) {
                    int index = j * resolutionX + i;
                    float x = x0 + i * dx;
                    float z = z0 + j * dz;

                    vertices[index].x = x;
                    vertices[index].y = surfScaleY * Mathf.Sin(x * surfParam1) * Mathf.Sin(z);
                    vertices[index].z = z;
                }
            }
        }
        else if (surfType==1) { //cylinder
            float da = 2.0f * Mathf.PI / (resolutionX - 1);

            float y0 = -2.0f;
            float y1 = 2.0f;
            float dy = (y1 - y0) / (resolutionZ - 1);

            for(int j=0; j<resolutionZ; j++) {
                for(int i=0; i<resolutionX; i++) {
                    int index = j * resolutionX + i;
                    float angle = i * da;
                    float y = y0 + j * dy;

                    float radius = surfScaleY * (1.0f + Mathf.Cos(y* surfParam1));

                    vertices[index].x = radius * Mathf.Cos(angle);
                    vertices[index].y = y;
                    vertices[index].z = radius * Mathf.Sin(angle);
                }
            }
        }
        else if (surfType==2) { //saddle surface (hyperbolic paraboloid) with noise
            float x0 = -5.0f;
            float x1 = 5.0f;

            float z0 = -5.0f;
            float z1 = 5.0f;

            float dx = (x1 - x0) / (resolutionX - 1);
            float dz = (z1 - z0) / (resolutionZ - 1);

            for(int j=0; j<resolutionZ; j++) {
                for(int i=0; i<resolutionX; i++) {
                    int index = j * resolutionX + i;
                    float x = x0 + i * dx;
                    float z = z0 + j * dz;

                    vertices[index].x = x;
                    vertices[index].y = x*z*surfScaleY + Random.value*surfParam1;
                    vertices[index].z = z;
                }
            }
        }
        else if (surfType==3) { //surface from distance to points
            float x0 = -5.0f;
            float x1 = 5.0f;

            float z0 = -5.0f;
            float z1 = 5.0f;

            float dx = (x1 - x0) / (resolutionX - 1);
            float dz = (z1 - z0) / (resolutionZ - 1);

            for(int j=0; j<resolutionZ; j++) {
                for(int i=0; i<resolutionX; i++) {
                    int index = j * resolutionX + i;
                    float x = x0 + i * dx;
                    float z = z0 + j * dz;

                    //get 2d distance from each created object and pick the minimum (voronoi diagram)
                    float minDist = 1000.0f;
                    foreach(var obj in createdObjects) {
                        var pos = obj.transform.position;
                        float dist = Mathf.Sqrt((pos.x-x)*(pos.x-x) + (pos.z-z)*(pos.z-z));
                        if (dist<minDist) minDist = dist;
                    }

                    vertices[index].x = x;
                    vertices[index].y = minDist*surfScaleY;
                    vertices[index].z = z;
                }
            }
        }

        //assign the updated vertex array back to the mesh
        mesh.vertices = vertices;
        //recalculate normals to update the lighting
        mesh.RecalculateNormals();

        //update the mesh collider as well so that the mouse ray collides with the new shape of the surface geometry
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    //create a grid mesh with given resolution and size
    Mesh CreateGrid(int rx, int rz, float minx, float maxx, float minz, float maxz)
    {
        //create new mesh
        Mesh m = new Mesh();

        //create arrays for vertices, triangles and uv coordinates
        Vector3[] vertices = new Vector3[rx*rz];
        int[] triangles = new int[(rx-1)*(rz-1)*2*3];
        Vector2[] uv = new Vector2[rx*rz];

        //step size for uv coordinates (normalized) to cover {0,1} range
        float dnx = 1.0f / (rx - 1);
        float dnz = 1.0f / (rz - 1);

        //step size for vertices to cover {minx,maxx} range
        float dx = (maxx - minx) / (rx - 1);
        float dz = (maxz - minz) / (rz - 1);

        //index of vertex in vertices array
        int i = 0;

        //fill vertices and uv arrays
        for (int Zstep = 0; Zstep < rz; Zstep++)
        {
            float z = minz + Zstep * dz;
            float nz = Zstep * dnz;
            for (int Xstep = 0; Xstep < rx; Xstep++)
            {
                float x = minx + Xstep * dx;
                float nx = Xstep * dnx;

                float y = 0.0f;
                vertices[i] = new Vector3(x, y, z);
                uv[i] = new Vector2(nx, nz);
                i++;
            }
        }

        //index of triangle vertex index in triangles array
        int ti = 0;

        //fill triangles array
        for (int z = 0; z < rz - 1; z++)
        {
            for (int x = 0; x < rx - 1; x++)
            {
                //index of bottom left corner of grid quad in vertex array
                int cornerVertexIndex = z * rx + x;

                //first triangle
                triangles[ti++] = cornerVertexIndex;
                triangles[ti++] = cornerVertexIndex + rx + 1;
                triangles[ti++] = cornerVertexIndex + 1;

                //second triangle
                triangles[ti++] = cornerVertexIndex;
                triangles[ti++] = cornerVertexIndex + rx;
                triangles[ti++] = cornerVertexIndex + rx + 1;
            }
        }        

        //assign arrays to mesh
        m.vertices = vertices;
        m.uv = uv;
        m.triangles = triangles;

        //calculate normals
        m.RecalculateNormals();

        return m;
    }

    void Start()
    {
        //create a grid mesh and assign it to the mesh filter
        GetComponent<MeshFilter>().mesh = CreateGrid(resolutionX, resolutionZ, -5, 5, -5, 5);
    }

    //Set and highlight the currently selected object
    void SetSelectedObject(GameObject obj)
    {
        if (selectedObject != null) {
            selectedObject.GetComponent<Renderer>().material.color = Color.white;
        }
        selectedObject = obj;
        if (selectedObject != null) {
            selectedObject.GetComponent<Renderer>().material.color = Color.red;
        }
    }

    void Update()
    {
        //handle mouse input
        var mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;


        if (EventSystem.current.IsPointerOverGameObject()) {
            //mouse is over UI element, do not process mouse input
            return;
        }
        else if (actionType== ActionType.Select) {
            //select object under mouse
            if (Input.GetMouseButtonDown(0)) {
                if (Physics.Raycast(mouseRay, out hit)) {
                    SetSelectedObject(hit.collider.gameObject);
                }
                else {
                    SetSelectedObject(null);
                }
            }    
        }
        else if (actionType== ActionType.Add) {
            //add new object under mouse
            if (Input.GetMouseButtonDown(0) && Physics.Raycast(mouseRay, out hit)) {
                GameObject newObject = Instantiate(objectPrototype);
                newObject.SetActive(true);
                newObject.transform.position = hit.point;
                //add new object to the list of created objects to keep track of all objects created
                createdObjects.Add(newObject);
                UpdateSurface();
            }
        }
        else if (actionType== ActionType.Delete) {
            //delete object under mouse
            if (Input.GetMouseButtonDown(0) && Physics.Raycast(mouseRay, out hit)) {
                var objecToDelete = hit.collider.gameObject;
                if (createdObjects.Contains(objecToDelete)) { 
                    createdObjects.Remove(objecToDelete);
                    SetSelectedObject(null);
                    Destroy(objecToDelete);
                    UpdateSurface();
                }

            }
        }     
    }
}
