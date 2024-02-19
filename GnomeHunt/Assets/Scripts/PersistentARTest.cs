using System.Collections.Generic;
using Niantic.Lightship.AR.LocationAR;
using Niantic.Lightship.AR.NavigationMesh;
using UnityEngine;
using UnityEngine.EventSystems;

public class PersistentARTest : MonoBehaviour
{
	[SerializeField] private Camera arCamera = null;

	[SerializeField] private ARLocationManager _arLocationManager;
	[SerializeField] private LightshipNavMeshManager navMeshManager = null;

	[SerializeField] private GameObject reticlePrefab = null;

	[SerializeField] private float reticleDistance = 2f;

	[SerializeField] private float reticleHeightAboveGround = .1f;

	[SerializeField] private GameObject objectToPlace = null;

	private GameObject reticleInstance;

	private float currentReticleHeight = 0;

	private List<GameObject> spawnedObjects = new List<GameObject>();
	private List<PersistentARData> persistentAR = new List<PersistentARData>();
	private string PERSISTENT_AR_COUNT_KEY = "ar_count";
	private string PERSISTENT_PREFAB_NAME = "test";

	void Start()
	{
		if (reticlePrefab != null)
		{
			reticleInstance = Instantiate(reticlePrefab);
			reticleInstance.SetActive(false);
		}

		for (int index = 0; index < PlayerPrefs.GetInt(PERSISTENT_AR_COUNT_KEY, 0); index++)
		{
			string data = PlayerPrefs.GetString(PERSISTENT_PREFAB_NAME + index, null);
			if (string.IsNullOrEmpty(data) == false)
			{
				PersistentARData arData = JsonUtility.FromJson<PersistentARData>(data);
				persistentAR.Add(arData);
				GameObject arObject = Instantiate(objectToPlace, _arLocationManager.ARLocations[0].transform);
				arObject.transform.localPosition = arData.position.ToVector3();
				arObject.transform.rotation = arData.rotation.ToQuaternion();
				arObject.transform.localScale = arData.scale.ToVector3();
				spawnedObjects.Add(arObject);
			}
		}
	}

	private void OnApplicationQuit()
	{
		PlayerPrefs.SetInt(PERSISTENT_AR_COUNT_KEY, persistentAR.Count);

		for (var index = 0; index < persistentAR.Count; index++)
		{
			PersistentARData arData = persistentAR[index];
			PlayerPrefs.SetString(PERSISTENT_PREFAB_NAME + index, JsonUtility.ToJson(arData));
			PlayerPrefs.Save();
		}
	}

	void Update()
	{
		if (navMeshManager != null)
		{
			LightshipNavMesh navMesh = navMeshManager.LightshipNavMesh;

			Ray ray = new Ray(arCamera.transform.position, arCamera.transform.forward);
			RaycastHit hit;

			if (Physics.Raycast(ray, out hit, reticleDistance))
			{
				if (reticleInstance != null && reticleInstance.activeSelf == false)
				{
					reticleInstance.SetActive(true);
				}

				currentReticleHeight = hit.point.y;
			}
		}

		UpdateReticlePosition();

		if (Input.GetMouseButtonDown(0) && IsPointerOverUIObject() == false && reticleInstance != null && reticleInstance.activeSelf)
		{
			Transform objectPlaced = Instantiate(objectToPlace, reticleInstance.transform.position, Quaternion.identity, _arLocationManager.ARLocations[0].transform).transform;
			Vector3 position = objectPlaced.localPosition;
			Vector3 scale = objectPlaced.localScale;
			Quaternion rotation = objectPlaced.rotation;

			persistentAR.Add(new PersistentARData
			{
				prefabName = PERSISTENT_PREFAB_NAME,
				position = new PersistentVector(position.x, position.y, position.z),
				rotation = new PersistentRotation(rotation.x, rotation.y, rotation.z, rotation.w),
				scale = new PersistentVector(scale.x, scale.y, scale.z)
			});
		}
	}

	void UpdateReticlePosition()
	{
		if (reticleInstance != null)
		{
			Vector3 newPosition = arCamera.transform.position + arCamera.transform.forward * reticleDistance;
			newPosition.y = currentReticleHeight + reticleHeightAboveGround;
			reticleInstance.transform.position = newPosition;
		}
	}

	public void ClearPersistentAr()
	{
		foreach (GameObject arObject in spawnedObjects)
		{
			Destroy(arObject);
		}
		
		spawnedObjects.Clear();
		persistentAR.Clear();
		
		PlayerPrefs.DeleteAll();
		PlayerPrefs.Save();
	}
	
	public void CopyPersistentAr()
	{
		foreach (GameObject arObject in spawnedObjects)
		{
			Destroy(arObject);
		}
		
		spawnedObjects.Clear();
		persistentAR.Clear();
		
		PlayerPrefs.DeleteAll();
		PlayerPrefs.Save();
	}
	
	private bool IsPointerOverUIObject()
	{
		// Create a pointer event for the current mouse position
		PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
		eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);

		// Create a list to receive all results of the raycast
		List<RaycastResult> results = new List<RaycastResult>();
		EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

		// Return true if any UI elements were hit by the raycast
		return results.Count > 0;
	}
}