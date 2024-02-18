using System;
using UnityEngine;

[Serializable]
public class PersistentARData
{
	public string prefabName;
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