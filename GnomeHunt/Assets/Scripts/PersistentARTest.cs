using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
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
	private const string PERSISTENT_AR_DATA_PLAYER_PREF_KEY = "PERSISTENT_AR_DATA";

	private void Start()
	{
		if (reticlePrefab != null)
		{
			reticleInstance = Instantiate(reticlePrefab);
			reticleInstance.SetActive(false);
		}

		string persistentARData = PlayerPrefs.GetString(PERSISTENT_AR_DATA_PLAYER_PREF_KEY, null);
		SpawnObjectsFromPayload(DecompressString(persistentARData));
	}

	private void SpawnObjectsFromPayload(string argPayload)
	{
		if (string.IsNullOrEmpty(argPayload) == false)
		{
			PersistentARDataPayload arDataPayload = JsonUtility.FromJson<PersistentARDataPayload>(argPayload);
			foreach (PersistentARData arData in arDataPayload)
			{
				persistentAR.Add(arData);
				GameObject arObject = Instantiate(objectToPlace, _arLocationManager.ARLocations[0].transform);
				arObject.transform.localPosition = arData.position.ToVector3();
				arObject.transform.rotation = arData.rotation.ToQuaternion();
				arObject.transform.localScale = arData.scale.ToVector3();
				spawnedObjects.Add(arObject);
			}
		}
	}

	private void Update()
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
				prefabName = objectToPlace.name,
				guid = Guid.NewGuid().ToString(),
				position = new PersistentVector(position.x, position.y, position.z),
				rotation = new PersistentRotation(rotation.x, rotation.y, rotation.z, rotation.w),
				scale = new PersistentVector(scale.x, scale.y, scale.z)
			});

			spawnedObjects.Add(objectPlaced.gameObject);
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
		SavePersistentARData();
		GUIUtility.systemCopyBuffer = PlayerPrefs.GetString(PERSISTENT_AR_DATA_PLAYER_PREF_KEY);
	}

	public void LoadPersistentAr()
	{
		SpawnObjectsFromPayload(DecompressString(GUIUtility.systemCopyBuffer));
		SavePersistentARData();
	}

	private void OnApplicationQuit()
	{
		SavePersistentARData();
	}

	private void SavePersistentARData()
	{
		PlayerPrefs.SetString(PERSISTENT_AR_DATA_PLAYER_PREF_KEY, CompressString(JsonUtility.ToJson(new PersistentARDataPayload(persistentAR.ToArray()))));
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

	private string CompressString(string argString)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(argString);
		using (MemoryStream mso = new MemoryStream())
		{
			using (GZipStream gs = new GZipStream(mso, CompressionMode.Compress))
			{
				gs.Write(bytes, 0, bytes.Length);
			}

			return Convert.ToBase64String(mso.ToArray());
		}
	}
	
	private string DecompressString(string argString)
	{
		using (var compressedStream = new MemoryStream(Convert.FromBase64String(argString)))
		using (var decompressor = new GZipStream(compressedStream, CompressionMode.Decompress))
		using (var decompressedStream = new MemoryStream())
		{
			decompressor.CopyTo(decompressedStream);
			var decompressedData = decompressedStream.ToArray();
			return Encoding.UTF8.GetString(decompressedData);
		}
	}
}