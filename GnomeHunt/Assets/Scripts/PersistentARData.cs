using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class PersistentARDataPayload : IEnumerable
{
	public string locationData;
	public PersistentARData[] payload;

	public PersistentARDataPayload(string locationData, PersistentARData[] payload)
	{
		this.locationData = locationData;
		this.payload = payload;
	}

	public void Clear()
	{
		payload = Array.Empty<PersistentARData>();
	}

	public IEnumerator GetEnumerator()
	{
		return payload.GetEnumerator();
	}
}

[Serializable]
public class PersistentARData
{
	public string prefabName;
	public string guid;
	public PersistentVector position;
	public PersistentRotation rotation;
	public PersistentVector scale;
}

[Serializable]
public class PersistentVector
{
	public float x;
	public float y;
	public float z;

	public PersistentVector(float x, float y, float z)
	{
		this.x = x;
		this.y = y;
		this.z = z;
	}

	public Vector3 ToVector3()
	{
		return new Vector3(x, y, z);
	}
}

[Serializable]
public class PersistentRotation
{
	public float x;
	public float y;
	public float z;
	public float w;

	public PersistentRotation(float x, float y, float z, float w)
	{
		this.x = x;
		this.y = y;
		this.z = z;
		this.w = w;
	}

	public Quaternion ToQuaternion()
	{
		return new Quaternion(x, y, z, w);
	}
}