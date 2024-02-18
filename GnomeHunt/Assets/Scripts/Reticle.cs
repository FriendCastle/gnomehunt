using System.Collections.Generic;
using Niantic.Lightship.AR.LocationAR;
using Niantic.Lightship.AR.NavigationMesh;
using Niantic.Lightship.AR.PersistentAnchors;
using UnityEngine;
using UnityEngine.Serialization;

public class Reticle : MonoBehaviour
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
				GameObject arObject = Instantiate(objectToPlace, arData.position.ToVector3(), arData.rotation.ToQuaternion(), _arLocationManager.ARLocations[0].transform);
				arObject.transform.localScale = arData.scale.ToVector3();
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

		if (Input.GetMouseButtonDown(0) && reticleInstance != null && reticleInstance.activeSelf)
		{
			Transform objectPlaced = Instantiate(objectToPlace, reticleInstance.transform.position, Quaternion.identity, _arLocationManager.ARLocations[0].transform).transform;
			Vector3 position = objectPlaced.position;
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
}