using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(UniqueId))]
[RequireComponent(typeof(SerializedStaticObject))]
[AddComponentMenu("Unit/Team")]
public class Team : MonoBehaviour
{
  private readonly static string className = "Team";

  [Tooltip("Enemy teams for this team.")]
  [SerializeField]
  private GameObject[] enemyTeams;

  [Tooltip("Max unit can be add to this team.")]
  [SerializeField]
  private int maxUnits = int.MaxValue;

  private int _numberUnits = 0;
  private Dictionary<string, GameObject> _objectPrefabs = new Dictionary<string, GameObject>();

  public bool CanAddUnit
  {
    get { return _numberUnits < maxUnits; }
  }

  public void AddUnit(GameObject unit)
  {
    if (!CanAddUnit)
    {
      Debug.LogWarning("Too many warriors in team. Can't add new.");
      return;
    }
    unit.transform.parent = transform;
    _numberUnits++;
  }

  public void SaveStaticObject(ref Dictionary<string, object> dictionary)
  {
    var id = GetComponent<UniqueId>().ID;
    var objects = GetComponentsInChildren<SerializedDynamicObject>();
    dictionary.Add(className + "[" + id + "]._numberUnits", _numberUnits);

    try
    {
      int index = 0;
      foreach (var obj in objects)
      {
        var prefix = className + "[" + id + "].object[" + index + "]";
        dictionary.Add(prefix + ".prefabName", obj.PrefabName);
        obj.SaveObject(new KeyValuePair<string, Dictionary<string, object>>(prefix, dictionary));
        index++;
      }
      dictionary.Add(className + "[" + id + "].numActiveUnits", index);
    }
    catch (Exception e)
    {
      Debug.LogException(e);
    }
  }

  public void LoadStaticObject(ref Dictionary<string, object> dictionary)
  {
    RemoveAllDynamicChild();
    var id = GetComponent<UniqueId>().ID;
    try
    {
      _numberUnits = (int)dictionary[className + "[" + id + "]._numberUnits"];
      var numActiveUnits = (int)dictionary[className + "[" + id + "].numActiveUnits"];
      for (var index = 0;index < numActiveUnits; ++index)
      {
        var prefix = className + "[" + id + "].object[" + index + "]";
        var prefabName = (string)dictionary[prefix + ".prefabName"];
        var obj = CreateObject(prefabName);
        if (obj)
        {
          obj.transform.parent = transform;
          obj.GetComponent<SerializedDynamicObject>().LoadObject(new KeyValuePair<string, Dictionary<string, object>>(prefix, dictionary));
        }
      };
    }
    catch (Exception e)
    {
      Debug.LogException(e);
    }
  }

  private void RemoveAllDynamicChild()
  {
    var objects = GetComponentsInChildren<SerializedDynamicObject>();
    foreach (var obj in objects)
      Destroy(obj.gameObject);
  }

  private GameObject CreateObject(string prefabName)
  {
    if (!_objectPrefabs.TryGetValue(prefabName, out GameObject prefab))
    {
      Debug.LogError("Cen't find " + prefabName + " !");
      return null;
    }
    return Instantiate(prefab);
  }

  public void AddPrefabIfNotAddedYet(GameObject prefab)
  {
    if (_objectPrefabs.ContainsKey(prefab.name))
      return;
    try
    {
      _objectPrefabs.Add(prefab.name, prefab);
    }
    catch (Exception e)
    {
      Debug.LogException(e);
    }
  }
  
  public Transform FindNearEnemy(Vector3 position, float maxDistance)
  {
    var minDistanceSqr = maxDistance * maxDistance;
    Transform result = null;
    foreach (var team in enemyTeams)
      for (var i = 0; i < gameObject.transform.childCount; ++i)
      {
        var enemyTransform = gameObject.transform.GetChild(i);
        var dis = Vector3.SqrMagnitude(position - enemyTransform.position);
        if (dis<minDistanceSqr)
        {
          minDistanceSqr = dis;
          result = enemyTransform;
        }
      }
    return result;
  }
}
