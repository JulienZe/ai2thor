using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
public static class UtilityFunctions {

    public static Bounds CreateEmptyBounds() {
        Bounds b = new Bounds();
        Vector3 inf = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        b.SetMinMax(min: inf, max: -inf);
        return b;
    }

    private static IEnumerable<int[]> Combinations(int m, int n) {
        // Enumerate all possible m-size combinations of [0, 1, ..., n-1] array
        // in lexicographic order (first [0, 1, 2, ..., m-1]).
        // Taken from https://codereview.stackexchange.com/questions/194967/get-all-combinations-of-selecting-k-elements-from-an-n-sized-array
        int[] result = new int[m];
        Stack<int> stack = new Stack<int>(m);
        stack.Push(0);
        while (stack.Count > 0) {
            int index = stack.Count - 1;
            int value = stack.Pop();
            while (value < n) {
                result[index++] = value++;
                stack.Push(value);
                if (index != m) {
                    continue;
                }

                yield return (int[])result.Clone(); // thanks to @xanatos
                // yield return result;
                break;
            }
        }
    }

    public static IEnumerable<T[]> Combinations<T>(T[] array, int m) {
        // Taken from https://codereview.stackexchange.com/questions/194967/get-all-combinations-of-selecting-k-elements-from-an-n-sized-array
        if (array.Length < m) {
            throw new ArgumentException("Array length can't be less than number of selected elements");
        }
        if (m < 1) {
            throw new ArgumentException("Number of selected elements can't be less than 1");
        }
        T[] result = new T[m];
        foreach (int[] j in Combinations(m, array.Length)) {
            for (int i = 0; i < m; i++) {
                result[i] = array[j[i]];
            }
            yield return result;
        }
    }

    public static bool isObjectColliding(
        GameObject go,
        List<GameObject> ignoreGameObjects = null,
        float expandBy = 0.0f,
        bool useBoundingBoxInChecks = false
     ) {
        return null != firstColliderObjectCollidingWith(
            go: go,
            ignoreGameObjects: ignoreGameObjects,
            expandBy: expandBy,
            useBoundingBoxInChecks: useBoundingBoxInChecks
        );
    }

    public static Collider firstColliderObjectCollidingWith(
        GameObject go,
        List<GameObject> ignoreGameObjects = null,
        float expandBy = 0.0f,
        bool useBoundingBoxInChecks = false
     ) {
        if (ignoreGameObjects == null) {
            ignoreGameObjects = new List<GameObject>();
        }
        ignoreGameObjects.Add(go);
        HashSet<Collider> ignoreColliders = new HashSet<Collider>();
        foreach (GameObject toIgnoreGo in ignoreGameObjects) {
            foreach (Collider c in toIgnoreGo.GetComponentsInChildren<Collider>()) {
                ignoreColliders.Add(c);
            }
        }

        int layerMask = LayerMask.GetMask("Agent", "SimObjVisible", "Procedural1", "Procedural2", "Procedural3", "Procedural0");
        foreach (CapsuleCollider cc in go.GetComponentsInChildren<CapsuleCollider>()) {
            if (cc.isTrigger) {
                continue;
            }
            foreach (Collider c in PhysicsExtensions.OverlapCapsule(cc, layerMask, QueryTriggerInteraction.Ignore, expandBy)) {
                if (!ignoreColliders.Contains(c)) {
                    return c;
                }
            }
        }
        foreach (BoxCollider bc in go.GetComponentsInChildren<BoxCollider>()) {
            if (bc.isTrigger || ("BoundingBox" == bc.gameObject.name && (!useBoundingBoxInChecks))) {
                continue;
            }
            foreach (Collider c in PhysicsExtensions.OverlapBox(bc, layerMask, QueryTriggerInteraction.Ignore, expandBy)) {
                if (!ignoreColliders.Contains(c)) {
                    return c;
                }
            }
        }
        foreach (SphereCollider sc in go.GetComponentsInChildren<SphereCollider>()) {
            if (sc.isTrigger) {
                continue;
            }
            foreach (Collider c in PhysicsExtensions.OverlapSphere(sc, layerMask, QueryTriggerInteraction.Ignore, expandBy)) {
                if (!ignoreColliders.Contains(c)) {
                    return c;
                }
            }
        }
        return null;
    }

    public static Collider[] collidersObjectCollidingWith(
        GameObject go,
        List<GameObject> ignoreGameObjects = null,
        float expandBy = 0.0f,
        bool useBoundingBoxInChecks = false
        ) {
        if (ignoreGameObjects == null) {
            ignoreGameObjects = new List<GameObject>();
        }
        ignoreGameObjects.Add(go);
        HashSet<Collider> ignoreColliders = new HashSet<Collider>();
        foreach (GameObject toIgnoreGo in ignoreGameObjects) {
            foreach (Collider c in toIgnoreGo.GetComponentsInChildren<Collider>()) {
                ignoreColliders.Add(c);
            }
        }

        HashSet<Collider> collidersSet = new HashSet<Collider>();
        int layerMask = LayerMask.GetMask("SimObjVisible", "Agent");
        foreach (CapsuleCollider cc in go.GetComponentsInChildren<CapsuleCollider>()) {
            foreach (Collider c in PhysicsExtensions.OverlapCapsule(cc, layerMask, QueryTriggerInteraction.Ignore, expandBy)) {
                if (!ignoreColliders.Contains(c)) {
                    collidersSet.Add(c);
                }
            }
        }
        foreach (BoxCollider bc in go.GetComponentsInChildren<BoxCollider>()) {
            if ("BoundingBox" == bc.gameObject.name && (!useBoundingBoxInChecks)) {
                continue;
            }
            foreach (Collider c in PhysicsExtensions.OverlapBox(bc, layerMask, QueryTriggerInteraction.Ignore, expandBy)) {
                if (!ignoreColliders.Contains(c)) {
                    collidersSet.Add(c);
                }
            }
        }
        foreach (SphereCollider sc in go.GetComponentsInChildren<SphereCollider>()) {
            foreach (Collider c in PhysicsExtensions.OverlapSphere(sc, layerMask, QueryTriggerInteraction.Ignore, expandBy)) {
                if (!ignoreColliders.Contains(c)) {
                    collidersSet.Add(c);
                }
            }
        }
        return collidersSet.ToArray();
    }

    public static RaycastHit[] CastAllPrimitiveColliders(GameObject go, Vector3 direction, float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) {
        HashSet<Transform> transformsToIgnore = new HashSet<Transform>();
        foreach (Transform t in go.GetComponentsInChildren<Transform>()) {
            transformsToIgnore.Add(t);
        }
        List<RaycastHit> hits = new List<RaycastHit>();
        foreach (CapsuleCollider cc in go.GetComponentsInChildren<CapsuleCollider>()) {
            foreach (RaycastHit h in PhysicsExtensions.CapsuleCastAll(cc, direction, maxDistance, layerMask, queryTriggerInteraction)) {
                if (!transformsToIgnore.Contains(h.transform)) {
                    hits.Add(h);
                }
            }
        }
        foreach (BoxCollider bc in go.GetComponentsInChildren<BoxCollider>()) {
            foreach (RaycastHit h in PhysicsExtensions.BoxCastAll(bc, direction, maxDistance, layerMask, queryTriggerInteraction)) {
                if (!transformsToIgnore.Contains(h.transform)) {
                    hits.Add(h);
                }
            }
        }
        foreach (SphereCollider sc in go.GetComponentsInChildren<SphereCollider>()) {
            foreach (RaycastHit h in PhysicsExtensions.SphereCastAll(sc, direction, maxDistance, layerMask, queryTriggerInteraction)) {
                if (!transformsToIgnore.Contains(h.transform)) {
                    hits.Add(h);
                }
            }
        }
        return hits.ToArray();
    }

    // get a copy of a specific component and apply it to another object at runtime
    // usage: var copy = myComp.GetCopyOf(someOtherComponent);
    public static T GetCopyOf<T>(this Component comp, T other) where T : Component {
        Type type = comp.GetType();
        if (type != other.GetType()) {
            return null; // type mis-match
        }

        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
        PropertyInfo[] pinfos = type.GetProperties(flags);
        foreach (var pinfo in pinfos) {
            if (pinfo.CanWrite) {
                try {
                    pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                } catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
            }
        }
        FieldInfo[] finfos = type.GetFields(flags);
        foreach (var finfo in finfos) {
            finfo.SetValue(comp, finfo.GetValue(other));
        }
        return comp as T;
    }

    // usage: Health myHealth = gameObject.AddComponent<Health>(enemy.health); or something like that
    public static T AddComponent<T>(this GameObject go, T toAdd) where T : Component {
        return go.AddComponent<T>().GetCopyOf(toAdd) as T;
    }


    // Taken from https://answers.unity.com/questions/589983/using-mathfround-for-a-vector3.html
    public static Vector3 Round(this Vector3 vector3, int decimalPlaces = 2) {
        float multiplier = 1;
        for (int i = 0; i < decimalPlaces; i++) {
            multiplier *= 10f;
        }
        return new Vector3(
            Mathf.Round(vector3.x * multiplier) / multiplier,
            Mathf.Round(vector3.y * multiplier) / multiplier,
            Mathf.Round(vector3.z * multiplier) / multiplier);
    }

    public static Vector3[] CornerCoordinatesOfBoxColliderToWorld(BoxCollider b) {
        Vector3[] corners = new Vector3[8];

        corners[0] = b.transform.TransformPoint(b.center + new Vector3(b.size.x, -b.size.y, b.size.z) * 0.5f);
        corners[1] = b.transform.TransformPoint(b.center + new Vector3(-b.size.x, -b.size.y, b.size.z) * 0.5f);
        corners[2] = b.transform.TransformPoint(b.center + new Vector3(-b.size.x, -b.size.y, -b.size.z) * 0.5f);
        corners[3] = b.transform.TransformPoint(b.center + new Vector3(b.size.x, -b.size.y, -b.size.z) * 0.5f);

        corners[4] = b.transform.TransformPoint(b.center + new Vector3(b.size.x, b.size.y, b.size.z) * 0.5f);
        corners[5] = b.transform.TransformPoint(b.center + new Vector3(-b.size.x, b.size.y, b.size.z) * 0.5f);
        corners[6] = b.transform.TransformPoint(b.center + new Vector3(-b.size.x, b.size.y, -b.size.z) * 0.5f);
        corners[7] = b.transform.TransformPoint(b.center + new Vector3(b.size.x, b.size.y, -b.size.z) * 0.5f);

        return corners;
    }

#if UNITY_EDITOR
    [MenuItem("SimObjectPhysics/Toggle Off PlaceableSurface Material")]
    private static void ToggleOffPlaceableSurface() {
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++) {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(SceneUtility.GetScenePathByBuildIndex(i), OpenSceneMode.Single);
            var meshes = UnityEngine.Object.FindObjectsOfType<MeshRenderer>();

            foreach (MeshRenderer m in meshes) {
                if (m.sharedMaterial.ToString() == "Placeable_Surface_Mat (UnityEngine.Material)") {
                    m.enabled = false;
                }
            }
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
    }

    [MenuItem("SimObjectPhysics/Toggle On PlaceableSurface Material")]
    private static void ToggleOnPlaceableSurface() {
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++) {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(SceneUtility.GetScenePathByBuildIndex(i), OpenSceneMode.Single);
            var meshes = UnityEngine.Object.FindObjectsOfType<MeshRenderer>();

            foreach (MeshRenderer m in meshes) {
                if (m.sharedMaterial.ToString() == "Placeable_Surface_Mat (UnityEngine.Material)") {
                    m.enabled = true;
                }
            }
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        }
    }


    [MenuItem("AI2-THOR/Name All Scene Light Objects")]
    //light naming convention: {PrefabName/Scene}|{Type}|{instance}
    //Editor-only function used to set names of all light assets in scenes that have Lights in them prior to any additional lights being
    //dynamically spawned in by something like a Procedural action.
    private static void NameAllSceneLightObjects() {
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++) {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(SceneUtility.GetScenePathByBuildIndex(i), OpenSceneMode.Single);
            var lights = UnityEngine.Object.FindObjectsOfType<Light>(true);

            //List<Light> lights = new List<Light>();

            // //do this to only include light objects that are in the active scene, otherwise Prefabs with lights in Assets are also included cause RESOURCES>FINDOBJECTSOFTYPEALL AHHHHHH
            // foreach (Light li in everyLight) {
            //     if(li.hideFlags == HideFlags.NotEditable || li.hideFlags == HideFlags.HideAndDontSave)
            //     continue;

            //     if(!EditorUtility.IsPersistent(li.transform.root.gameObject))
            //     continue;

            //     lights.Add(li);
            // }

            //separate lights into scene-level lights, and lights that are children of sim objects
            Dictionary<Light, LightType> sceneLights = new Dictionary<Light, LightType>();
            Dictionary<Light, LightType> simObjChildLights = new Dictionary<Light, LightType>();

            foreach (Light l in lights) {
                 if(!l.GetComponentInParent<SimObjPhysics>()) {
                    Debug.Log($"adding {l.transform.name} to sceneLights");
                    sceneLights.Add(l, l.type);
                 }

                 else {
                    Debug.Log($"adding {l.transform.name} to simObjChildLights");
                    simObjChildLights.Add(l, l.type);
                 }
            }

            int directionalInstance = 0;
            int spotInstance = 0;
            int pointInstance = 0;
            int areaInstance = 0;

            Debug.Log($"scene light count is: {sceneLights.Count}");
            Debug.Log($"child light count is: {simObjChildLights.Count}");
            //sort the scene lights into point, directional, or spot
            foreach (KeyValuePair<Light, LightType> l in sceneLights) {
                //Debug.Log(directionalInstance);
                if(l.Value == LightType.Spot) {
                    l.Key.name = "scene|" + l.Value.ToString()+ "|" + spotInstance.ToString();
                    spotInstance++;
                }

                else if(l.Value == LightType.Directional) {
                    l.Key.name = "scene|" + l.Value.ToString()+ "|" + directionalInstance.ToString();
                    directionalInstance++;
                }

                else if(l.Value == LightType.Point) {
                    l.Key.name = "scene|" + l.Value.ToString()+ "|" + pointInstance.ToString();
                    pointInstance++;
                }
            
                else if(l.Value == LightType.Area) {
                    l.Key.name = "scene|" + l.Value.ToString()+ "|" + areaInstance.ToString();
                    areaInstance++;
                }
            }

            //make new dictionary to pair specific Sim Object instances with potentially multiple child lights, so multiple child light keys might have same SimObj parent value
            Dictionary<KeyValuePair<Light, LightType>, SimObjPhysics> lightAndTypeToSimObjPhys = new Dictionary<KeyValuePair<Light, LightType>, SimObjPhysics>();
            
            //map each light/lightType pair to the sim object that they are associated with
            foreach (KeyValuePair<Light, LightType> l in simObjChildLights) {
                //KeyValuePair<SimObjPhysics, int> simObjToInstanceCount = new KeyValuePair<SimObjPhysics, int>(l.Key.GetComponentInParent<SimObjPhysics>(), 0);
                lightAndTypeToSimObjPhys.Add(l, l.Key.GetComponentInParent<SimObjPhysics>());
            }

            //track if multiple key lights are children of the same SimObjPhysics
            Dictionary<SimObjPhysics, int> simObjToSpotInstanceCountInThatSimObj = new Dictionary<SimObjPhysics, int>();
            Dictionary<SimObjPhysics, int> simObjToDirectionalInstanceCountInThatSimObj = new Dictionary<SimObjPhysics, int>();
            Dictionary<SimObjPhysics, int> simObjToPointInstanceCountInThatSimObj = new Dictionary<SimObjPhysics, int>();
            Dictionary<SimObjPhysics, int> simObjToAreaInstanceCountInThatSimObj = new Dictionary<SimObjPhysics, int>();

            foreach(KeyValuePair< KeyValuePair<Light, LightType>, SimObjPhysics> light in lightAndTypeToSimObjPhys) {
                
                if(light.Key.Value == LightType.Spot) {
                    if(!simObjToSpotInstanceCountInThatSimObj.ContainsKey(light.Value)){
                        //this is the first instance of a spotlight found in the sim object
                        simObjToSpotInstanceCountInThatSimObj.Add(light.Value, 0);
                    }

                    else {
                        //we have found another instance of this type of light in this sim object before
                        simObjToSpotInstanceCountInThatSimObj[light.Value]++;
                    }

                    light.Key.Key.name = light.Value.transform.name + "|" + light.Key.Value.ToString() + "|" + simObjToSpotInstanceCountInThatSimObj[light.Value].ToString();
                }

                else if(light.Key.Value == LightType.Directional) {
                    if(!simObjToDirectionalInstanceCountInThatSimObj.ContainsKey(light.Value)){
                        simObjToDirectionalInstanceCountInThatSimObj.Add(light.Value, 0);
                    }

                    else {
                        simObjToDirectionalInstanceCountInThatSimObj[light.Value]++;
                    }

                    light.Key.Key.name = light.Value.transform.name + "|" + light.Key.Value.ToString() + "|" + simObjToDirectionalInstanceCountInThatSimObj[light.Value].ToString();
                }

                else if(light.Key.Value == LightType.Point) {    
                    if(!simObjToPointInstanceCountInThatSimObj.ContainsKey(light.Value)){
                        simObjToPointInstanceCountInThatSimObj.Add(light.Value, 0);
                    }

                    else {
                        simObjToPointInstanceCountInThatSimObj[light.Value]++;
                    }

                    light.Key.Key.name = light.Value.transform.name + "|" + light.Key.Value.ToString() + "|" + simObjToPointInstanceCountInThatSimObj[light.Value].ToString();
                }

                else if(light.Key.Value == LightType.Area) {

                    if(!simObjToAreaInstanceCountInThatSimObj.ContainsKey(light.Value)){
                        simObjToAreaInstanceCountInThatSimObj.Add(light.Value, 0);
                    }

                    else {
                        simObjToAreaInstanceCountInThatSimObj[light.Value]++;
                    }

                    light.Key.Key.name = light.Value.transform.name + "|" + light.Key.Value.ToString() + "|" + simObjToAreaInstanceCountInThatSimObj[light.Value].ToString();
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
    }
#endif
}