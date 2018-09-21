﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SimObjPhysics : MonoBehaviour, SimpleSimObj
{
	[Header("String ID of this Object")]
	[SerializeField]
	public string uniqueID = string.Empty;

	[Header("Object Type")]
	[SerializeField]
	public SimObjType Type = SimObjType.Undefined;

	[Header("Primary Property (Must Have only 1)")]
	[SerializeField]
	public SimObjPrimaryProperty PrimaryProperty;

	[Header("Additional Properties (Can Have Multiple)")]
	[SerializeField]
	public SimObjSecondaryProperty[] SecondaryProperties;

	[Header("non Axis-Aligned Box enclosing all colliders of this object")]
	//This can be used to get the "bounds" of the object, but needs to be created manually
	//we should look into a programatic way to figure this out so we don't have to set it up for EVERY object
	//for now, only CanPickup objects have a BoundingBox, although maybe every sim object needs one for
	//spawning eventually? For now make sure the Box Collider component is disabled on this, because it's literally
	//just holding values for the center and size of the box.
	public GameObject BoundingBox = null;

	[Header("Raycast to these points to determine Visible/Interactable")]
	[SerializeField]
	public Transform[] VisibilityPoints = null;

    [Header("If this object is a Receptacle, put all trigger boxes here")]
	[SerializeField]
	public GameObject[] ReceptacleTriggerBoxes = null;

	[Header("State information Bools here")]
	public bool isVisible = false;
	public bool isInteractable = false;
	public bool isColliding = false;

	private Bounds bounds;


	//initial position object spawned in in case we want to reset the scene
	//private Vector3 startPosition;   

	public string UniqueID
	{
		get
		{
			return uniqueID;
		}

		set
		{
			uniqueID = value;
		}
	}

       public bool IsVisible
       {
               get
               {
                       return IsVisible;;
               }

               set {
                       isVisible = value;
               }
       }

	public Bounds Bounds
	{
		get
		{
			// XXX must define how to get the bounds of the simobj
			return bounds;
		}
	}


	public bool IsPickupable
	{
		get
		{
			return this.PrimaryProperty == SimObjPrimaryProperty.CanPickup;
		}
	}


	public SimObjType ObjType
	{
		get
		{
			return Type;
		}
	}

	public int ReceptacleCount
	{
		get
		{
			return 0;
		}
	}

	public List<string> ReceptacleObjectIds
	{
		// XXX need to implement 
		get
		{
			return this.Contains();
		}
	}

	public List<PivotSimObj> PivotSimObjs
	{
		get
		{
			return new List<PivotSimObj>();
		}
	}

	public bool Open()
	{
		// XXX need to implement
		return false;
	}

	public bool Close()
	{
		// XXX need to implement
		return false;
	}

	public bool IsOpenable
	{
		get { return this.GetComponent<CanOpen_Object>(); }
	}

	public bool IsOpen
	{
		get
		{
			CanOpen_Object coo = this.GetComponent<CanOpen_Object>();

			if (coo != null)
			{
				return coo.isOpen;
			}
			else
			{
				return false;
			}
		}
	}

	public bool IsReceptacle
	{
		get
		{
			return Array.IndexOf(SecondaryProperties, SimObjSecondaryProperty.Receptacle) > -1 &&
			 ReceptacleTriggerBoxes != null;
		}
	}

	//duplicate a non trigger collider, add a rigidbody to it and parant the duplicate to the original selection
	//for use with cabinet/fridge doors that need a secondary rigidbody to allow physics on the door while animating
#if UNITY_EDITOR
	[UnityEditor.MenuItem("SimObjectPhysics/Create RB Collider")]
	public static void CreateRBCollider()
	{
		GameObject prefabRoot = Selection.activeGameObject;
		//print(prefabRoot.name);

		GameObject inst = Instantiate(prefabRoot, Selection.activeGameObject.transform, true);

		//inst.transform.SetParent(Selection.activeGameObject.transform);

		inst.name = "rbCol";
		inst.gameObject.AddComponent<Rigidbody>();
		inst.GetComponent<Rigidbody>().isKinematic = true;
		inst.GetComponent<Rigidbody>().useGravity = true;

		//default tag and layer so that nothing is raycast against this. The only thing this exists for is to make physics real
		inst.tag = "Untagged";
		inst.layer = 0;// default layer

		//EditorUtility.GetPrefabParent(Selection.activeGameObject);
		//PrefabUtility.InstantiatePrefab(prefabRoot);
	}

#endif

	// Use this for initialization
	void Start()
	{
		//XXX For Debug setting up scene, comment out or delete when done settig up scenes
#if UNITY_EDITOR
		List<SimObjSecondaryProperty> temp = new List<SimObjSecondaryProperty>(SecondaryProperties);
		if (temp.Contains(SimObjSecondaryProperty.Receptacle))
		{
			if (ReceptacleTriggerBoxes.Length == 0)
			{
				Debug.LogError(this.name + " is missing ReceptacleTriggerBoxes please hook them up");
			}
		}
#endif
		//end debug setup stuff
	}

	public bool DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty prop)
	{
		bool result = false;
		List<SimObjSecondaryProperty> temp = new List<SimObjSecondaryProperty>(SecondaryProperties);

		if (temp.Contains(prop))
		{
			result = true;
		}

		return result;
	}
	// Update is called once per frame
	void Update()
	{

		//this is overriden by the Agent when doing the Visibility Sphere test
		//XXX Probably don't need to do this EVERY update loop except in editor for debug purposes
		isVisible = false;
		isInteractable = false;

	}

	private void FixedUpdate()
	{
		isColliding = false;
	}

	//used for throwing the sim object, or anything that requires adding force for some reason
	public void ApplyForce(ServerAction action)
	{
		Vector3 dir = new Vector3(action.x, action.y, action.z);
		Rigidbody myrb = gameObject.GetComponent<Rigidbody>();
		myrb.AddForce(dir * action.moveMagnitude);
	}

	//returns a game object list of all sim objects contained by this object if it is a receptacle
	public List<GameObject> Contains_GameObject()
	{
		List<SimObjSecondaryProperty> sspList = new List<SimObjSecondaryProperty>(SecondaryProperties);

		List<GameObject> objs = new List<GameObject>();

		//is this object a receptacle?
		if (sspList.Contains(SimObjSecondaryProperty.Receptacle))
		{
			//this is a receptacle, now populate objs list of contained objets to return below
			if (ReceptacleTriggerBoxes != null)
			{
				//do this once per ReceptacleTriggerBox referenced by this object
				foreach (GameObject rtb in ReceptacleTriggerBoxes)
				{
					//now go through every object each ReceptacleTriggerBox is keeping track of and add their string UniqueID to objs
					foreach (SimObjPhysics sop in rtb.GetComponent<Contains>().CurrentlyContainedObjects())
					{
						//don't add repeats
						if (!objs.Contains(sop.gameObject))
							objs.Add(sop.gameObject);
					}
				}
			}
		}

		return objs;
	}

	//if this is a receptacle object, check what is inside the Receptacle
	//make sure to return array of strings so that this info can be put into MetaData
	public List<string> Contains()
	{
		//grab a list of all secondary properties of this object
		List<SimObjSecondaryProperty> sspList = new List<SimObjSecondaryProperty>(SecondaryProperties);

		List<string> objs = new List<string>();

		//is this object a receptacle?
		if (sspList.Contains(SimObjSecondaryProperty.Receptacle))
		{
			//this is a receptacle, now populate objs list of contained objets to return below
			if (ReceptacleTriggerBoxes != null)
			{
				//do this once per ReceptacleTriggerBox referenced by this object
				foreach (GameObject rtb in ReceptacleTriggerBoxes)
				{
					//now go through every object each ReceptacleTriggerBox is keeping track of and add their string UniqueID to objs
					foreach (string id in rtb.GetComponent<Contains>().CurrentlyContainedUniqueIDs())
					{
						//don't add repeats
						if (!objs.Contains(id))
							objs.Add(id);
					}
					//objs.Add(rtb.GetComponent<Contains>().CurrentlyContainedUniqueIDs()); 
				}

#if UNITY_EDITOR

				if (objs.Count != 0)
				{
					//print the objs for now just to check in editor
					string result = UniqueID + " contains: ";

					foreach (string s in objs)
					{
						result += s + ", ";
					}

					Debug.Log(result);
				}

#endif
				return objs;
			}

			else
			{
				Debug.Log("No Receptacle Trigger Box!");
				return objs;
			}
		}

		else
		{
			Debug.Log(gameObject.name + " is not a Receptacle!");
			return objs;
		}
	}

	public void OnTriggerStay(Collider other)
	{

		//ignore collision of ghosted receptacle trigger boxes
		//because of this MAKE SURE ALL receptacle trigger boxes are tagged as "Receptacle," they should be by default
		//do this flag first so that the check against non Player objects overrides it in the right order
		if (other.tag == "Receptacle")
		{
			isColliding = false;
			return;
		}

		//make sure nothing is dropped while inside the agent (the agent will try to "push(?)" it out and it will fall in unpredictable ways
		else if (other.tag == "Player" && other.name == "FPSController")
		{
			isColliding = true;
			return;
		}

		//this is hitting something else so it must be colliding at this point!
		else if (other.tag != "Player")
		{
			isColliding = true;
			return;
		}

	}

#if UNITY_EDITOR
	void OnDrawGizmos()
	{
		Gizmos.color = Color.white;

		//if this object is in visibile range and not blocked by any other object, it is visible
		//visible drawn in yellow
		if (isVisible == true)
		{
			MeshFilter mf = gameObject.GetComponentInChildren<MeshFilter>(false);
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireMesh(mf.sharedMesh, -1, mf.transform.position, mf.transform.rotation, mf.transform.lossyScale);
		}

		//interactable drawn in magenta
		if (isInteractable == true)
		{
			MeshFilter mf = gameObject.GetComponentInChildren<MeshFilter>(false);
			Gizmos.color = Color.magenta;
			Gizmos.DrawWireMesh(mf.sharedMesh, -1, mf.transform.position, mf.transform.rotation, mf.transform.lossyScale);
		}

		//draw visibility points for editor
		Gizmos.color = Color.yellow;

		if (VisibilityPoints.Length > 0)
		{
			foreach (Transform t in VisibilityPoints)
			{
				Gizmos.DrawSphere(t.position, 0.01f);

			}
		}

		////draw interaction points for editor
		//Gizmos.color = Color.magenta;

		//foreach (Transform t in InteractionPoints)
		//{
		//    Gizmos.DrawSphere(t.position, 0.01f);

		//}

	}

	//CONTEXT MENU STUFF FOR SETTING UP SIM OBJECTS
	//RIGHT CLICK this script in the inspector to reveal these options
	//[ContextMenu("Cabinet")]
	void SetUpCabinet()
	{
		Type = SimObjType.Cabinet;
		PrimaryProperty = SimObjPrimaryProperty.Static;

		SecondaryProperties = new SimObjSecondaryProperty[2];
		SecondaryProperties[0] = SimObjSecondaryProperty.CanOpen;
		SecondaryProperties[1] = SimObjSecondaryProperty.Receptacle;

		if (!gameObject.GetComponent<Rigidbody>())
			gameObject.AddComponent<Rigidbody>();

		this.GetComponent<Rigidbody>().isKinematic = true;

		if (!gameObject.GetComponent<CanOpen_Object>())
		{
			gameObject.AddComponent<CanOpen_Object>();
			gameObject.GetComponent<CanOpen_Object>().SetMovementToRotate();
		}


		//if (!gameObject.GetComponent<MovingPart>())
		//gameObject.AddComponent<MovingPart>();

		List<GameObject> cols = new List<GameObject>();
		List<GameObject> tcols = new List<GameObject>();
		List<Transform> vpoints = new List<Transform>();
		List<GameObject> recepboxes = new List<GameObject>();

		List<GameObject> movparts = new List<GameObject>();

		List<Vector3> openPositions = new List<Vector3>();

		foreach (Transform child in gameObject.transform)
		{
			if (child.name == "StaticVisPoints")
			{
				foreach (Transform svp in child)
				{
					if (!vpoints.Contains(svp))
						vpoints.Add(svp);
				}
			}

			if (child.name == "ReceptacleTriggerBox")
			{
				//print("check");
				if (!recepboxes.Contains(child.gameObject))
					recepboxes.Add(child.gameObject);
			}

			//found the cabinet door, go into it and populate triggerboxes, colliders, t colliders, and vis points
			if (child.name == "CabinetDoor")
			{
				if (child.GetComponent<Rigidbody>())
					DestroyImmediate(child.GetComponent<Rigidbody>(), true);

				if (child.GetComponent<SimObjPhysics>())
					DestroyImmediate(child.GetComponent<SimObjPhysics>(), true);

				if (!movparts.Contains(child.gameObject))
				{
					movparts.Add(child.gameObject);
				}

				foreach (Transform c in child)
				{
					if (c.name == "Colliders")
					{
						foreach (Transform col in c)
						{
							if (!cols.Contains(col.gameObject))
								cols.Add(col.gameObject);

							if (col.childCount == 0)
							{
								GameObject prefabRoot = col.gameObject;

								GameObject inst = Instantiate(prefabRoot, col.gameObject.transform, true);

								//inst.transform.SetParent(Selection.activeGameObject.transform);

								inst.name = "rbCol";
								inst.gameObject.AddComponent<Rigidbody>();
								inst.GetComponent<Rigidbody>().isKinematic = true;
								inst.GetComponent<Rigidbody>().useGravity = true;

								//default tag and layer so that nothing is raycast against this. The only thing this exists for is to make physics real
								inst.tag = "Untagged";
								inst.layer = 0;// default layer
							}
						}
					}

					if (c.name == "TriggerColliders")
					{
						foreach (Transform col in c)
						{
							if (!tcols.Contains(col.gameObject))
								tcols.Add(col.gameObject);
						}
					}

					if (c.name == "VisibilityPoints")
					{
						foreach (Transform col in c)
						{
							if (!vpoints.Contains(col.transform))
								vpoints.Add(col.transform);
						}
					}
				}
			}
		}

		VisibilityPoints = vpoints.ToArray();
		//MyColliders = cols.ToArray();
		//MyTriggerColliders = tcols.ToArray();
		ReceptacleTriggerBoxes = recepboxes.ToArray();


		gameObject.GetComponent<CanOpen_Object>().MovingParts = movparts.ToArray();
		gameObject.GetComponent<CanOpen_Object>().openPositions = new Vector3[movparts.Count];
		gameObject.GetComponent<CanOpen_Object>().closedPositions = new Vector3[movparts.Count];

		if (openPositions.Count != 0)
			gameObject.GetComponent<CanOpen_Object>().openPositions = openPositions.ToArray();


		//this.GetComponent<CanOpen>().SetMovementToRotate();
	}

	//[ContextMenu("Drawer")]
	void SetUpDrawer()
	{
		//Type = SimObjType.Drawer;
		//PrimaryProperty = SimObjPrimaryProperty.Static;

		//SecondaryProperties = new SimObjSecondaryProperty[2];
		//SecondaryProperties[0] = SimObjSecondaryProperty.CanOpen;
		//SecondaryProperties[1] = SimObjSecondaryProperty.Receptacle;

		//if (!gameObject.GetComponent<Rigidbody>())
		//	gameObject.AddComponent<Rigidbody>();

		//this.GetComponent<Rigidbody>().isKinematic = true;

		//if (!gameObject.GetComponent<CanOpen>())
		//	gameObject.AddComponent<CanOpen>();

		//gameObject.GetComponent<CanOpen>().SetClosedPosition();

		if (!gameObject.GetComponent<CanOpen_Object>())
		{
			gameObject.AddComponent<CanOpen_Object>();
		}

		GameObject[] myobject = new GameObject[] { gameObject };
		gameObject.GetComponent<CanOpen_Object>().MovingParts = myobject;

	}

	//[ContextMenu("Find BoundingBox")]
	void ContextFindBoundingBox()
	{
		BoundingBox = gameObject.transform.Find("BoundingBox").gameObject;

	}

	[ContextMenu("Set Up Microwave")]
	void ContextSetUpMicrowave()
	{
		this.Type = SimObjType.Microwave;
		this.PrimaryProperty = SimObjPrimaryProperty.Static;

		this.SecondaryProperties = new SimObjSecondaryProperty[2];
		this.SecondaryProperties[0] = SimObjSecondaryProperty.Receptacle;
		this.SecondaryProperties[1] = SimObjSecondaryProperty.CanOpen;

		if(!gameObject.transform.Find("BoundingBox"))
		{
			GameObject bb = new GameObject("BoundingBox");
			bb.transform.position = gameObject.transform.position;
			bb.transform.SetParent(gameObject.transform);
			bb.AddComponent<BoxCollider>();
			bb.GetComponent<BoxCollider>().enabled = false;
			bb.tag = "Untagged";
			bb.layer = 0;

			BoundingBox = bb;
		}

		else
		{
			BoundingBox = gameObject.transform.Find("BoundingBox").gameObject;
		}

		if(!gameObject.GetComponent<CanOpen_Object>())
		{
			gameObject.AddComponent<CanOpen_Object>();
		}

		List<Transform> vplist = new List<Transform>();

		if(!gameObject.transform.Find("StaticVisPoints"))
		{
			GameObject svp = new GameObject("StaticVisPoints");
			svp.transform.position = gameObject.transform.position;
			svp.transform.SetParent(gameObject.transform);

			GameObject vp = new GameObject("vPoint");
			vp.transform.position = svp.transform.position;
			vp.transform.SetParent(svp.transform);
		}

		else
		{
			Transform vp = gameObject.transform.Find("StaticVisPoints");
			foreach (Transform child in vp)
			{
				vplist.Add(child);

				//set correct tag and layer for each object
				child.gameObject.tag = "Untagged";
				child.gameObject.layer = 8;
			}
		}

		Transform door = gameObject.transform.Find("Door");
		if(!door.Find("Col"))
		{
			GameObject col = new GameObject("Col");
			col.transform.position = door.transform.position;
			col.transform.SetParent(door.transform);

			col.AddComponent<BoxCollider>();

			col.transform.tag = "SimObjPhysics";
			col.layer = 8;

		}

		if(!door.Find("VisPoints"))
		{
						//empty to hold all visibility points
			GameObject vp = new GameObject("VisPoints");
			vp.transform.position = door.transform.position;
			vp.transform.SetParent(door.transform);

			//create first Visibility Point to work with
			GameObject vpc = new GameObject("vPoint");
			vpc.transform.position = vp.transform.position;
			vpc.transform.SetParent(vp.transform);
		}

		else
		{
			Transform vp = door.Find("VisPoints");
			foreach (Transform child in vp)
			{
				vplist.Add(child);

				//set correct tag and layer for each object
				child.gameObject.tag = "Untagged";
				child.gameObject.layer = 8;
			}
		}

		VisibilityPoints = vplist.ToArray();

		CanOpen_Object coo = gameObject.GetComponent<CanOpen_Object>();
		coo.MovingParts = new GameObject[] {door.transform.gameObject};
		coo.openPositions = new Vector3[] {new Vector3(0, 90, 0)};
		coo.closedPositions = new Vector3[] {Vector3.zero};
		coo.SetMovementToRotate();

		if(gameObject.transform.Find("ReceptacleTriggerBox"))
		{
			GameObject[] rtb = new GameObject[] {gameObject.transform.Find("ReceptacleTriggerBox").transform.gameObject};
			ReceptacleTriggerBoxes = rtb;
		}



	}

	//[ContextMenu("Set Up SimObjPhysics")]
	void ContextSetUpSimObjPhysics()
	{
		if (this.Type == SimObjType.Undefined || this.PrimaryProperty == SimObjPrimaryProperty.Undefined)
		{
			Debug.Log("Type / Primary Property is missing");
			return;
		}
		//set up this object ot have the right tag and layer
		gameObject.tag = "SimObjPhysics";
		gameObject.layer = 8;

		if (!gameObject.GetComponent<Rigidbody>())
			gameObject.AddComponent<Rigidbody>();

		if (!gameObject.transform.Find("Colliders"))
		{
			GameObject c = new GameObject("Colliders");
			c.transform.position = gameObject.transform.position;
			c.transform.SetParent(gameObject.transform);
            
			GameObject cc = new GameObject("Col");
			cc.transform.position = c.transform.position;
			cc.transform.SetParent(c.transform);
		}

		if (!gameObject.transform.Find("TriggerColliders"))//static sim objets still need trigger colliders
		{
			//empty to hold all Trigger Colliders
			GameObject tc = new GameObject("TriggerColliders");
			tc.transform.position = gameObject.transform.position;
			tc.transform.SetParent(gameObject.transform);

			//create first trigger collider to work with
			GameObject tcc = new GameObject("tCol");
			tcc.transform.position = tc.transform.position;
			tcc.transform.SetParent(tc.transform);
		}

		if (!gameObject.transform.Find("VisibilityPoints"))
		{
			//empty to hold all visibility points
			GameObject vp = new GameObject("VisibilityPoints");
			vp.transform.position = gameObject.transform.position;
			vp.transform.SetParent(gameObject.transform);

			//create first Visibility Point to work with
			GameObject vpc = new GameObject("vPoint");
			vpc.transform.position = vp.transform.position;
			vpc.transform.SetParent(vp.transform);
		}

		if (!gameObject.transform.Find("BoundingBox") && this.PrimaryProperty != SimObjPrimaryProperty.Static)
		{
			GameObject rac = new GameObject("BoundingBox");
			rac.transform.position = gameObject.transform.position;
			rac.transform.SetParent(gameObject.transform);
		}

		ContextSetUpColliders();
		ContextSetUpTriggerColliders();
		ContextSetUpVisibilityPoints();
		//ContextSetUpInteractionPoints();
		ContextSetUpBoundingBox();
	}

	//[ContextMenu("Set Up Colliders")]
	void ContextSetUpColliders()
	{
		if (transform.Find("Colliders"))
		{
			Transform Colliders = transform.Find("Colliders");

			List<GameObject> listColliders = new List<GameObject>();

			foreach (Transform child in Colliders)
			{
				//list.toarray
				listColliders.Add(child.gameObject);

				//set correct tag and layer for each object
				//also ensure all colliders are NOT trigger
				child.gameObject.tag = "SimObjPhysics";
				child.gameObject.layer = 8;

				if (child.GetComponent<Collider>())
				{
					child.GetComponent<Collider>().enabled = true;
					child.GetComponent<Collider>().isTrigger = false;
				}

			}

			//MyColliders = listColliders.ToArray();
		}
	}

	//[ContextMenu("Set Up TriggerColliders")]
	void ContextSetUpTriggerColliders()
	{
		if (transform.Find("TriggerColliders"))
		{
			Transform tc = transform.Find("TriggerColliders");

			List<GameObject> listtc = new List<GameObject>();

			foreach (Transform child in tc)
			{
				//list.toarray
				listtc.Add(child.gameObject);

				//set correct tag and layer for each object
				//also ensure all colliders are set to trigger
				child.gameObject.tag = "SimObjPhysics";
				child.gameObject.layer = 8;

				if (child.GetComponent<Collider>())
				{
					child.GetComponent<Collider>().enabled = true;
					child.GetComponent<Collider>().isTrigger = true;
				}

			}

			//MyTriggerColliders = listtc.ToArray();
		}
	}

	// [ContextMenu("Set Up VisibilityPoints")]
	void ContextSetUpVisibilityPoints()
	{
		if (transform.Find("VisibilityPoints"))
		{
			Transform vp = transform.Find("VisibilityPoints");

			List<Transform> vplist = new List<Transform>();

			foreach (Transform child in vp)
			{
				vplist.Add(child);

				//set correct tag and layer for each object
				child.gameObject.tag = "Untagged";
				child.gameObject.layer = 8;
			}

			VisibilityPoints = vplist.ToArray();
		}
	}

	//[ContextMenu("Set Up Rotate Agent Collider")]
	void ContextSetUpBoundingBox()
	{
		if (transform.Find("BoundingBox"))
		{
			BoundingBox = transform.Find("BoundingBox").gameObject;

			//This collider is used as a size reference for the Agent's Rotation checking boxes, so it does not need
			//to be enabled. To ensure this doesn't interact with anything else, set the Tag to Untagged, the layer to 
			//SimObjInvisible, and disable this component. Component values can still be accessed if the component itself
			//is not enabled.
			BoundingBox.tag = "Untagged";
			BoundingBox.layer = 9;//layer 9 - SimObjInvisible

			if (BoundingBox.GetComponent<BoxCollider>())
				BoundingBox.GetComponent<BoxCollider>().enabled = false;
		}
	}
	#endif

    
}
