using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Niantic.Lightship.AR.LocationAR;
using Niantic.Lightship.AR.NavigationMesh;
using Niantic.Lightship.AR.PersistentAnchors;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PersistentARTest : MonoBehaviour
{
	[Header("UI")]
	[SerializeField] private CanvasGroup mainMenuGroup;
	[SerializeField] private TextMeshProUGUI trackingText;
	[SerializeField] private Button findGnomeButton;
	[SerializeField] private Button copyGnomeButton;
	

	[Header("AR")]
	[SerializeField] private Camera arCamera = null;
	[SerializeField] private ARLocationManager _arLocationManager;
	[SerializeField] private LightshipNavMeshManager navMeshManager = null;

	[Header("Reticle")]
	[SerializeField] private GameObject reticlePrefab = null;

	[SerializeField] private float reticleDistance = 2f;

	[SerializeField] private float reticleHeightAboveGround = .1f;

	[SerializeField] private GameObject objectToPlace = null;

	private GameObject reticleInstance;

	private float currentReticleHeight = 0;

	private PersistentARDataPayload currentPayload;
	private List<GameObject> spawnedObjects = new List<GameObject>();
	private List<PersistentARData> persistentAR = new List<PersistentARData>();
	private string currentLocationData;
	private bool tracking = false;
	private bool canPlaceGnome = false;
	private bool gnomePlaced = false;

	private const string PERSISTENT_AR_DATA_PLAYER_PREF_KEY = "PERSISTENT_AR_DATA";
	public enum GameState
	{
		NotTracking,
		FindGnome,
		PlaceGnome
	}

	private GameState currentGameState = GameState.NotTracking;
	
	private void Awake()
	{
		if (reticlePrefab != null)
		{
			reticleInstance = Instantiate(reticlePrefab);
			reticleInstance.SetActive(false);
		}
	}

	private void Start()
	{
		_arLocationManager.locationTrackingStateChanged += OnLocationTrackingStateChanged;
	}

	private void OnApplicationFocus(bool hasFocus)
	{
		if (hasFocus)
		{
			currentPayload = GetCurrentPersistentARData();
			findGnomeButton.interactable = currentPayload != null;
			if (tracking)
			{
				trackingText.text = string.Format("Tracked - {0}", currentPayload == null ? "No Data Found" : "Existing Gnome Data");
			}
		}
	}

	private void OnLocationTrackingStateChanged(ARLocationTrackedEventArgs argEvent)
	{
		if (tracking == false && argEvent.Tracking)
		{
			// could be expanded to support more than one location if we load the location dynamically in the future
			currentLocationData = argEvent.ARLocation.Payload.ToBase64();

			mainMenuGroup.interactable = true;
			currentPayload = GetCurrentPersistentARData();
			trackingText.text = string.Format("Tracked - {0}", currentPayload == null ? "No Data Found" : "Existing Gnome Data");
			findGnomeButton.interactable = currentPayload != null;
		}
	}

	private void SpawnObjectsFromPayload(PersistentARDataPayload argPayload)
	{
		if (argPayload != null)
		{
			foreach (PersistentARData arData in argPayload)
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
		switch (currentGameState)
		{
			case GameState.NotTracking:
				break;
			case GameState.FindGnome:
				break;
			case GameState.PlaceGnome:
				PlaceGnomeUpdate();
				break;
		}
	}

	public void OnPlaceGnomeButtonPressed()
	{
		currentGameState = GameState.PlaceGnome;
		mainMenuGroup.gameObject.SetActive(false);
		trackingText.text = "Look around to enable Gnome placement!";
	}
	
	private void PlaceGnomeUpdate()
	{
		if (navMeshManager != null)
		{
			Ray ray = new Ray(arCamera.transform.position, arCamera.transform.forward);
			RaycastHit hit;

			if (Physics.Raycast(ray, out hit, reticleDistance) && Vector3.Distance(hit.normal, Vector3.up) < .25f)
			{
				if (reticleInstance != null && reticleInstance.activeSelf == false)
				{
					reticleInstance.SetActive(true);
					trackingText.text = "Tap To Place Gnome!";
				}

				currentReticleHeight = hit.point.y;
			}
		}

		UpdateReticlePosition();

		if (Input.GetMouseButtonDown(0) && IsPointerOverUIObject() == false && reticleInstance != null && reticleInstance.activeSelf)
		{
			// Clear existing placements since we only want one gnome
			foreach (GameObject arObject in spawnedObjects)
			{
				Destroy(arObject);
			}
			persistentAR.Clear();
			spawnedObjects.Clear();
			
			// Place gnome at new location
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
			gnomePlaced = true;
			copyGnomeButton.gameObject.SetActive(true);
			trackingText.text = "Gnome Placed!\nTap to replace or copy data to send to a friend!";
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
		if (currentPayload != null)
		{
			SpawnObjectsFromPayload(currentPayload);
		}

		SavePersistentARData();
	}

	private PersistentARDataPayload GetCurrentPersistentARData()
	{
		PersistentARDataPayload CheckValidLocationData(PersistentARDataPayload argPayload)
		{
			if (argPayload != null && string.IsNullOrEmpty(argPayload.locationData) == false)
			{
				if (argPayload.locationData == currentLocationData && argPayload.payload != null && argPayload.payload.Length > 0)
				{
					return argPayload;
				}
			}

			return null;
		}

		// Check clipboard for new data
		if (string.IsNullOrEmpty(GUIUtility.systemCopyBuffer) == false)
		{
			PersistentARDataPayload clipboardPayload = GetPayloadFromJsonString(GUIUtility.systemCopyBuffer);
			return CheckValidLocationData(clipboardPayload);
		}

		// Check if you already have data saved for this location
		string persistentARData = PlayerPrefs.GetString(PERSISTENT_AR_DATA_PLAYER_PREF_KEY, null);
		PersistentARDataPayload savedPayload = GetPayloadFromJsonString(persistentARData);
		return CheckValidLocationData(savedPayload);
	}
	
	private void SavePersistentARData()
	{
		PlayerPrefs.SetString(PERSISTENT_AR_DATA_PLAYER_PREF_KEY, CompressString(JsonUtility.ToJson(new PersistentARDataPayload(currentLocationData, persistentAR.ToArray()))));
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

	private PersistentARDataPayload GetPayloadFromJsonString(string argJsonString)
	{
		// Check for valid payload
		if (string.IsNullOrEmpty(argJsonString) == false && Convert.TryFromBase64String(argJsonString, new Span<byte>(new byte[argJsonString.Length]), out _))
		{
			return JsonUtility.FromJson<PersistentARDataPayload>(DecompressString(argJsonString));
		}

		return null;
	}
}